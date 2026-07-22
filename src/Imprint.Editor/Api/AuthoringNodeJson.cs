using System.Collections.Immutable;
using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Editor.Api;

/// <summary>
/// The wire form of a page node for the headless surfaces (the authoring API and the MCP):
/// one flat, self-describing JSON object per node type — read out by the tree endpoint and
/// read in by add-node / set-props.
/// </summary>
/// <remarks>
/// It is deliberately NOT the domain's own polymorphic serialization. That form is the
/// persisted event shape; binding it straight off the wire would (a) let a caller mint node
/// ids — the aggregate's job — and (b) freeze the event schema into a public API. This
/// mapper is the translation layer, so the two evolve independently, ids are always minted
/// here, and a bad spec fails with a sentence a caller can act on rather than a binder error.
///
/// Text fields (<c>text</c>, <c>html</c>, <c>label</c>, <c>alt</c>) read out as a
/// locale → value object and read in as EITHER that object or a bare string (which means
/// "the default locale"), because a single-locale site is the common case and should not
/// have to spell the locale out on every call.
/// </remarks>
public static class AuthoringNodeJson
{
    /// <summary>The type-specific props of one node, for the tree endpoint.</summary>
    public static Dictionary<string, object?> Describe(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var props = new Dictionary<string, object?>(StringComparer.Ordinal);
        switch (node)
        {
            case SectionNode section:
                props["width"] = section.Width.ToString();
                props["background"] = section.Background.ToString();
                props["padding"] = section.Padding.ToString();
                props["appearance"] = section.Appearance.ToString();
                props["anchor"] = section.Anchor;
                break;

            case StackNode stack:
                props["gap"] = stack.Gap.ToString();
                props["align"] = stack.Align.ToString();
                break;

            case ColumnsNode columns:
                props["ratios"] = columns.Ratios.ToArray();
                props["collapseBelow"] = (int)columns.CollapseBelow;
                props["gap"] = columns.Gap.ToString();
                break;

            case GridNode grid:
                props["minItemPx"] = grid.MinItemPx;
                props["gap"] = grid.Gap.ToString();
                break;

            case HeadingNode heading:
                props["level"] = heading.Level;
                props["text"] = Localized(heading.Text);
                break;

            case RichTextNode richText:
                props["html"] = Localized(richText.Html);
                break;

            case ButtonNode button:
                props["label"] = Localized(button.Label);
                props["variant"] = button.Variant.ToString();
                props["link"] = DescribeLink(button.LinkTo);
                break;

            case ImageNode image:
                props["assetId"] = image.AssetId?.Compact;
                props["alt"] = Localized(image.Alt);
                props["aspect"] = image.Aspect.ToString();
                props["rounded"] = image.Rounded;
                break;

            case VideoNode video:
                props["assetId"] = video.AssetId?.Compact;
                props["mode"] = video.Mode.ToString();
                break;

            case SvgNode svg:
                props["assetId"] = svg.AssetId?.Compact;
                props["maxWidthPx"] = svg.MaxWidthPx;
                props["alt"] = Localized(svg.Alt);
                break;

            case SpacerNode spacer:
                props["size"] = spacer.Size.ToString();
                break;

            case WidgetNode widget:
                props["tag"] = widget.Tag;
                props["props"] = widget.Props.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
                break;

            case BlockInstanceNode instance:
                props["definitionId"] = instance.DefinitionId.Compact;
                props["overrides"] = instance.Overrides.Entries
                    .Select(e => (object)new { nodeId = e.DefinitionNodeId.Compact, field = e.Field, locale = e.Locale.Value, value = e.Value })
                    .ToList();
                break;
        }

        return props;
    }

