using Imprint.Authoring.Domain.Blocks.Events;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Blocks;

/// <summary>
/// A reusable "symbol": a named node subtree that pages place via
/// <see cref="BlockInstanceNode"/>. Editing the definition updates every instance.
/// </summary>
public sealed class BlockDefinition : AggregateRoot
{
    // A block is a fragment, not a page — a fraction of the page budget is plenty.
    private const int MaxSpecNodes = 200;
    private const int MaxNameLength = 100;

    // An instance always sits inside a section (depth ≥ 2 on the page), so the spec
    // must leave at least one level of the page's depth budget for that section.
    private const int MaxSpecDepth = Placement.MaxDepth - 1;

    public BlockDefinitionId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>Set by <c>block.defined</c>, the stream's first event — never null on a loaded aggregate.</summary>
    public Node Spec { get; private set; } = null!;

    public bool IsDeleted { get; private set; }

    public override string StreamId => Id.Stream;

    public static BlockDefinition Define(BlockDefinitionId id, string name, Node spec)
    {
        ValidateName(name);
        ValidateSpec(spec);
        var definition = new BlockDefinition();
        definition.Raise(new BlockDefined(id, name, spec));
        return definition;
    }

    public void Rename(string name)
    {
        EnsureNotDeleted();
        ValidateName(name);
        // A no-op rename would be noise in the history.
        if (name == Name)
        {
            return;
        }

        Raise(new BlockRenamed(name));
    }

    public void ChangeSpec(Node spec)
    {
        EnsureNotDeleted();
        ValidateSpec(spec);
        Raise(new BlockSpecChanged(spec));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        // Pages with live instances are protected by the slice via the BlockUsage read
        // model, not here: a page could place a new instance in the same instant the
        // check passes. Accepted race — an orphaned instance renders as a visible
        // "missing block" placeholder in the editor, never a crash.
        Raise(new BlockDeleted());
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case BlockDefined e:
                Id = e.BlockDefinitionId;
                Name = e.Name;
                Spec = e.Spec;
                break;
            case BlockRenamed e:
                Name = e.Name;
                break;
            case BlockSpecChanged e:
                Spec = e.Spec;
                break;
            case BlockDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"BlockDefinition cannot fold unknown event {@event.GetType().Name}.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A block needs a name.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Block names are limited to {MaxNameLength} characters.");
        }
    }

    private static void ValidateSpec(Node spec)
    {
        // The spec root renders wherever an instance is placed, and instances live
        // inside sections — so the root must be placeable there. Placement is the
        // single source of that truth; a throwaway section stands in for the parent.
        if (!Placement.CanPlace(new SectionNode { Id = NodeId.Root }, spec))
        {
            throw new DomainException("A block cannot be a section — start from a layout or content node instead.");
        }

        var nodes = PageTree.Flatten(spec).ToList();
        if (nodes.Count > MaxSpecNodes)
        {
            throw new DomainException($"A block is limited to {MaxSpecNodes} nodes; this one has {nodes.Count}.");
        }

        if (PageTree.SubtreeDepth(spec) > MaxSpecDepth)
        {
            throw new DomainException($"A block can nest at most {MaxSpecDepth} levels deep.");
        }

        var ids = new HashSet<NodeId>();
        foreach (var node in nodes)
        {
            if (!ids.Add(node.Id))
            {
                throw new DomainException("Every node in a block must have its own id — a duplicate would make edits ambiguous.");
            }

            // No symbols inside symbols: a definition referencing a definition could
            // form a cycle (A contains B contains A) and render forever.
            if (node is BlockInstanceNode)
            {
                throw new DomainException("A block cannot contain another block.");
            }
        }

        ValidatePlacement(spec);
    }

    // Deliberately duplicates the Page aggregate's ~15-line subtree walk: sharing a
    // validator would entangle two aggregates' change cadence over trivially little
    // code, and their rules already differ at the root (pages demand sections, blocks
    // forbid them).
    private static void ValidatePlacement(Node node)
    {
        if (node is ColumnsNode columns)
        {
            if (columns.Ratios.Length is < 2 or > 4)
            {
                throw new DomainException("Columns must have between 2 and 4 columns.");
            }

            if (columns.Ratios.Any(r => r is < 1 or > 3))
            {
                throw new DomainException("Column ratios must be between 1 and 3.");
            }

            if (columns.Children.Count != columns.Ratios.Length)
            {
                throw new DomainException("Columns must have exactly one cell per column.");
            }

            if (columns.Children.Any(cell => cell is not StackNode))
            {
                throw new DomainException("Column cells must be stacks.");
            }
        }
        else if (node is IContainerNode container)
        {
            foreach (var child in container.Children)
            {
                if (!Placement.CanPlace(node, child))
                {
                    throw new DomainException($"A {child.DisplayName} cannot be placed inside a {node.DisplayName}.");
                }
            }
        }

        if (node is IContainerNode walkable)
        {
            foreach (var child in walkable.Children)
            {
                ValidatePlacement(child);
            }
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException($"The block '{Name}' has been deleted.");
        }
    }
}
