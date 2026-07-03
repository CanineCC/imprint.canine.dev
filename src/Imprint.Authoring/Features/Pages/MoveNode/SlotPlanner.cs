using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Features.Pages.MoveNode;

/// <summary>
/// Computes every valid drop target for a drag — from the same <see cref="Placement"/>
/// rules the Page aggregate enforces, which is how an invalid drop becomes
/// unrepresentable in the UI rather than an error after the fact. The editor sends
/// this plan to the canvas JS once at drag start; tracking happens client-side at
/// 60 fps with no further round-trips (docs/editor-ux.md §3).
/// </summary>
public static class SlotPlanner
{
    /// <summary>A valid insertion point. <c>Index</c> uses after-removal semantics — identical to <c>PageTree.Move</c>.</summary>
    public sealed record Slot(
        int SlotId,
        NodeId ParentId,
        int Index,
        NodeId AnchorId,
        SlotEdge Edge,
        SlotOrientation Orientation);

    public enum SlotEdge { Before, After, Into }

    public enum SlotOrientation { Vertical, Horizontal }

    public sealed record DragPlan(NodeId NodeId, string DragLabel, IReadOnlyList<Slot> Slots);

    public static DragPlan? Plan(PageTree tree, NodeId draggedId)
    {
        var dragged = tree.Find(draggedId);
        if (dragged is null || Placement.IsManagedCell(tree, draggedId))
        {
            return null;
        }

        var draggedDepth = PageTree.SubtreeDepth(dragged);
        var currentParent = tree.ParentOf(draggedId);
        var currentParentId = currentParent?.Id ?? NodeId.Root;
        var currentIndex = IndexWithin(tree, currentParentId, draggedId);

        var slots = new List<Slot>();

        // The page root is a container too (for sections).
        CollectSlots(tree, parent: null, parentId: NodeId.Root, parentDepth: 0, tree.Roots);

        foreach (var node in tree.All())
        {
            if (node is IContainerNode container)
            {
                CollectSlots(tree, node, node.Id, tree.DepthOf(node.Id), container.Children);
            }
        }

        return new DragPlan(draggedId, dragged.DisplayName, slots);

        void CollectSlots(PageTree pageTree, Node? parent, NodeId parentId, int parentDepth, NodeList children)
        {
            if (!Placement.CanPlace(parent, dragged) ||
                Placement.WouldCreateCycle(pageTree, draggedId, parentId) ||
                parentDepth + draggedDepth > Placement.MaxDepth)
            {
                return;
            }

            // Anchor geometry is measured against the DOM with the dragged node still
            // present, but indexes use after-removal semantics — so the visible list
            // (children minus the dragged node) is the shared frame of reference.
            var visible = children.Where(child => child.Id != draggedId).ToList();

            if (visible.Count == 0)
            {
                slots.Add(new Slot(slots.Count, parentId, 0,
                    parentId.IsRoot ? NodeId.Root : parentId, SlotEdge.Into, SlotOrientation.Vertical));
                return;
            }

            var orientation = parent is GridNode ? SlotOrientation.Horizontal : SlotOrientation.Vertical;
            var isCurrentParent = parentId == currentParentId;

            for (var index = 0; index <= visible.Count; index++)
            {
                // Dropping back where the node already sits is a no-op, not a target.
                if (isCurrentParent && index == currentIndex)
                {
                    continue;
                }

                slots.Add(index < visible.Count
                    ? new Slot(slots.Count, parentId, index, visible[index].Id, SlotEdge.Before, orientation)
                    : new Slot(slots.Count, parentId, index, visible[^1].Id, SlotEdge.After, orientation));
            }
        }
    }

    /// <summary>The dragged node's index in the after-removal frame (= its index among its siblings).</summary>
    private static int IndexWithin(PageTree tree, NodeId parentId, NodeId nodeId)
    {
        var siblings = parentId.IsRoot
            ? tree.Roots
            : ((IContainerNode)tree.Find(parentId)!).Children;
        return siblings.IndexOf(nodeId);
    }
}
