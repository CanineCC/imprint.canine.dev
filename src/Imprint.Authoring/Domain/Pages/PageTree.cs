namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The immutable node tree of a page (or block definition). Purely mechanical
/// structure operations — *rules* about what may go where live in <see cref="Placement"/>
/// and are enforced by the aggregate before it raises an event. By the time a tree
/// operation runs (folding an event), failure means a corrupted stream, so mechanical
/// impossibilities throw <see cref="InvalidOperationException"/>, never DomainException.
/// </summary>
public sealed record PageTree(NodeList Roots)
{
    public static readonly PageTree Empty = new(NodeList.Empty);

    public Node? Find(NodeId id) => FindIn(Roots, id);

    public bool Contains(NodeId id) => Find(id) is not null;

    /// <summary>The parent of a node — null when the node is a root section (its parent is the Root sentinel).</summary>
    public Node? ParentOf(NodeId id)
    {
        Node? result = null;

        bool Walk(Node? parent, NodeList children)
        {
            foreach (var child in children)
            {
                if (child.Id == id)
                {
                    result = parent;
                    return true;
                }

                if (child is IContainerNode container && Walk(child, container.Children))
                {
                    return true;
                }
            }

            return false;
        }

        return Walk(null, Roots) ? result : throw new InvalidOperationException($"Node {id} is not in the tree.");
    }

    /// <summary>All nodes in document order (depth-first).</summary>
    public IEnumerable<Node> All()
    {
        static IEnumerable<Node> Walk(NodeList nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                if (node is IContainerNode container)
                {
                    foreach (var descendant in Walk(container.Children))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        return Walk(Roots);
    }

    public int Count() => All().Count();

    public bool IsDescendantOf(NodeId candidate, NodeId ancestor)
    {
        if (Find(ancestor) is not IContainerNode container)
        {
            return false;
        }

        return FindIn(container.Children, candidate) is not null;
    }

    /// <summary>Depth of a node: root sections are depth 1.</summary>
    public int DepthOf(NodeId id)
    {
        var depth = 1;
        for (var parent = ParentOf(id); parent is not null; parent = ParentOf(parent.Id))
        {
            depth++;
        }

        return depth;
    }

    /// <summary>Deepest nesting level inside a subtree (a leaf spec has depth 1).</summary>
    public static int SubtreeDepth(Node node) =>
        node is IContainerNode container && container.Children.Count > 0
            ? 1 + container.Children.Max(SubtreeDepth)
            : 1;

    public static IEnumerable<Node> Flatten(Node node)
    {
        yield return node;
        if (node is IContainerNode container)
        {
            foreach (var child in container.Children)
            {
                foreach (var descendant in Flatten(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    public PageTree Insert(NodeId parentId, int index, Node node)
    {
        if (parentId.IsRoot)
        {
            return new PageTree(Roots.Insert(ClampCheck(index, Roots.Count), node));
        }

        return Mutate(parentId, (container, children) =>
            container.WithChildren(children.Insert(ClampCheck(index, children.Count), node)));
    }

    public PageTree Remove(NodeId id)
    {
        var (tree, _) = Take(id);
        return tree;
    }

    /// <summary>
    /// Moves a node. <c>newIndex</c> is the index in the target parent's child list
    /// *after* the node has been removed from its old position — the same convention
    /// the drag-and-drop slot planner uses, so an event's index is unambiguous.
    /// </summary>
    public PageTree Move(NodeId id, NodeId newParentId, int newIndex)
    {
        var (without, node) = Take(id);
        return without.Insert(newParentId, newIndex, node);
    }

    /// <summary>Replaces a node in place via a transform. The transform must preserve the id.</summary>
    public PageTree Replace(NodeId id, Func<Node, Node> transform)
    {
        static NodeList WalkChildren(NodeList children, NodeId id, Func<Node, Node> transform, ref bool found)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Id == id)
                {
                    var replacement = transform(child);
                    if (replacement.Id != id)
                    {
                        throw new InvalidOperationException("Replace must preserve the node id.");
                    }

                    found = true;
                    return children.SetItem(i, replacement);
                }

                if (child is IContainerNode container)
                {
                    var updated = WalkChildren(container.Children, id, transform, ref found);
                    if (found)
                    {
                        return children.SetItem(i, container.WithChildren(updated));
                    }
                }
            }

            return children;
        }

        var found = false;
        var roots = WalkChildren(Roots, id, transform, ref found);
        return found ? new PageTree(roots) : throw new InvalidOperationException($"Node {id} is not in the tree.");
    }

    // ------------------------------------------------------------------ internals

    private (PageTree Tree, Node Node) Take(NodeId id)
    {
        Node? taken = null;

        NodeList Walk(NodeList children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Id == id)
                {
                    taken = child;
                    return children.RemoveAt(i);
                }

                if (child is IContainerNode container)
                {
                    var updated = Walk(container.Children);
                    if (taken is not null)
                    {
                        return children.SetItem(i, container.WithChildren(updated));
                    }
                }
            }

            return children;
        }

        var roots = Walk(Roots);
        return taken is not null
            ? (new PageTree(roots), taken)
            : throw new InvalidOperationException($"Node {id} is not in the tree.");
    }

    private PageTree Mutate(NodeId containerId, Func<IContainerNode, NodeList, Node> mutate)
    {
        var found = false;

        NodeList Walk(NodeList children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Id == containerId)
                {
                    if (children[i] is not IContainerNode container)
                    {
                        throw new InvalidOperationException($"Node {containerId} is not a container.");
                    }

                    found = true;
                    return children.SetItem(i, mutate(container, container.Children));
                }

                if (children[i] is IContainerNode walkable)
                {
                    var updated = Walk(walkable.Children);
                    if (found)
                    {
                        return children.SetItem(i, walkable.WithChildren(updated));
                    }
                }
            }

            return children;
        }

        var roots = Walk(Roots);
        return found ? new PageTree(roots) : throw new InvalidOperationException($"Node {containerId} is not in the tree.");
    }

    private static Node? FindIn(NodeList nodes, NodeId id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
            {
                return node;
            }

            if (node is IContainerNode container && FindIn(container.Children, id) is { } hit)
            {
                return hit;
            }
        }

        return null;
    }

    private static int ClampCheck(int index, int count) =>
        index >= 0 && index <= count
            ? index
            : throw new InvalidOperationException($"Index {index} outside 0..{count}.");
}
