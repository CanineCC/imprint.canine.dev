using System.Text.Json.Nodes;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace ContentSeeder;

/// <summary>
/// The deterministic BLOCK → NODE mapper. One method per CMS block template
/// (packages/tina-shared/src/blocks.ts + the renderers in packages/ui/src/blocks.tsx).
/// COPY blocks become native Imprint Section/Stack/Columns/Grid/Heading/RichText/Button
/// subtrees carrying the block's kicker/heading/lede/items copy VERBATIM. The
/// widget-embedding blocks (cardGallery, liveCard, bandScale, composition, flow,
/// contactForm) become a Section wrapping the block's section head plus a WidgetNode.
/// When an <paramref name="apiBase"/> is supplied (the --api-base seeder flag, set to
/// the kennel public-API origin), the CAI data widgets are seeded to fetch REAL data live
/// from it — each widget hits its OWN already-published-gated endpoint (/api/oss for the
/// hero + home gallery + band-scale + composition, /api/public/c4 for the C4 carousel,
/// /api/public/findings for the findings carousel) and does its own selection client-side.
/// The labelled SAMPLE JSON is still emitted, but only as the no-live fallback attribute.
/// With no api-base the widgets carry the sample alone (the faithful offline default).
/// Nothing is invented; an unknown template is flagged.
/// </summary>
public sealed class BlockMapper(
    string origin,
    string? apiBase = null,
    IReadOnlyDictionary<string, AssetId>? svgAssets = null)
{
    public sealed record Flag(string Rel, string Note);

    public readonly List<Flag> Flags = [];

    // src path → uploaded AssetId, ingested by the Migrator before mapping.
    private readonly IReadOnlyDictionary<string, AssetId> _svgAssets =
        svgAssets ?? new Dictionary<string, AssetId>(StringComparer.Ordinal);

    /// <summary>Map one CMS block object to a single root Section node, or null to skip (never invent).</summary>
    public SectionNode? Map(JsonNode block, string rel)
    {
        var template = block.Template();
        var section = template switch
        {
            "hero" => Hero(block),
            "boundary" => Boundary(block),
            "features" => Features(block),
            "statband" => Statband(block),
            "personas" => Personas(block),
            "steps" => Steps(block),
            "panels" => Panels(block),
            "pricingTiers" => PricingTiers(block),
            "prose" => Prose(block),
            "composition" => Composition(block),
            "table" => Table(block),
            "docmock" => Docmock(block),
            "note" => Note(block),
            "cta" => Cta(block),
            "cardGallery" => CardGallery(block),
            "liveCard" => LiveCard(block),
            "bandScale" => BandScale(block),
            "c4Heat" => C4Heat(block),
            "findings" => Findings(block),
            "flow" => Flow(block),
            "contactForm" => ContactForm(block),
            "svgFigure" => SvgFigure(block, rel),
            _ => FlagUnknown(template, rel),
        };

        // Stamp the marketing appearance (the ip-ap-* hook) from the block template — the
        // shared contract between this seeder, the SectionNode model and the theme. An
        // unmapped template keeps Plain. Done once here so each builder stays focused on copy.
        return section is null ? null : section with { Appearance = AppearanceOf(template) };
    }

    // The one-per-template appearance contract (kept in lockstep with SectionAppearance).
    private static SectionAppearance AppearanceOf(string template) => template switch
    {
        "hero" => SectionAppearance.Hero,
        "boundary" => SectionAppearance.Boundary,
        "features" => SectionAppearance.FeatureGrid,
        "statband" => SectionAppearance.StatBand,
        "personas" => SectionAppearance.Personas,
        "steps" => SectionAppearance.Steps,
        "panels" => SectionAppearance.Panels,
        "pricingTiers" => SectionAppearance.Pricing,
        "prose" => SectionAppearance.Plain,
        "composition" => SectionAppearance.Composition,
        "table" => SectionAppearance.TableList,
        "docmock" => SectionAppearance.Docmock,
        "note" => SectionAppearance.Note,
        "cta" => SectionAppearance.Cta,
        "cardGallery" => SectionAppearance.Gallery,
        "liveCard" => SectionAppearance.LiveCard,
        "bandScale" => SectionAppearance.BandScale,
        "c4Heat" => SectionAppearance.C4Heat,
        "findings" => SectionAppearance.Findings,
        "flow" => SectionAppearance.Flow,
        "contactForm" => SectionAppearance.Contact,
        _ => SectionAppearance.Plain,
    };

    private SectionNode? FlagUnknown(string template, string rel)
    {
        Flags.Add(new Flag(rel, $"Unknown block template '{template}' — skipped (no faithful mapping)."));
        return null;
    }

    // ── section-head helper (kicker / heading / lede), shared by most blocks ─────
    private IEnumerable<Node> SectionHead(JsonNode block)
    {
        var kicker = block.Str("kicker");
        var heading = block.Str("heading");
        var lede = block.Str("lede");
        if (!string.IsNullOrWhiteSpace(kicker))
        {
            yield return Nodes.RichHtml($"<p><strong>{Inline.Escape(kicker!)}</strong></p>");
        }

        if (!string.IsNullOrWhiteSpace(heading))
        {
            yield return Nodes.Heading(2, PlainHeading(heading!));
        }

        if (!string.IsNullOrWhiteSpace(lede))
        {
            yield return Nodes.Paragraph(lede, origin);
        }
    }

    private IEnumerable<Node> CtaRow(JsonNode block)
    {
        var l1 = block.Str("ctaLabel");
        if (!string.IsNullOrWhiteSpace(l1))
        {
            yield return Nodes.Button(l1!, block.Str("ctaHref"), origin, ButtonVariant.Primary);
        }

        var l2 = block.Str("cta2Label");
        if (!string.IsNullOrWhiteSpace(l2))
        {
            yield return Nodes.Button(l2!, block.Str("cta2Href"), origin, ButtonVariant.Secondary);
        }
    }

    // ────────────────────────────────────────────────────────── COPY blocks ────

    private SectionNode Hero(JsonNode block)
    {
        var items = new List<Node>();
        var kicker = block.Str("kicker");
        if (!string.IsNullOrWhiteSpace(kicker))
        {
            items.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(kicker!)}</strong></p>"));
        }

        items.Add(Nodes.Heading(1, PlainHeading(block.Str("heading") ?? "")));
        var lede = block.Str("lede");
        if (!string.IsNullOrWhiteSpace(lede))
        {
            items.Add(Nodes.Paragraph(lede, origin));
        }

        items.AddRange(CtaRow(block));
        var microcopy = block.Str("microcopy");
        if (!string.IsNullOrWhiteSpace(microcopy))
        {
            items.Add(Nodes.Paragraph(microcopy, origin));
        }

        // The hero proof object: the CAI score card. Live from the corpus when an
        // api-base is configured (the curated hero repo), else the labelled sample.
        if (block.Bool("showCard"))
        {
            var card = block.Obj("card");
            if (card is not null)
            {
                var props = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["card"] = card.ToJsonString(),
                    ["brand"] = origin.Contains("assay") ? "assay" : origin.Contains("cai.") ? "cai" : "watchdog",
                    ["seal-text"] = "✓ Signed evidence",
                };
                InjectApiBase(props);
                var caption = card.Str("caption");
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    props["caption"] = caption!;
                }

                items.Add(Nodes.Widget("cai-score-card", props));
            }
        }

        return Nodes.Section(Nodes.Stack([.. items]));
    }

    private SectionNode Boundary(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var notCol = new List<Node> { Nodes.Heading(3, "What it is not") };
        notCol.Add(ItemsList(block.Arr("notItems")));
        var isCol = new List<Node> { Nodes.Heading(3, "What it is") };
        isCol.Add(ItemsList(block.Arr("isItems")));

        stack.Add(Nodes.Columns([Nodes.Stack([.. notCol]), Nodes.Stack([.. isCol])]));
        return Nodes.Section(Nodes.Stack([.. stack]));

        RichTextNode ItemsList(JsonArray arr)
        {
            var sb = new System.Text.StringBuilder("<ul>");
            foreach (var item in arr)
            {
                if (item is null)
                {
                    continue;
                }

                var title = item.Str("title") ?? "";
                var body = item.Str("body");
                sb.Append("<li><strong>").Append(Inline.Escape(title)).Append("</strong>");
                if (!string.IsNullOrWhiteSpace(body))
                {
                    sb.Append("<br>").Append(Inline.ToCanonicalInline(body, origin));
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
            return Nodes.RichHtml(sb.ToString());
        }
    }

    private SectionNode Features(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var cards = new List<Node>();
        foreach (var item in block.Arr("items"))
        {
            if (item is null)
            {
                continue;
            }

            var cell = new List<Node>();
            var tag = item.Str("tag");
            if (!string.IsNullOrWhiteSpace(tag))
            {
                cell.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(tag!)}</strong></p>"));
            }

            var title = item.Str("title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                cell.Add(Nodes.Heading(3, PlainHeading(title!)));
            }

            var body = item.Str("body");
            if (!string.IsNullOrWhiteSpace(body))
            {
                cell.Add(Nodes.Paragraph(body, origin));
            }

            var href = item.Str("href");
            var linkLabel = item.Str("linkLabel");
            if (!string.IsNullOrWhiteSpace(href) && !string.IsNullOrWhiteSpace(linkLabel))
            {
                cell.Add(Nodes.Button(linkLabel!, href, origin, ButtonVariant.Ghost));
            }

            cards.Add(Nodes.Stack([.. cell]));
        }

        if (cards.Count > 0)
        {
            stack.Add(Nodes.Grid(280, [.. cards]));
        }

        var footnote = block.Str("footnote");
        if (!string.IsNullOrWhiteSpace(footnote))
        {
            stack.Add(Nodes.Paragraph(footnote, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode Statband(JsonNode block)
    {
        var cells = new List<Node>();
        foreach (var stat in block.Arr("stats"))
        {
            if (stat is null)
            {
                continue;
            }

            var value = stat.Str("value") ?? "";
            var label = stat.Str("label") ?? "";
            cells.Add(Nodes.Stack(
                Nodes.Heading(2, Nodes.Clamp(value, 500)),
                Nodes.Paragraph(label, origin)));
        }

        var grid = cells.Count > 0 ? Nodes.Grid(200, [.. cells]) : (Node)Nodes.Stack();
        return Nodes.Section(SectionBackground.SurfaceAlt, Nodes.Stack(grid));
    }

    private SectionNode Personas(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var cards = new List<Node>();
        foreach (var card in block.Arr("cards"))
        {
            if (card is null)
            {
                continue;
            }

            var cell = new List<Node>();
            var title = card.Str("title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                cell.Add(Nodes.Heading(3, PlainHeading(title!)));
            }

            var body = card.Str("body");
            if (!string.IsNullOrWhiteSpace(body))
            {
                cell.Add(Nodes.Paragraph(body, origin));
            }

            var linkLabel = card.Str("linkLabel");
            if (!string.IsNullOrWhiteSpace(linkLabel))
            {
                cell.Add(Nodes.Button(linkLabel!, card.Str("href"), origin, ButtonVariant.Ghost));
            }

            cards.Add(Nodes.Stack([.. cell]));
        }

        if (cards.Count > 0)
        {
            stack.Add(Nodes.Grid(280, [.. cards]));
        }

        var note = block.Str("note");
        if (!string.IsNullOrWhiteSpace(note))
        {
            stack.Add(Nodes.Paragraph(note, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode Steps(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var cells = new List<Node>();
        foreach (var step in block.Arr("items"))
        {
            if (step is null)
            {
                continue;
            }

            var cell = new List<Node>();
            var title = step.Str("title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                cell.Add(Nodes.Heading(3, PlainHeading(title!)));
            }

            var body = step.Str("body");
            if (!string.IsNullOrWhiteSpace(body))
            {
                cell.Add(Nodes.Paragraph(body, origin));
            }

            cells.Add(Nodes.Stack([.. cell]));
        }

        if (cells.Count > 0)
        {
            stack.Add(Nodes.Grid(240, [.. cells]));
        }

        var footnote = block.Str("footnote");
        if (!string.IsNullOrWhiteSpace(footnote))
        {
            stack.Add(Nodes.Paragraph(footnote, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode Panels(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var cells = new List<Node>();
        foreach (var panel in block.Arr("items"))
        {
            if (panel is null)
            {
                continue;
            }

            var cell = new List<Node>();
            var pk = panel.Str("kicker");
            if (!string.IsNullOrWhiteSpace(pk))
            {
                cell.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(pk!)}</strong></p>"));
            }

            var title = panel.Str("title");
            if (!string.IsNullOrWhiteSpace(title))
            {
                cell.Add(Nodes.Heading(3, PlainHeading(title!)));
            }

            var body = panel.Str("body");
            if (!string.IsNullOrWhiteSpace(body))
            {
                cell.Add(Nodes.Paragraph(body, origin));
            }

            var ctaLabel = panel.Str("ctaLabel");
            if (!string.IsNullOrWhiteSpace(ctaLabel))
            {
                cell.Add(Nodes.Button(ctaLabel!, panel.Str("ctaHref"), origin, ButtonVariant.Secondary));
            }

            cells.Add(Nodes.Stack([.. cell]));
        }

        // Two-up panels; Columns needs 2–4 cells, so fall back to a Grid for other counts.
        if (cells.Count is 2 or 3 or 4)
        {
            stack.Add(Nodes.Columns([.. cells.Cast<StackNode>()]));
        }
        else if (cells.Count > 0)
        {
            stack.Add(Nodes.Grid(320, [.. cells]));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode PricingTiers(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var cells = new List<Node>();
        foreach (var tier in block.Arr("tiers"))
        {
            if (tier is null)
            {
                continue;
            }

            var cell = new List<Node>();
            var badge = tier.Str("badge");
            if (!string.IsNullOrWhiteSpace(badge))
            {
                cell.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(badge!)}</strong></p>"));
            }

            var name = tier.Str("name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                cell.Add(Nodes.Heading(3, PlainHeading(name!)));
            }

            var who = tier.Str("who");
            if (!string.IsNullOrWhiteSpace(who))
            {
                cell.Add(Nodes.Paragraph(who, origin));
            }

            var sizeNote = tier.Str("sizeNote");
            if (!string.IsNullOrWhiteSpace(sizeNote))
            {
                cell.Add(Nodes.Paragraph(sizeNote, origin));
            }

            var price = tier.Str("price");
            if (!string.IsNullOrWhiteSpace(price))
            {
                cell.Add(Nodes.Heading(4, Nodes.Clamp(price!, 500)));
            }

            var priceNote = tier.Str("priceNote");
            if (!string.IsNullOrWhiteSpace(priceNote))
            {
                cell.Add(Nodes.Paragraph(priceNote, origin));
            }

            var feats = tier.Arr("features");
            if (feats.Count > 0)
            {
                var sb = new System.Text.StringBuilder("<ul>");
                foreach (var f in feats)
                {
                    sb.Append("<li>").Append(Inline.ToCanonicalInline(f?.ToString(), origin)).Append("</li>");
                }

                sb.Append("</ul>");
                cell.Add(Nodes.RichHtml(sb.ToString()));
            }

            var ctaLabel = tier.Str("ctaLabel");
            if (!string.IsNullOrWhiteSpace(ctaLabel))
            {
                cell.Add(Nodes.Button(ctaLabel!, tier.Str("ctaHref"), origin,
                    tier.Bool("featured") ? ButtonVariant.Primary : ButtonVariant.Secondary));
            }

            var cardFoot = tier.Str("footnote");
            if (!string.IsNullOrWhiteSpace(cardFoot))
            {
                cell.Add(Nodes.Paragraph(cardFoot, origin));
            }

            cells.Add(Nodes.Stack([.. cell]));
        }

        if (cells.Count is 2 or 3 or 4)
        {
            stack.Add(Nodes.Columns([.. cells.Cast<StackNode>()]));
        }
        else if (cells.Count > 0)
        {
            stack.Add(Nodes.Grid(280, [.. cells]));
        }

        var footnote = block.Str("footnote");
        if (!string.IsNullOrWhiteSpace(footnote))
        {
            stack.Add(Nodes.Paragraph(footnote, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    /// <summary>
    /// Long-form marketing prose: the section head, then body paragraphs, then an
    /// optional bulleted list, then a closing footnote paragraph. Not a Tina template —
    /// added for the canine.dev studio site, whose hand-written Blazor home page has
    /// multi-paragraph manifesto/mission/doctrine/timeline sections that no CMS block
    /// shape carries. The copy is transcribed verbatim into this shape.
    /// </summary>
    private SectionNode Prose(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        foreach (var paragraph in block.Arr("paragraphs"))
        {
            var text = paragraph?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                stack.Add(Nodes.Paragraph(text, origin));
            }
        }

        var items = block.Arr("items");
        if (items.Count > 0)
        {
            var sb = new System.Text.StringBuilder("<ul>");
            foreach (var item in items)
            {
                sb.Append("<li>").Append(Inline.ToCanonicalInline(item?.ToString(), origin)).Append("</li>");
            }

            sb.Append("</ul>");
            stack.Add(Nodes.RichHtml(sb.ToString()));
        }

        var footnote = block.Str("footnote");
        if (!string.IsNullOrWhiteSpace(footnote))
        {
            stack.Add(Nodes.Paragraph(footnote, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode Table(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        // Canonical HTML has no <table>; render faithfully as a bulleted list of rows
        // (header row bolded, cells verbatim). Flagged as a transform, not a loss.
        var headers = block.Arr("headers");
        var sb = new System.Text.StringBuilder("<ul>");
        if (headers.Count > 0)
        {
            sb.Append("<li><strong>")
              .Append(Inline.Escape(string.Join(" · ", headers.Select(h => h?.ToString() ?? ""))))
              .Append("</strong></li>");
        }

        foreach (var row in block.Arr("rows"))
        {
            if (row is null)
            {
                continue;
            }

            var cells = row.Arr("cells");
            sb.Append("<li>");
            for (var c = 0; c < cells.Count; c++)
            {
                if (c > 0)
                {
                    sb.Append(" — ");
                }

                sb.Append(Inline.ToCanonicalInline(cells[c]?.ToString(), origin));
            }

            sb.Append("</li>");
        }

        sb.Append("</ul>");
        stack.Add(Nodes.RichHtml(sb.ToString()));
        Flags.Add(new Flag("(table block)",
            "table block rendered as a canonical bulleted list of rows (Imprint has no <table> in its canonical HTML subset); all header/cell copy verbatim."));

        var caption = block.Str("caption");
        if (!string.IsNullOrWhiteSpace(caption))
        {
            stack.Add(Nodes.Paragraph(caption, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    private SectionNode Docmock(JsonNode block)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var tag = block.Str("tag");
        if (!string.IsNullOrWhiteSpace(tag))
        {
            stack.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(tag!)}</strong></p>"));
        }

        var title = block.Str("title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            stack.Add(Nodes.Heading(4, PlainHeading(title!)));
        }

        var rows = block.Arr("rows");
        if (rows.Count > 0)
        {
            var sb = new System.Text.StringBuilder("<ul>");
            foreach (var row in rows)
            {
                if (row is null)
                {
                    continue;
                }

                var label = row.Str("label") ?? "";
                var value = row.Str("value") ?? "";
                sb.Append("<li><strong>").Append(Inline.Escape(label)).Append("</strong> — ")
                  .Append(Inline.ToCanonicalInline(value, origin)).Append("</li>");
            }

            sb.Append("</ul>");
            stack.Add(Nodes.RichHtml(sb.ToString()));
        }

        var clauses = block.Arr("clauses");
        if (clauses.Count > 0)
        {
            var sb = new System.Text.StringBuilder("<ul>");
            foreach (var c in clauses)
            {
                sb.Append("<li>").Append(Inline.ToCanonicalInline(c?.ToString(), origin)).Append("</li>");
            }

            sb.Append("</ul>");
            stack.Add(Nodes.RichHtml(sb.ToString()));
        }

        var caption = block.Str("caption");
        if (!string.IsNullOrWhiteSpace(caption))
        {
            stack.Add(Nodes.Paragraph(caption, origin));
        }

        return Nodes.Section(SectionBackground.Surface, Nodes.Stack([.. stack]));
    }

    private SectionNode Note(JsonNode block)
    {
        var stack = new List<Node>();
        var title = block.Str("title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            stack.Add(Nodes.Heading(3, PlainHeading(title!)));
        }

        var body = block.Str("body");
        if (!string.IsNullOrWhiteSpace(body))
        {
            stack.Add(Nodes.Paragraph(body, origin));
        }

        var background = block.Str("tone") == "warn" ? SectionBackground.Primary : SectionBackground.Surface;
        return Nodes.Section(background, Nodes.Stack([.. stack]));
    }

    private SectionNode Cta(JsonNode block)
    {
        var stack = new List<Node>();
        var heading = block.Str("heading");
        if (!string.IsNullOrWhiteSpace(heading))
        {
            stack.Add(Nodes.Heading(2, PlainHeading(heading!)));
        }

        var lede = block.Str("lede");
        if (!string.IsNullOrWhiteSpace(lede))
        {
            stack.Add(Nodes.Paragraph(lede, origin));
        }

        stack.AddRange(CtaRow(block));
        var microcopy = block.Str("microcopy");
        if (!string.IsNullOrWhiteSpace(microcopy))
        {
            stack.Add(Nodes.Paragraph(microcopy, origin));
        }

        return Nodes.Section(SectionBackground.SurfaceAlt, Nodes.Stack([.. stack]));
    }

    /// <summary>
    /// An old-site infographic: a Section holding an optional head (the heading maps to
    /// an H3 — these figures titled sub-sections, e.g. "SonarQube + Watchdog"), the
    /// SvgNode referencing the pre-ingested asset (light + optional dark variant, both
    /// inlined by the publisher and revealed by theme), and an optional caption. The
    /// figure's design width rides along as MaxWidthPx so it never upscales.
    /// </summary>
    private SectionNode? SvgFigure(JsonNode block, string rel)
    {
        var src = block.Str("src");
        if (string.IsNullOrWhiteSpace(src) || !_svgAssets.TryGetValue(src!, out var assetId))
        {
            Flags.Add(new Flag(rel, $"svgFigure '{src}' has no ingested asset — skipped (never invented)."));
            return null;
        }

        var stack = new List<Node>();
        var kicker = block.Str("kicker");
        if (!string.IsNullOrWhiteSpace(kicker))
        {
            stack.Add(Nodes.RichHtml($"<p><strong>{Inline.Escape(kicker!)}</strong></p>"));
        }

        var heading = block.Str("heading");
        if (!string.IsNullOrWhiteSpace(heading))
        {
            stack.Add(Nodes.Heading(3, PlainHeading(heading!)));
        }

        var lede = block.Str("lede");
        if (!string.IsNullOrWhiteSpace(lede))
        {
            stack.Add(Nodes.Paragraph(lede, origin));
        }

        int? maxWidth = null;
        var maxWidthRaw = block.Str("maxWidth");
        if (int.TryParse(maxWidthRaw, out var parsed) && parsed > 0)
        {
            maxWidth = parsed;
        }

        stack.Add(new SvgNode
        {
            Id = NodeId.New(),
            AssetId = assetId,
            MaxWidthPx = maxWidth,
            Alt = Nodes.Text(Nodes.Clamp(block.Str("alt") ?? "", 500)),
        });

        var caption = block.Str("caption");
        if (!string.IsNullOrWhiteSpace(caption))
        {
            stack.Add(Nodes.Paragraph(caption, origin));
        }

        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    // ─────────────────────────────────────────────────────── WIDGET blocks ─────

    private string Brand() => origin.Contains("assay") ? "assay" : origin.Contains("cai.") ? "cai" : "watchdog";

    /// <summary>
    /// Stamp the live kennel API origin onto a CAI data widget's props when the
    /// --api-base flag was supplied. This is the ONE toggle that turns a seeded sample
    /// widget into a live-fetching one: the island reads `api-base` and, when set, fetches
    /// real curated data — falling back to the sample attribute only on failure/absence.
    /// The parent sets this to Track K's public API base at reseed time.
    /// </summary>
    private void InjectApiBase(Dictionary<string, string> props)
    {
        if (!string.IsNullOrWhiteSpace(apiBase))
        {
            props["api-base"] = apiBase!.Trim();
        }
    }

    private SectionNode WidgetSection(JsonNode block, string tag, Action<Dictionary<string, string>> fill)
    {
        var stack = new List<Node>();
        stack.AddRange(SectionHead(block));

        var props = new Dictionary<string, string>(StringComparer.Ordinal) { ["brand"] = Brand() };
        fill(props);
        stack.Add(Nodes.Widget(tag, props));
        return Nodes.Section(Nodes.Stack([.. stack]));
    }

    // Each CAI data widget picks WHICH repo(s) it shows from its own published-gated endpoint
    // (the hero = highest bestScore; the home gallery = second-best + most-improved + random;
    // the carousels cycle the server's LoC / weighted order), so blocks carry no owner/name
    // pin to drive the live pick. Two narrow, still-meaningful knobs remain:
    //
    //  • CopyCount     — the display cap the gallery widget honours (how many cards to
    //                    render). Not a repo pin.
    //  • CopySampleLabel — owner/name that label the OFFLINE SAMPLE card only (an optional
    //                    author override); a LIVE card always IS the repo the widget picked,
    //                    so these never influence the live pick.
    private static void CopyCount(JsonNode block, Dictionary<string, string> props)
    {
        var count = block.Str("count");
        if (!string.IsNullOrWhiteSpace(count))
        {
            props["count"] = count!;
        }
    }

    private static void CopySampleLabel(JsonNode block, Dictionary<string, string> props)
    {
        Copy(block, "owner", props, "owner");
        Copy(block, "name", props, "name");
    }

    private SectionNode CardGallery(JsonNode block) =>
        WidgetSection(block, "cai-card-gallery", props =>
        {
            // The labelled sample stays as the fallback attribute; api-base drives live.
            // The widget fetches /api/oss and picks the home trio (second-best + most-improved
            // + random, hero-excluded) itself — no owner/name pin here, only the display `count`.
            props["cards"] = block.Arr("cards").ToJsonString();
            InjectApiBase(props);
            CopyCount(block, props);
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "footnote", props, "footnote");
        });

    private SectionNode LiveCard(JsonNode block) =>
        // Live from the HERO (the widget fetches /api/oss and picks the highest bestScore)
        // when an api-base is set; the labelled SAMPLE card is kept as the no-live fallback
        // attribute. owner/name only label the OFFLINE sample (an override), never a live pin.
        WidgetSection(block, "cai-score-card", props =>
        {
            var card = block.Obj("card");
            if (card is not null)
            {
                props["card"] = card.ToJsonString();
                var caption = card.Str("caption");
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    props["caption"] = caption!;
                }
            }

            InjectApiBase(props);
            CopySampleLabel(block, props);
        });

    private SectionNode BandScale(JsonNode block) =>
        // Pins at the HERO published card's bestScore (the widget fetches /api/oss and picks
        // the highest-scoring repo) when an api-base is set — no owner/name pin here.
        WidgetSection(block, "cai-band-scale", props =>
        {
            InjectApiBase(props);
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "caption", props, "caption");
            // The seeded score is the no-live fallback pin.
            var score = block.Str("score");
            if (!string.IsNullOrWhiteSpace(score))
            {
                props["score"] = score!;
            }
        });

    private SectionNode C4Heat(JsonNode block) =>
        // The C4 architecture heat-map CAROUSEL — a LIVE-ONLY island (no meaningful static
        // twin of an architecture map). The widget fetches /api/public/c4 (the LoC-ordered
        // published C4-eligible repos) and cycles them, loading each repo's public c4.svg. So
        // we stamp ONLY api-base — no owner/name pin. With no api-base it shows nothing.
        WidgetSection(block, "cai-c4-heat", props =>
        {
            InjectApiBase(props);
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "caption", props, "caption");
        });

    private SectionNode Findings(JsonNode block) =>
        // The deterministic architecture / domain-model / event findings located to
        // file:line, from real published reports — a CAROUSEL. The widget fetches
        // /api/public/findings (the DDD-moat weighted list) and cycles the repos. So we stamp
        // ONLY api-base — no owner/name pin. The island keeps its own small labelled SAMPLE
        // as the no-live fallback.
        WidgetSection(block, "cai-findings", props =>
        {
            InjectApiBase(props);
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "footnote", props, "footnote");
        });

    private SectionNode Composition(JsonNode block) =>
        WidgetSection(block, "cai-composition-bar", props =>
        {
            // The seeded segments stay as the fallback; api-base drives live. The widget
            // fetches /api/oss and builds the split from the HERO card's brilliant%/slop% —
            // no owner/name pin here.
            props["segments"] = block.Arr("segments").ToJsonString();
            InjectApiBase(props);
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "caption", props, "caption");
        });

    private SectionNode Flow(JsonNode block) =>
        WidgetSection(block, "cai-evidence-flow", props =>
        {
            props["nodes"] = block.Arr("nodes").ToJsonString();
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "footnote", props, "footnote");
            var loop = block.Str("loopLabel");
            if (!string.IsNullOrWhiteSpace(loop))
            {
                props["loop-label"] = loop!;
            }
        });

    private SectionNode ContactForm(JsonNode block) =>
        WidgetSection(block, "contact-form", props =>
        {
            props["topics"] = block.Arr("topics").ToJsonString();
            var fallback = block.Str("fallbackEmail");
            props["fallback-email"] = string.IsNullOrWhiteSpace(fallback) ? "sales@canine.dev" : fallback!;
            Copy(block, "kicker", props, "kicker");
            Copy(block, "heading", props, "heading");
            Copy(block, "lede", props, "lede");
            Copy(block, "privacyNote", props, "privacy-note");
        });

    // widget kicker/heading/lede/etc. props are rendered by the island's own renderInline,
    // so they carry the CMS inline markup VERBATIM (the island understands **bold** etc.).
    private static void Copy(JsonNode block, string src, Dictionary<string, string> props, string prop)
    {
        var v = block.Str(src);
        if (!string.IsNullOrWhiteSpace(v))
        {
            props[prop] = v!;
        }
    }

    // Heading text is plain in the canonical model; keep the words, drop the inline markers.
    private static string PlainHeading(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                i += 2;
                continue;
            }

            if (text[i] == '`')
            {
                i++;
                continue;
            }

            if (text[i] == '[')
            {
                var close = text.IndexOf(']', i);
                if (close > i && close + 1 < text.Length && text[close + 1] == '(')
                {
                    sb.Append(text[(i + 1)..close]);
                    var end = text.IndexOf(')', close);
                    i = end >= 0 ? end + 1 : text.Length;
                    continue;
                }
            }

            sb.Append(text[i]);
            i++;
        }

        return sb.ToString();
    }
}
