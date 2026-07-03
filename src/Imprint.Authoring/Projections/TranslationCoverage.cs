using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Projections;

/// <summary>One translatable field of a page, addressed the way EditText addresses it.</summary>
public sealed record TranslatableField(
    NodeId NodeId,
    string Field,
    string Label,
    string SourceText,
    bool IsHtml)
{
    /// <summary>Page-level fields (title, meta) use the Root sentinel as their node id.</summary>
    public bool IsPageField => NodeId.IsRoot;
}

public sealed record LocaleCoverage(
    Locale Locale,
    int TranslatedCount,
    int TotalCount,
    IReadOnlyList<TranslatableField> Missing)
{
    public double Ratio => TotalCount == 0 ? 1 : (double)TranslatedCount / TotalCount;
    public bool Complete => TranslatedCount == TotalCount;
}

/// <summary>
/// Side-by-side translation support as a query service (see <see cref="ContentUsage"/>
/// for why computed-not-folded). A field "exists" when the site's default locale has a
/// value for it; coverage for a locale counts how many of those it also has. Block
/// instance overrides are per-instance content and deliberately out of scope here —
/// their translations live on the block definition's own fields.
/// </summary>
public sealed class TranslationCoverage(PageDrafts drafts, SiteOverview site)
{
    public IReadOnlyList<TranslatableField> FieldsOf(PageId pageId)
    {
        if (drafts.Get(pageId) is not { } page || site.Current is not { } current)
        {
            return [];
        }

        var defaultLocale = current.DefaultLocale;
        var fields = new List<TranslatableField>();

        AddIf(page.Title, NodeId.Root, "text", "Page title", html: false);
        AddIf(page.MetaTitle, NodeId.Root, "meta-title", "Meta title", html: false);
        AddIf(page.MetaDescription, NodeId.Root, "meta-description", "Meta description", html: false);

        foreach (var node in page.Tree.All())
        {
            switch (node)
            {
                case HeadingNode heading:
                    AddIf(heading.Text, node.Id, "text", heading.DisplayName, html: false);
                    break;
                case RichTextNode richText:
                    AddIf(richText.Html, node.Id, "html", "Text", html: true);
                    break;
                case ButtonNode button:
                    AddIf(button.Label, node.Id, "label", "Button label", html: false);
                    break;
                case ImageNode image:
                    AddIf(image.Alt, node.Id, "alt", "Image alt text", html: false);
                    break;
                case SvgNode svg:
                    AddIf(svg.Alt, node.Id, "alt", "Graphic alt text", html: false);
                    break;
            }
        }

        return fields;

        void AddIf(LocalizedText text, NodeId nodeId, string field, string label, bool html)
        {
            if (text.Get(defaultLocale) is { Length: > 0 } source)
            {
                fields.Add(new TranslatableField(nodeId, field, label, source, html));
            }
        }
    }

    public LocaleCoverage For(PageId pageId, Locale locale)
    {
        var all = FieldsOf(pageId);
        if (drafts.Get(pageId) is not { } page)
        {
            return new LocaleCoverage(locale, 0, 0, []);
        }

        var missing = all.Where(field => !HasValue(page, field, locale)).ToList();
        return new LocaleCoverage(locale, all.Count - missing.Count, all.Count, missing);
    }

    /// <summary>Coverage for every non-default site locale — the translations panel's overview.</summary>
    public IReadOnlyList<LocaleCoverage> ForAllLocales(PageId pageId) =>
        site.Current is not { } current
            ? []
            : [.. current.Locales.Where(l => l != current.DefaultLocale).Select(l => For(pageId, l))];

    private static bool HasValue(Domain.Pages.Page page, TranslatableField field, Locale locale)
    {
        var text = field switch
        {
            { IsPageField: true, Field: "text" } => page.Title,
            { IsPageField: true, Field: "meta-title" } => page.MetaTitle,
            { IsPageField: true, Field: "meta-description" } => page.MetaDescription,
            _ => page.Tree.Find(field.NodeId) switch
            {
                HeadingNode heading => heading.Text,
                RichTextNode richText => richText.Html,
                ButtonNode button => button.Label,
                ImageNode image => image.Alt,
                SvgNode svg => svg.Alt,
                _ => LocalizedText.Empty,
            },
        };
        return text.Get(locale) is { Length: > 0 };
    }
}
