using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Pages.Events;

// The Page aggregate's event union (docs/domain-model.md §2). Events are
// self-contained and deterministic: anything generated at decision time (fresh ids in
// duplicated/detached subtrees, resolved children in props changes) travels in the
// event, so replay never regenerates or re-derives anything.

[EventType("page.created")]
public sealed record PageCreated(PageId PageId, SiteId SiteId, string Slug, Locale InitialLocale, string Title);

[EventType("page.title-changed")]
public sealed record TitleChanged(Locale Locale, string Title);

[EventType("page.slug-changed")]
public sealed record SlugChanged(string Slug);

[EventType("page.meta-changed")]
public sealed record MetaChanged(Locale Locale, string? MetaTitle, string? MetaDescription);

[EventType("page.node-added")]
public sealed record NodeAdded(NodeId ParentId, int Index, Node Spec);

[EventType("page.node-moved")]
public sealed record NodeMoved(NodeId NodeId, NodeId NewParentId, int NewIndex);

[EventType("page.node-removed")]
public sealed record NodeRemoved(NodeId NodeId);

// Copy carries the complete duplicated subtree with ids that were freshly generated
// when the command was decided — replaying the event must not mint new ids.
[EventType("page.node-duplicated")]
public sealed record NodeDuplicated(NodeId SourceId, Node Copy);

// Carries the FINAL node, children included (resolved by the behavior), so the fold
// is a mechanical PageTree.Replace — no merging logic can drift between behavior and
// fold, and column-cell ids created by a ratio change are recorded, not regenerated.
[EventType("page.node-props-changed")]
public sealed record NodePropsChanged(Node Node);

[EventType("page.text-changed")]
public sealed record TextChanged(NodeId NodeId, string Field, Locale Locale, string Value);

// Replaces the page's ENTIRE content with a given set of section roots — the primitive
// behind "discard unpublished changes / restore last published". Self-contained: the
// full target tree travels in the event, so the fold is a mechanical PageTree swap and
// replay never depends on any other stream (e.g. the published snapshot it came from).
[EventType("page.content-restored")]
public sealed record PageContentRestored(NodeList Roots);

// Null Value clears the override.
[EventType("page.block-override-set")]
public sealed record BlockOverrideSet(NodeId InstanceId, NodeId DefinitionNodeId, string Field, Locale Locale, string? Value);

// Replacement is the resolved definition subtree with fresh ids, generated at
// decision time like a duplication; the fold swaps it in at the instance's position.
[EventType("page.block-instance-detached")]
public sealed record BlockInstanceDetached(NodeId InstanceId, Node Replacement);

// Version is the stream position the publish event itself occupies: after commit,
// PublishedVersion == aggregate Version exactly when nothing changed since the last
// publish — which is both the no-op guard and the PageList 'Modified' badge test.
[EventType("page.published")]
public sealed record PagePublished(long Version);

[EventType("page.unpublished")]
public sealed record PageUnpublished;

[EventType("page.deleted")]
public sealed record PageDeleted;
