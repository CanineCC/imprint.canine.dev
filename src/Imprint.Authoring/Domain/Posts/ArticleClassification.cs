using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// The vocabulary a post is classified with: what KIND of article it is, and which PRODUCTS it is
/// about. Both are closed lists, because the resource center's template and index switch on the kind
/// and its accent on the products; a value nothing renders is a value nobody should be able to store.
/// </summary>
public static class ArticleClassification
{
    /// <summary>The kinds, in the order the resource center's index groups them.</summary>
    public static readonly IReadOnlyList<string> Kinds = ["essay", "guide", "whitepaper", "documentation"];

    /// <summary>The products, in canonical order: the company first, then the product family.</summary>
    public static readonly IReadOnlyList<string> Products = ["canine", "assert", "assure", "assay", "cca", "cai"];

    /// <summary>The kind, trimmed and lower-cased, or a refusal that names the kinds there are.</summary>
    public static string NormalizeKind(string kind)
    {
        var normalized = (kind ?? string.Empty).Trim().ToLowerInvariant();
        if (!Kinds.Contains(normalized))
        {
            throw new DomainException(
                $"\"{kind}\" is not an article kind. Use one of: {string.Join(", ", Kinds)}.");
        }

        return normalized;
    }

    /// <summary>The products, de-duplicated and in canonical order, or a refusal naming the unknown one.</summary>
    public static IReadOnlyList<string> NormalizeProducts(IEnumerable<string> products)
    {
        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var product in products ?? [])
        {
            var normalized = (product ?? string.Empty).Trim().ToLowerInvariant();
            if (!Products.Contains(normalized))
            {
                throw new DomainException(
                    $"\"{product}\" is not a product. Use any of: {string.Join(", ", Products)}.");
            }

            wanted.Add(normalized);
        }

        return [.. Products.Where(wanted.Contains)];
    }
}
