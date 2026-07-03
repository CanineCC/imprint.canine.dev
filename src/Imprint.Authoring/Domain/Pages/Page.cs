using System.Collections.Immutable;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Pages;

// The richest aggregate: nearly every editor interaction lands here. Behaviors
// validate and raise; When() only folds. Structural rules the drag-and-drop slot
// planner also needs live in Placement — the aggregate consults them, so the editor's
// offered drop targets and the domain's accepted ones can never disagree.
public sealed class Page : AggregateRoot
{
    private const int MaxTitleLength = 200;
    private const int MaxMetaLength = 300;
    private const int MaxTextLength = 500;
    private const int MinColumns = 2;
    private const int MaxColumns = 4;
    private const int MinRatio = 1;
    private const int MaxRatio = 3;

    private const string TextField = "text";
    private const string HtmlField = "html";
    private const string LabelField = "label";
    private const string AltField = "alt";

    public PageId Id { get; private set; }
    public SiteId SiteId { get; private set; }
    public Slug Slug { get; private set; }
    public LocalizedText Title { get; private set; } = LocalizedText.Empty;
    public LocalizedText MetaTitle { get; private set; } = LocalizedText.Empty;
    public LocalizedText MetaDescription { get; private set; } = LocalizedText.Empty;
    public PageTree Tree { get; private set; } = PageTree.Empty;
    public long? PublishedVersion { get; private set; }
    public bool IsDeleted { get; private set; }

    public override string StreamId => Id.Stream;

    // ------------------------------------------------------------------ behaviors

    public static Page Create(PageId id, SiteId siteId, Slug slug, Locale initialLocale, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("A page needs a title.");
        }