    /// <summary>
    /// Build a fresh node (and its whole subtree) from a spec. Every id is minted here —
    /// a caller never supplies one, so an add can never collide with or hijack an existing node.
    /// </summary>
    public static bool TryParse(JsonElement spec, Locale defaultLocale, out Node node, out string error)
    {
        try
        {
            node = ParseNode(spec, defaultLocale);
            error = string.Empty;
            return true;
        }
        catch (SpecException ex)
        {
            node = null!;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Apply the props present in <paramref name="patch"/> to <paramref name="current"/>,
    /// leaving everything else (and the node's children, which the aggregate re-resolves)
    /// as it is. A partial patch is the useful shape: "make this section Wide" should not
    /// require restating its background, padding and appearance.
    /// </summary>
    public static bool TryApply(Node current, JsonElement patch, Locale defaultLocale, out Node replacement, out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        try
        {
            replacement = ApplyProps(current, patch, defaultLocale);
            error = string.Empty;
            return true;
        }
        catch (SpecException ex)
        {
            replacement = null!;
            error = ex.Message;
            return false;
        }
    }

    // ------------------------------------------------------------------------ parse

    private static Node ParseNode(JsonElement spec, Locale locale)
    {
        if (spec.ValueKind != JsonValueKind.Object)
        {
            throw new SpecException("A node spec must be a JSON object.");
        }

        var type = (String(spec, "type") ?? throw new SpecException("A node spec needs a 'type'.")).Trim().ToLowerInvariant();
        var id = NodeId.New();
        var children = ParseChildren(spec, locale);

        return type switch
        {
            "section" => new SectionNode
            {
                Id = id,
                Width = Enum(spec, "width", SectionWidth.Normal),
                Background = Enum(spec, "background", SectionBackground.None),
                Padding = Enum(spec, "padding", SectionPadding.Normal),
                Appearance = Enum(spec, "appearance", SectionAppearance.Plain),
                Anchor = String(spec, "anchor"),
                Children = children,
            },
            "stack" => new StackNode
            {
                Id = id,
                Gap = Enum(spec, "gap", Gap.Normal),
                Align = Enum(spec, "align", StackAlign.Start),
                Children = children,
            },
            "columns" => ParseColumns(spec, id, locale),
            "grid" => new GridNode
            {
                Id = id,
                MinItemPx = Int(spec, "minItemPx") ?? 280,
                Gap = Enum(spec, "gap", Gap.Normal),
                Children = children,
            },
            "heading" => new HeadingNode
            {
                Id = id,
                Level = Int(spec, "level") ?? 2,
                Text = LocalizedOf(spec, "text", locale),
            },
            "richtext" or "text" => new RichTextNode
            {
                Id = id,
                Html = LocalizedOf(spec, "html", locale),
            },
            "button" => new ButtonNode
            {
                Id = id,
                Label = LocalizedOf(spec, "label", locale),
                Variant = Enum(spec, "variant", ButtonVariant.Primary),
                LinkTo = ParseLink(spec),
            },
            "image" => new ImageNode
            {
                Id = id,
                AssetId = Asset(spec, "assetId"),
                Alt = LocalizedOf(spec, "alt", locale),
                Aspect = Enum(spec, "aspect", ImageAspect.Natural),
                Rounded = Bool(spec, "rounded") ?? false,
            },
            "video" => new VideoNode
            {
                Id = id,
                AssetId = Asset(spec, "assetId"),
                Mode = Enum(spec, "mode", VideoMode.Player),
            },
            "svg" or "graphic" => new SvgNode
            {
                Id = id,
                AssetId = Asset(spec, "assetId"),
                MaxWidthPx = Int(spec, "maxWidthPx"),
                Alt = LocalizedOf(spec, "alt", locale),
            },
            "divider" => new DividerNode { Id = id },
            "spacer" => new SpacerNode { Id = id, Size = Enum(spec, "size", SpacerSize.Medium) },
            "widget" => new WidgetNode
            {
                Id = id,
                Tag = String(spec, "tag") ?? throw new SpecException("A widget spec needs a 'tag'."),
                Props = ParseWidgetProps(spec),
            },
            _ => throw new SpecException($"'{type}' is not a node type. Use section, stack, columns, grid, heading, richtext, button, image, video, svg, divider, spacer or widget."),
        };
    }

    private static ColumnsNode ParseColumns(JsonElement spec, NodeId id, Locale locale)
    {
        // Cells are structural: the aggregate creates one stack per ratio and rejects a
        // mismatch, so a caller declares the shape (ratios) and fills the cells afterwards.
        var ratios = spec.TryGetProperty("ratios", out var r) && r.ValueKind == JsonValueKind.Array
            ? r.EnumerateArray().Select(x => x.TryGetInt32(out var v) ? v : throw new SpecException("Column ratios must be whole numbers.")).ToImmutableArray()
            : [1, 1];

        var cells = ratios.Select(_ => (Node)new StackNode { Id = NodeId.New() }).ToArray();
        return new ColumnsNode
        {
            Id = id,
            Ratios = ratios,
            CollapseBelow = ParseBreakpoint(spec),
            Gap = Enum(spec, "gap", Gap.Normal),
            Children = NodeList.Of(cells),
        };
    }

    private static CollapseBreakpoint ParseBreakpoint(JsonElement spec)
    {
        if (Int(spec, "collapseBelow") is not { } px)
        {
            return CollapseBreakpoint.Px640;
        }

        return px switch
        {
            480 => CollapseBreakpoint.Px480,
            640 => CollapseBreakpoint.Px640,
            768 => CollapseBreakpoint.Px768,
            _ => throw new SpecException("collapseBelow must be 480, 640 or 768."),
        };
    }

    private static NodeList ParseChildren(JsonElement spec, Locale locale)
    {
        if (!spec.TryGetProperty("children", out var children) || children.ValueKind == JsonValueKind.Null)
        {
            return NodeList.Empty;
        }

        if (children.ValueKind != JsonValueKind.Array)
        {
            throw new SpecException("'children' must be an array of node specs.");
        }

        return NodeList.Of([.. children.EnumerateArray().Select(child => ParseNode(child, locale))]);
    }

    private static PropBag ParseWidgetProps(JsonElement spec)
    {
        if (!spec.TryGetProperty("props", out var props) || props.ValueKind != JsonValueKind.Object)
        {
            return PropBag.Empty;
        }

        return PropBag.Of(props.EnumerateObject().Select(p => new KeyValuePair<string, string>(
            p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? string.Empty : p.Value.ToString())));
    }

    private static Link? ParseLink(JsonElement spec)
    {
        // Accept the flat shape a caller reaches for first (href / pageId), and the
        // read-back shape ({"link":{"kind":…}}) so a tree read round-trips.
        if (spec.TryGetProperty("link", out var link) && link.ValueKind == JsonValueKind.Object)
        {
            return ParseLink(link);
        }

        if (String(spec, "href") is { Length: > 0 } href)
        {
            if (!CanonicalHtml.IsAllowedHref(href))
            {
                throw new SpecException("A link must be https, http, mailto or a page reference.");
            }

            return new ExternalLink(href);
        }

        if (String(spec, "url") is { Length: > 0 } url)
        {
            if (!CanonicalHtml.IsAllowedHref(url))
            {
                throw new SpecException("A link must be https, http, mailto or a page reference.");
            }

            return new ExternalLink(url);
        }

        if (String(spec, "pageId") is { Length: > 0 } page)
        {
            if (!Guid.TryParseExact(page, "N", out var guid) && !Guid.TryParse(page, out guid))
            {
                throw new SpecException("pageId is not a valid page id.");
            }

            return new PageLink(PageId.From(guid));
        }

        return null;
    }

    // ------------------------------------------------------------------------ patch

    private static Node ApplyProps(Node current, JsonElement patch, Locale locale)
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            throw new SpecException("Props must be a JSON object.");
        }

        return current switch
        {
            SectionNode section => section with
            {
                Width = Enum(patch, "width", section.Width),
                Background = Enum(patch, "background", section.Background),
                Padding = Enum(patch, "padding", section.Padding),
                Appearance = Enum(patch, "appearance", section.Appearance),
                Anchor = Has(patch, "anchor") ? String(patch, "anchor") : section.Anchor,
            },
            StackNode stack => stack with
            {
                Gap = Enum(patch, "gap", stack.Gap),
                Align = Enum(patch, "align", stack.Align),
            },
            ColumnsNode columns => columns with
            {
                Ratios = Has(patch, "ratios")
                    ? [.. patch.GetProperty("ratios").EnumerateArray().Select(x => x.TryGetInt32(out var v) ? v : throw new SpecException("Column ratios must be whole numbers."))]
                    : columns.Ratios,
                CollapseBelow = Has(patch, "collapseBelow") ? ParseBreakpoint(patch) : columns.CollapseBelow,
                Gap = Enum(patch, "gap", columns.Gap),
            },
            GridNode grid => grid with
            {
                MinItemPx = Int(patch, "minItemPx") ?? grid.MinItemPx,
                Gap = Enum(patch, "gap", grid.Gap),
            },
            HeadingNode heading => heading with
            {
                Level = Int(patch, "level") ?? heading.Level,
                Text = Merge(heading.Text, patch, "text", locale),
            },
            RichTextNode richText => richText with
            {
                Html = Merge(richText.Html, patch, "html", locale),
            },
            ButtonNode button => button with
            {
                Label = Merge(button.Label, patch, "label", locale),
                Variant = Enum(patch, "variant", button.Variant),
                LinkTo = Has(patch, "link") || Has(patch, "href") || Has(patch, "url") || Has(patch, "pageId")
                    ? ParseLink(patch)
                    : button.LinkTo,
            },
            ImageNode image => image with
            {
                AssetId = Has(patch, "assetId") ? Asset(patch, "assetId") : image.AssetId,
                Alt = Merge(image.Alt, patch, "alt", locale),
                Aspect = Enum(patch, "aspect", image.Aspect),
                Rounded = Bool(patch, "rounded") ?? image.Rounded,
            },
            VideoNode video => video with
            {
                AssetId = Has(patch, "assetId") ? Asset(patch, "assetId") : video.AssetId,
                Mode = Enum(patch, "mode", video.Mode),
            },
            SvgNode svg => svg with
            {
                AssetId = Has(patch, "assetId") ? Asset(patch, "assetId") : svg.AssetId,
                MaxWidthPx = Has(patch, "maxWidthPx") ? Int(patch, "maxWidthPx") : svg.MaxWidthPx,
                Alt = Merge(svg.Alt, patch, "alt", locale),
            },
            SpacerNode spacer => spacer with { Size = Enum(patch, "size", spacer.Size) },
            WidgetNode widget => widget with
            {
                // A widget's props are the whole bag by contract (the pre-existing
                // set-props semantics): an absent 'props' object clears them.
                Props = ParseWidgetProps(WidgetBag(patch)),
            },
            DividerNode divider => divider,
            _ => throw new SpecException($"{current.DisplayName} has no editable props."),
        };
    }

