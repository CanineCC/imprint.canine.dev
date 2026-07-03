using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.MoveNode;

namespace Imprint.Authoring.Tests.Features;

public sealed class SlotPlannerTests
{
    private static readonly NodeId SectionA = NodeId.New();
    private static readonly NodeId SectionB = NodeId.New();
    private static readonly NodeId StackA = NodeId.New();
    private static readonly NodeId HeadingA = NodeId.New();
    private static readonly NodeId TextA = NodeId.New();
    private static readonly NodeId ColumnsB = NodeId.New();
    private static readonly NodeId CellB1 = NodeId.New();
    private static readonly NodeId CellB2 = NodeId.New();

    /// <summary>
    /// Section A [ Stack A [ Heading A, Text A ] ]
    /// Section B [ Columns B [ Cell B1 (empty), Cell B2 (empty) ] ]
    /// </summary>
    private static PageTree Tree() => new(NodeList.Of(
        new SectionNode
        {
            Id = SectionA,
            Children = NodeList.Of(new StackNode
            {
                Id = StackA,
                Children = NodeList.Of(
                    new HeadingNode { Id = HeadingA, Level = 2 },
                    new RichTextNode { Id = TextA }),
            }),
        },
        new SectionNode
        {
            Id = SectionB,
            Children = NodeList.Of(new ColumnsNode
            {
                Id = ColumnsB,
                Ratios = [1, 1],
                Children = NodeList.Of(
                    new StackNode { Id = CellB1 },
                    new StackNode { Id = CellB2 }),
            }),
        }));

    [Fact]
    public void Section_drag_offers_only_root_slots()
    {
        var plan = SlotPlanner.Plan(Tree(), SectionA)!;

        Assert.All(plan.Slots, slot => Assert.True(slot.ParentId.IsRoot));
        // Two root positions exist for two sections; the current one is excluded.
        var slot = Assert.Single(plan.Slots);
        Assert.Equal(1, slot.Index);
        Assert.Equal(SectionB, slot.AnchorId);
        Assert.Equal(SlotPlanner.SlotEdge.After, slot.Edge);
    }

    [Fact]
    public void Content_drag_offers_container_slots_but_never_root_or_columns()
    {
        var plan = SlotPlanner.Plan(Tree(), HeadingA)!;

        Assert.DoesNotContain(plan.Slots, slot => slot.ParentId.IsRoot);
        Assert.DoesNotContain(plan.Slots, slot => slot.ParentId == ColumnsB);

        // The empty column cells are valid 'into' targets.
        Assert.Contains(plan.Slots, slot => slot.ParentId == CellB1 && slot.Edge == SlotPlanner.SlotEdge.Into);
        Assert.Contains(plan.Slots, slot => slot.ParentId == CellB2 && slot.Edge == SlotPlanner.SlotEdge.Into);

        // Within its own stack: position 0 is 'before Text A'... position 1 (its own) excluded.
        var own = plan.Slots.Where(slot => slot.ParentId == StackA).ToList();
        Assert.Single(own);
        Assert.Equal(1, own[0].Index);
        Assert.Equal(TextA, own[0].AnchorId);

        // Section B accepts it directly too.
        Assert.Contains(plan.Slots, slot => slot.ParentId == SectionB);
    }

    [Fact]
    public void Anchors_use_the_after_removal_frame()
    {
        var plan = SlotPlanner.Plan(Tree(), TextA)!;
        var own = plan.Slots.Where(slot => slot.ParentId == StackA).ToList();

        // Text A is at index 1 of 2; after removal the only slot in its stack is
        // index 0 = before Heading A.
        var slot = Assert.Single(own);
        Assert.Equal(0, slot.Index);
        Assert.Equal(HeadingA, slot.AnchorId);
        Assert.Equal(SlotPlanner.SlotEdge.Before, slot.Edge);
    }

    [Fact]
    public void Dragging_a_container_excludes_its_own_subtree()
    {
        var plan = SlotPlanner.Plan(Tree(), StackA)!;

        Assert.DoesNotContain(plan.Slots, slot => slot.ParentId == StackA);
        // Section A becomes empty in the after-removal frame: an 'into' slot.
        Assert.Contains(plan.Slots, slot =>
            slot.ParentId == SectionA && slot.Edge == SlotPlanner.SlotEdge.Into);
    }

    [Fact]
    public void Managed_column_cells_cannot_be_dragged()
    {
        Assert.Null(SlotPlanner.Plan(Tree(), CellB1));
    }

    [Fact]
    public void Unknown_node_yields_no_plan()
    {
        Assert.Null(SlotPlanner.Plan(Tree(), NodeId.New()));
    }

    [Fact]
    public void Slot_ids_are_dense_and_unique()
    {
        var plan = SlotPlanner.Plan(Tree(), HeadingA)!;
        Assert.Equal(Enumerable.Range(0, plan.Slots.Count), plan.Slots.Select(slot => slot.SlotId));
    }

    [Fact]
    public void Depth_cap_excludes_too_deep_targets()
    {
        // Build a chain of stacks at max depth and try to drop another stack into it.
        var deepest = NodeId.New();
        Node chain = new StackNode { Id = deepest };
        for (var i = 0; i < Placement.MaxDepth - 2; i++)
        {
            chain = new StackNode { Id = NodeId.New(), Children = NodeList.Of(chain) };
        }

        var loose = NodeId.New();
        var tree = new PageTree(NodeList.Of(
            new SectionNode { Id = NodeId.New(), Children = NodeList.Of(chain) },
            new SectionNode { Id = NodeId.New(), Children = NodeList.Of(new StackNode { Id = loose }) }));

        var plan = SlotPlanner.Plan(tree, loose)!;
        Assert.DoesNotContain(plan.Slots, slot => slot.ParentId == deepest);
    }
}
