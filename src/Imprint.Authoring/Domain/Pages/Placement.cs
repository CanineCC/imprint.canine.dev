namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The single source of placement truth, shared by the Page aggregate (enforcing
/// invariants) and the editor's drag-and-drop slot planner (offering only valid
/// targets) — which is how an invalid drop becomes unrepresentable in the UI without
/// duplicating rules.
/// </summary>
public static class Placement
{
    public const int MaxDepth = 8;
    public const int MaxNodesPerPage = 500;

    /// <summary>May <paramref name="child"/> be a direct child of <paramref name="parent"/> (null = page root)?</summary>
    public static bool CanPlace(Node? parent, Node child) => parent switch
    {
        // The page root holds sections and nothing else.
        null => child is SectionNode,

        // Columns manage their own cells; content goes into the cells, never the
        // columns node itself (see CellsAreManaged).
        ColumnsNode => false,

        // Other containers hold anything except sections.
        SectionNode or StackNode or GridNode => child is not SectionNode,

        // Content nodes are leaves.
        _ => false,
    };

    /// <summary>
    /// Column cells (the implicit stacks directly under a ColumnsNode) cannot be
    /// individually moved, removed or duplicated — they are part of the columns node.
    /// </summary>
    public static bool IsManagedCell(PageTree tree, NodeId id) =>
        tree.ParentOf(id) is ColumnsNode;

    /// <summary>Containers a dragged node may not enter: itself and its own subtree.</summary>
    public static bool WouldCreateCycle(PageTree tree, NodeId dragged, NodeId targetParent) =>
        !targetParent.IsRoot && (dragged == targetParent || tree.IsDescendantOf(targetParent, dragged));
}
