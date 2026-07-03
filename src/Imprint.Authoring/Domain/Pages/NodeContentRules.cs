using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// Content invariants for a single node — canonical rich text, documented property
/// ranges, text-length caps. Shared verbatim by the Page and BlockDefinition
/// aggregates: unlike the placement rules (which legitimately differ at the root and
/// are duplicated on purpose), these are a security invariant — the publisher renders
/// stored rich text as raw markup, so any entry point that let non-canonical HTML into
/// the tree would be a stored-XSS hole. One rule, one place, no drift.
/// </summary>
public static class NodeContentRules
{
    public const int MaxTextLength = 500;

    public static void Validate(Node node)
    {
        switch (node)
        {
            case HeadingNode heading:
                if (heading.Level is < 1 or > 4)
                {
                    throw new DomainException("Heading level must be between 1 and 4.");
                }

                EnsurePlainText(heading.Text);
                break;

            case RichTextNode richText:
                foreach (var (_, html) in richText.Html.Values)
                {
                    if (!CanonicalHtml.TryValidate(html, out var error))
                    {
                        throw new DomainException(error!);
                    }
                }

                break;

            case ButtonNode button:
                EnsurePlainText(button.Label);
                break;

            case ImageNode image:
                EnsurePlainText(image.Alt);
                break;

            case SvgNode svg:
                EnsurePlainText(svg.Alt);
                break;

            case GridNode grid:
                if (grid.MinItemPx is < 160 or > 480)
                {
                    throw new DomainException("Grid item size must be between 160 and 480 pixels.");
                }

                break;

            case BlockInstanceNode instance:
                // Overrides carry content that the publisher splices into the block's
                // rich text and renders as raw markup, so they are held to exactly the
                // same rules as any other stored content. Without this, a whole
                // BlockInstanceNode spec entering via AddNode/ChangeNodeProps/detach
                // could smuggle non-canonical HTML that SetBlockOverride would reject.
                foreach (var entry in instance.Overrides.Entries)
                {
                    if (entry.Field == "html")
                    {
                        if (!CanonicalHtml.TryValidate(entry.Value, out var error))
                        {
                            throw new DomainException(error!);
                        }
                    }
                    else if (entry.Value.Length > MaxTextLength)
                    {
                        throw new DomainException($"Text is limited to {MaxTextLength} characters.");
                    }
                }

                break;
        }
    }

    private static void EnsurePlainText(LocalizedText text)
    {
        foreach (var (_, value) in text.Values)
        {
            if (value.Length > MaxTextLength)
            {
                throw new DomainException($"Text is limited to {MaxTextLength} characters.");
            }
        }
    }
}