    /// <summary>
    /// A widget patch accepts both <c>{"tag":…,"props":{…}}</c> and the bare
    /// <c>{"k":"v"}</c> bag the endpoint took before this mapper existed.
    /// </summary>
    private static JsonElement WidgetBag(JsonElement patch) =>
        patch.TryGetProperty("props", out var inner) && inner.ValueKind == JsonValueKind.Object ? patch : Wrap(patch);

    private static JsonElement Wrap(JsonElement bag)
    {
        using var document = JsonDocument.Parse($"{{\"props\":{bag.GetRawText()}}}");
        return document.RootElement.Clone();
    }

    // ------------------------------------------------------------------------ helpers

    private static Dictionary<string, string> Localized(LocalizedText text) =>
        text.Values.ToDictionary(kv => kv.Key.Value, kv => kv.Value, StringComparer.Ordinal);

    private static object? DescribeLink(Link? link) => link switch
    {
        PageLink page => new { kind = "page", pageId = page.PageId.Compact },
        ExternalLink external => new { kind = "external", url = external.Url },
        _ => null,
    };

    private static LocalizedText LocalizedOf(JsonElement spec, string key, Locale locale) =>
        Merge(LocalizedText.Empty, spec, key, locale);

    private static LocalizedText Merge(LocalizedText current, JsonElement spec, string key, Locale locale)
    {
        if (!spec.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return current;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return current.With(locale, value.GetString() ?? string.Empty);
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new SpecException($"'{key}' must be a string or a locale → text object.");
        }

        var merged = current;
        foreach (var entry in value.EnumerateObject())
        {
            if (!Locale.TryCreate(entry.Name, out var entryLocale))
            {
                throw new SpecException($"'{entry.Name}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').");
            }

            merged = merged.With(entryLocale, entry.Value.GetString() ?? string.Empty);
        }

        return merged;
    }