        EnsureTitleLength(title);
        var page = new Page();
        page.Raise(new PageCreated(id, siteId, slug.Value, initialLocale, title));
        return page;
    }

    public void ChangeTitle(Locale locale, string title)
    {
        EnsureNotDeleted();
        EnsureTitleLength(title);
        Raise(new TitleChanged(locale, title));
    }

    public void ChangeSlug(Slug slug)
    {
        EnsureNotDeleted();
        if (slug == Slug)
        {
            // Nothing changed, so nothing happened — an event here would only bloat
            // the stream and mark published pages as modified for no reason.
            return;
        }

        Raise(new SlugChanged(slug.Value));
    }

    public void ChangeMeta(Locale locale, string? metaTitle, string? metaDescription)
    {
        EnsureNotDeleted();
        if (metaTitle?.Length > MaxMetaLength || metaDescription?.Length > MaxMetaLength)
        {
            throw new DomainException($"Meta text is limited to {MaxMetaLength} characters.");
        }

        Raise(new MetaChanged(locale, metaTitle, metaDescription));
    }

    public void AddNode(NodeId parentId, int index, Node spec)
    {
        EnsureNotDeleted();
        var parent = FindTargetParent(parentId);
        EnsureCanPlace(parent, spec);
        EnsureSpecInternallyValid(spec);
        EnsureFreshIds(spec);
        EnsureDepthBudget(parent is null ? 0 : Tree.DepthOf(parentId), spec);
        EnsureNodeBudget(spec, replacedNodes: 0);

        var siblings = parent is null ? Tree.Roots : ((IContainerNode)parent).Children;
        EnsureIndexInRange(index, siblings.Count);

        Raise(new NodeAdded(parentId, index, spec));
    }

    public void MoveNode(NodeId nodeId, NodeId newParentId, int newIndex)
    {
        EnsureNotDeleted();
        var node = Tree.Find(nodeId)
            ?? throw new DomainException("The element no longer exists on this page.");
        if (Placement.IsManagedCell(Tree, nodeId))
        {
            throw new DomainException("Column cells cannot be moved — adjust the column layout instead.");
        }

        if (Placement.WouldCreateCycle(Tree, nodeId, newParentId))
        {
            throw new DomainException("An element cannot be moved into itself or its own content.");
        }

        var newParent = FindTargetParent(newParentId);
        EnsureCanPlace(newParent, node);
        EnsureDepthBudget(newParent is null ? 0 : Tree.DepthOf(newParentId), node);

        var (currentParentId, currentIndex) = LocationOf(nodeId);
        var targetSiblings = newParent is null ? Tree.Roots : ((IContainerNode)newParent).Children;
        // PageTree.Move removes first, then inserts: the valid slot range shrinks by
        // one when the node stays within the same parent.
        var slotCount = targetSiblings.Count - (currentParentId == newParentId ? 1 : 0);
        EnsureIndexInRange(newIndex, slotCount);

        if (currentParentId == newParentId && newIndex == currentIndex)
        {
            return; // Dropping an element exactly where it already is is not a change.
        }

        Raise(new NodeMoved(nodeId, newParentId, newIndex));
    }

    public void RemoveNode(NodeId nodeId)
    {
        EnsureNotDeleted();
        if (!Tree.Contains(nodeId))
        {
            throw new DomainException("The element no longer exists on this page.");
        }

        if (Placement.IsManagedCell(Tree, nodeId))
        {
            throw new DomainException("Column cells cannot be removed — reduce the number of columns instead.");
        }

        Raise(new NodeRemoved(nodeId));
    }

    public void DuplicateNode(NodeId nodeId, NodeId copyId)
    {
        EnsureNotDeleted();
        var node = Tree.Find(nodeId)
            ?? throw new DomainException("The element no longer exists on this page.");
        if (Placement.IsManagedCell(Tree, nodeId))
        {
            throw new DomainException("Column cells cannot be duplicated — duplicate the whole columns element instead.");
        }

        EnsureNodeBudget(node, replacedNodes: 0);

        // The caller supplies the copy's root id (so it can build a RemoveNode inverse
        // for undo); descendant ids are minted here. All ids are recorded in the event,
        // so replay yields the identical tree without generating anything.
        if (copyId.IsRoot)
        {
            throw new DomainException("A duplicate cannot use the reserved root id.");
        }

        var copy = CloneWithFreshIds(node) with { Id = copyId };
        if (Tree.Contains(copyId))
        {
            throw new DomainException("The duplicate's id is already used on this page.");
        }

        Raise(new NodeDuplicated(nodeId, copy));
    }

    public void ChangeNodeProps(Node replacement)
    {
        EnsureNotDeleted();
        var current = Tree.Find(replacement.Id)
            ?? throw new DomainException("The element no longer exists on this page.");
        if (current.GetType() != replacement.GetType())
        {
            throw new DomainException($"A {current.DisplayName} cannot be changed into a {replacement.DisplayName}.");
        }

        // The event carries the final node so the fold is a plain Replace. Children
        // are resolved here: callers send props, the aggregate owns the structure.
        var resolved = current switch
        {
            ColumnsNode currentColumns => ResolveColumnCells(currentColumns, (ColumnsNode)replacement),
            IContainerNode currentContainer => ((IContainerNode)replacement).WithChildren(currentContainer.Children),
            _ => replacement,
        };

        // Same content + structural invariants as AddNode: a props change is another
        // way content enters the tree, so it cannot smuggle non-canonical HTML, an
        // out-of-range level/size, or (when a ColumnsNode grows) more nodes than the
        // page cap allows.
        EnsureSpecInternallyValid(resolved);
        EnsureNodeBudget(resolved, replacedNodes: PageTree.Flatten(current).Count());

        if (resolved.Equals(current))
        {
            return; // Identical result — nothing happened.
        }

        Raise(new NodePropsChanged(resolved));
    }

    public void EditText(NodeId nodeId, string field, Locale locale, string value)
    {
        EnsureNotDeleted();
        var node = Tree.Find(nodeId)
            ?? throw new DomainException("The element no longer exists on this page.");
        var currentText = TextFieldOf(node, field)
            ?? throw new DomainException($"{node.DisplayName} has no editable '{field}' text.");

        EnsureValidTextValue(field, value);

        if ((currentText.Get(locale) ?? string.Empty) == value)
        {
            return; // Unchanged — the editor fires on blur, which often changes nothing.
        }

        Raise(new TextChanged(nodeId, field, locale, value));
    }

    public void SetBlockOverride(NodeId instanceId, NodeId definitionNodeId, string field, Locale locale, string? value)
    {
        EnsureNotDeleted();
        FindBlockInstance(instanceId);
        if (field is not (TextField or HtmlField or LabelField or AltField))
        {
            throw new DomainException($"'{field}' cannot be overridden on a block instance.");
        }

        if (value is not null)
        {
            EnsureValidTextValue(field, value);
        }

        // Whether definitionNodeId actually exists in the block definition is a
        // slice/read-model concern: this aggregate cannot see the definition's stream,
        // and the render simply ignores overrides for unknown definition node ids —
        // an accepted race (definition edited concurrently), not a corruption risk.
        Raise(new BlockOverrideSet(instanceId, definitionNodeId, field, locale, value));
    }

    public void DetachBlockInstance(NodeId instanceId, Node replacement)
    {
        EnsureNotDeleted();
        FindBlockInstance(instanceId);

        // The replacement is validated exactly like an AddNode spec, placed where the
        // instance currently sits — detaching may not produce a tree that AddNode
        // would have rejected.
        var parent = Tree.ParentOf(instanceId);
        EnsureCanPlace(parent, replacement);
        EnsureSpecInternallyValid(replacement);
        EnsureFreshIds(replacement);
        EnsureDepthBudget(parent is null ? 0 : Tree.DepthOf(parent.Id), replacement);
        EnsureNodeBudget(replacement, replacedNodes: 1);

        Raise(new BlockInstanceDetached(instanceId, replacement));
    }

    public void Publish()
    {
        EnsureNotDeleted();
        if (UncommittedEvents.Count > 0)
        {
            // A programmer error, not a user mistake: the version arithmetic below is
            // only sound when the publish event is the sole event of its commit.
            throw new InvalidOperationException("Publish must be dispatched as a standalone command; the aggregate has uncommitted events.");
        }

        if (PublishedVersion == Version)
        {
            throw new DomainException("There are no changes to publish.");
        }

        // Version + 1 is the stream position this publish event will occupy — after
        // commit, PublishedVersion == Version marks the page as fully published.
        Raise(new PagePublished(Version + 1));
    }

    public void Unpublish()
    {
        EnsureNotDeleted();
        if (PublishedVersion is null)
        {
            throw new DomainException("This page is not published.");
        }

        Raise(new PageUnpublished());
    }

    public void Delete()
    {
        EnsureNotDeleted();
        Raise(new PageDeleted());
    }

    // ----------------------------------------------------------------------- fold

    protected override void When(object @event)
    {
        switch (@event)
        {
            case PageCreated e:
                Id = e.PageId;
                SiteId = e.SiteId;
                Slug = ParseStoredSlug(e.Slug);
                Title = LocalizedText.Of(e.InitialLocale, e.Title);
                break;
            case TitleChanged e:
                Title = Title.With(e.Locale, e.Title);
                break;
            case SlugChanged e:
                Slug = ParseStoredSlug(e.Slug);
                break;
            case MetaChanged e:
                MetaTitle = MetaTitle.With(e.Locale, e.MetaTitle ?? string.Empty);
                MetaDescription = MetaDescription.With(e.Locale, e.MetaDescription ?? string.Empty);
                break;
            case NodeAdded e:
                Tree = Tree.Insert(e.ParentId, e.Index, e.Spec);
                break;
            case NodeMoved e:
                Tree = Tree.Move(e.NodeId, e.NewParentId, e.NewIndex);
                break;
            case NodeRemoved e:
                Tree = Tree.Remove(e.NodeId);
                break;
            case NodeDuplicated e:
                var (sourceParentId, sourceIndex) = LocationOf(e.SourceId);
                Tree = Tree.Insert(sourceParentId, sourceIndex + 1, e.Copy);
                break;
            case NodePropsChanged e:
                Tree = Tree.Replace(e.Node.Id, _ => e.Node);
                break;
            case TextChanged e:
                Tree = Tree.Replace(e.NodeId, node => WithTextApplied(node, e.Field, e.Locale, e.Value));
                break;
            case BlockOverrideSet e:
                Tree = Tree.Replace(e.InstanceId, node => ((BlockInstanceNode)node) with
                {
                    Overrides = ((BlockInstanceNode)node).Overrides.With(e.DefinitionNodeId, e.Field, e.Locale, e.Value),
                });
                break;
            case BlockInstanceDetached e:
                var (parentId, index) = LocationOf(e.InstanceId);
                Tree = Tree.Remove(e.InstanceId).Insert(parentId, index, e.Replacement);
                break;
            case PagePublished e:
                PublishedVersion = e.Version;
                break;
            case PageUnpublished:
                PublishedVersion = null;
                break;
            case PageDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"Page cannot fold unknown event {@event.GetType().Name}.");
        }
    }

    // -------------------------------------------------------------------- guards

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("This page has been deleted.");
        }
    }

    private static void EnsureTitleLength(string title)
    {
        if (title.Length > MaxTitleLength)
        {
            throw new DomainException($"Titles are limited to {MaxTitleLength} characters.");
        }
    }

    private static void EnsureValidTextValue(string field, string value)
    {
        if (field == HtmlField)
        {
            if (!CanonicalHtml.TryValidate(value, out var error))
            {
                throw new DomainException(error!);
            }
        }
        else if (value.Length > MaxTextLength)
        {
            throw new DomainException($"Text is limited to {MaxTextLength} characters.");
        }
    }

    private Node? FindTargetParent(NodeId parentId) =>
        parentId.IsRoot
            ? null
            : Tree.Find(parentId) ?? throw new DomainException("The target container no longer exists on this page.");

    private BlockInstanceNode FindBlockInstance(NodeId instanceId) =>
        Tree.Find(instanceId) switch
        {
            BlockInstanceNode instance => instance,
            null => throw new DomainException("The block instance no longer exists on this page."),
            var other => throw new DomainException($"A {other.DisplayName} is not a block instance."),
        };

    private static void EnsureCanPlace(Node? parent, Node child)
    {
        if (!Placement.CanPlace(parent, child))
        {
            throw new DomainException(parent switch
            {
                null => "Only sections can be placed directly on the page.",
                ColumnsNode => "Content goes into a column cell, never onto the columns element itself.",
                SectionNode or StackNode or GridNode => "Sections can only be placed directly on the page, not inside other elements.",
                _ => $"A {parent.DisplayName} cannot contain other elements.",
            });
        }
    }

    // A spec is a whole subtree: placement must hold at every level, and columns must
    // arrive with their managed cells already consistent — the tree never holds a
    // columns element in a half-built state.
    private static void EnsureSpecInternallyValid(Node spec)
    {
        foreach (var node in PageTree.Flatten(spec))
        {
            // Content invariants must hold at EVERY entry point, not just EditText:
            // the publisher renders stored RichText as raw markup, so a node spec that
            // smuggled non-canonical HTML through AddNode/ChangeNodeProps would be a
            // stored-XSS hole. The same rule everywhere is also the point of a
            // reference project — the aggregate owns the invariant, uniformly.
            EnsureNodeContentValid(node);

            if (node is ColumnsNode columns)
            {
                EnsureValidRatios(columns.Ratios);
                if (columns.Children.Count != columns.Ratios.Length)
                {
                    throw new DomainException("A columns element needs exactly one cell per ratio entry.");
                }

                foreach (var cell in columns.Children)
                {
                    if (cell is not StackNode)
                    {
                        throw new DomainException("Column cells must be stacks.");
                    }
                }
            }
            else if (node is IContainerNode container)
            {
                foreach (var child in container.Children)
                {
                    EnsureCanPlace(node, child);
                }
            }
        }
    }

    private static void EnsureNodeContentValid(Node node) => NodeContentRules.Validate(node);

    private static void EnsureValidRatios(ImmutableArray<int> ratios)
    {
        if (ratios.IsDefault || ratios.Length is < MinColumns or > MaxColumns)
        {
            throw new DomainException($"Columns are limited to between {MinColumns} and {MaxColumns} columns.");
        }

        if (ratios.Any(ratio => ratio is < MinRatio or > MaxRatio))
        {
            throw new DomainException($"Column ratios must be between {MinRatio} and {MaxRatio}.");
        }
    }

    private void EnsureFreshIds(Node spec)
    {
        var seen = new HashSet<NodeId>();
        foreach (var node in PageTree.Flatten(spec))
        {
            if (node.Id.IsRoot)
            {
                throw new DomainException("An inserted element uses the reserved root id.");
            }

            if (!seen.Add(node.Id) || Tree.Contains(node.Id))
            {
                throw new DomainException("The inserted content reuses an element id already on this page.");
            }
        }
    }

    private static void EnsureDepthBudget(int parentDepth, Node spec)
    {
        if (parentDepth + PageTree.SubtreeDepth(spec) > Placement.MaxDepth)
        {
            throw new DomainException($"Content can be nested at most {Placement.MaxDepth} levels deep.");
        }
    }

    private void EnsureNodeBudget(Node spec, int replacedNodes)
    {
        if (Tree.Count() - replacedNodes + PageTree.Flatten(spec).Count() > Placement.MaxNodesPerPage)
        {
            throw new DomainException($"A page can hold at most {Placement.MaxNodesPerPage} elements.");
        }
    }

    private static void EnsureIndexInRange(int index, int slotCount)
    {
        if (index < 0 || index > slotCount)
        {
            throw new DomainException($"Position {index} is outside the valid range 0–{slotCount}.");
        }
    }

    // ------------------------------------------------------------------- helpers

    private Node ResolveColumnCells(ColumnsNode current, ColumnsNode replacement)
    {
        EnsureValidRatios(replacement.Ratios);

        // The replacement's children are ignored on purpose: cells are managed by the
        // aggregate, and only the ratio count may grow or shrink them at the tail.
        var cells = current.Children;
        var targetCount = replacement.Ratios.Length;
        for (var i = cells.Count - 1; i >= targetCount; i--)
        {
            if (cells[i] is IContainerNode cell && cell.Children.Count > 0)
            {
                throw new DomainException("A removed column still has content — move it to another column first.");
            }

            cells = cells.RemoveAt(i);
        }

        while (cells.Count < targetCount)
        {
            // Fresh cell ids are minted at decision time and travel in the event.
            cells = cells.Add(new StackNode { Id = NodeId.New() });
        }

        return replacement with { Children = cells };
    }

    private static Node CloneWithFreshIds(Node node)
    {
        var clone = node with { Id = NodeId.New() };
        return clone is IContainerNode container
            ? container.WithChildren(NodeList.Of([.. container.Children.Select(CloneWithFreshIds)]))
            : clone;
    }

    private static LocalizedText? TextFieldOf(Node node, string field) => (node, field) switch
    {
        (HeadingNode heading, TextField) => heading.Text,
        (RichTextNode richText, HtmlField) => richText.Html,
        (ButtonNode button, LabelField) => button.Label,
        (ImageNode image, AltField) => image.Alt,
        (SvgNode svg, AltField) => svg.Alt,
        _ => null,
    };

    private static Node WithTextApplied(Node node, string field, Locale locale, string value) => (node, field) switch
    {
        (HeadingNode heading, TextField) => heading with { Text = heading.Text.With(locale, value) },
        (RichTextNode richText, HtmlField) => richText with { Html = richText.Html.With(locale, value) },
        (ButtonNode button, LabelField) => button with { Label = button.Label.With(locale, value) },
        (ImageNode image, AltField) => image with { Alt = image.Alt.With(locale, value) },
        (SvgNode svg, AltField) => svg with { Alt = svg.Alt.With(locale, value) },
        _ => throw new InvalidOperationException($"'{field}' is not a text field of {node.GetType().Name}."),
    };

    private (NodeId ParentId, int Index) LocationOf(NodeId nodeId)
    {
        var parent = Tree.ParentOf(nodeId);
        var siblings = parent is null ? Tree.Roots : ((IContainerNode)parent).Children;
        return (parent?.Id ?? NodeId.Root, siblings.IndexOf(nodeId));
    }

    private static Slug ParseStoredSlug(string value) =>
        Slug.TryCreate(value, out var slug, out var error)
            ? slug
            : throw new InvalidOperationException($"Stored slug '{value}' is invalid: {error}");
}
