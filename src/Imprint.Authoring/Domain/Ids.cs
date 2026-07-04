using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain;

// Strongly-typed ids. `Stream` gives the event-store stream name; `Compact` (guid "N")
// is the wire/DOM form used in data-node-id attributes and file names.

public readonly record struct SiteId(Guid Value) : IGuidId<SiteId>
{
    // The stable identity a legacy, single-site install's content is adopted under. Every
    // page.created ever written already carries a SiteId (the field has existed since the
    // Page aggregate's first commit), so this is a defensive floor: a stream so old — or
    // hand-authored — that its SiteId is absent (deserializes to the empty Guid) is
    // upcast to this one well-known site instead of a phantom 'site-000…0'. Fixed and
    // never random so the upcast is deterministic across replays and machines.
    public static readonly SiteId Default = new(new Guid("00000000-0000-0000-0000-0000000051e0"));

    public static SiteId New() => new(Guid.NewGuid());
    public static SiteId From(Guid value) => new(value);
    public bool IsEmpty => Value == Guid.Empty;
    public string Stream => $"site-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}

public readonly record struct PageId(Guid Value) : IGuidId<PageId>
{
    public static PageId New() => new(Guid.NewGuid());
    public static PageId From(Guid value) => new(value);
    public string Stream => $"page-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}

public readonly record struct AssetId(Guid Value) : IGuidId<AssetId>
{
    public static AssetId New() => new(Guid.NewGuid());
    public static AssetId From(Guid value) => new(value);
    public string Stream => $"asset-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}

public readonly record struct BlockDefinitionId(Guid Value) : IGuidId<BlockDefinitionId>
{
    public static BlockDefinitionId New() => new(Guid.NewGuid());
    public static BlockDefinitionId From(Guid value) => new(value);
    public string Stream => $"block-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}

/// <summary>
/// Identifies a node within a page (or block definition) tree. <see cref="Root"/> is
/// the sentinel parent of top-level sections — it is never the id of an actual node.
/// </summary>
public readonly record struct NodeId(Guid Value) : IGuidId<NodeId>
{
    public static readonly NodeId Root = new(Guid.Empty);

    public static NodeId New() => new(Guid.NewGuid());
    public static NodeId From(Guid value) => new(value);
    public bool IsRoot => Value == Guid.Empty;
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;

    public static bool TryParse(string? compact, out NodeId id)
    {
        if (Guid.TryParseExact(compact, "N", out var guid) || Guid.TryParse(compact, out guid))
        {
            id = new NodeId(guid);
            return true;
        }

        id = default;
        return false;
    }
}