    private static bool Has(JsonElement spec, string key) =>
        spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty(key, out _);

    private static string? String(JsonElement spec, string key) =>
        spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? Int(JsonElement spec, string key) =>
        spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty(key, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static bool? Bool(JsonElement spec, string key) =>
        spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty(key, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static AssetId? Asset(JsonElement spec, string key)
    {
        if (String(spec, key) is not { Length: > 0 } raw)
        {
            return null;
        }

        if (!Guid.TryParseExact(raw, "N", out var guid) && !Guid.TryParse(raw, out guid))
        {
            throw new SpecException($"'{key}' is not a valid asset id.");
        }

        return AssetId.From(guid);
    }

    private static T Enum<T>(JsonElement spec, string key, T fallback) where T : struct, System.Enum
    {
        if (String(spec, key) is not { Length: > 0 } raw)
        {
            return fallback;
        }

        if (!System.Enum.TryParse<T>(raw, ignoreCase: true, out var parsed) || !System.Enum.IsDefined(parsed))
        {
            throw new SpecException($"'{raw}' is not a valid {key} — one of: {string.Join(", ", System.Enum.GetNames<T>())}.");
        }

        return parsed;
    }

    /// <summary>A caller-facing spec problem — always surfaced as a 400 with its message.</summary>
    private sealed class SpecException(string message) : Exception(message);
}
