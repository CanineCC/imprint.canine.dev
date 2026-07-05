using System.Text.Json;

namespace ContentSeeder;

/// <summary>
/// A locale translation resource: a flat English-source → target-string map, loaded from
/// <c>tools/ContentSeeder/da/&lt;siteKey&gt;.da.json</c>. It is keyed by the EXACT English value
/// that <c>TranslationCoverage</c> reports as a field's <c>SourceText</c> — plain text for
/// headings/titles/labels, canonical HTML for rich text — so identical English always maps to
/// identical target text (repeated CTAs like "Talk to us" translate once, consistently), and
/// the mapping is stable across <c>--reauthor</c> runs even though NodeIds are regenerated.
/// Empty/whitespace values are treated as MISSING (skeleton placeholders left to fill in).
/// </summary>
public sealed class Translations
{
    private readonly IReadOnlyDictionary<string, string> _map;
    private readonly List<string> _missing = [];

    private Translations(IReadOnlyDictionary<string, string> map) => _map = map;

    public static Translations Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Translations(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                  ?? new Dictionary<string, string>();
        var map = raw
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        return new Translations(map);
    }

    /// <summary>Look up a target string for an English source. Records a miss when absent.</summary>
    public bool TryGet(string english, out string translated)
    {
        if (_map.TryGetValue(english, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            translated = value;
            return true;
        }

        _missing.Add(english);
        translated = string.Empty;
        return false;
    }

    /// <summary>Every English source asked for that had no target — the fill-in worklist.</summary>
    public IReadOnlyList<string> Missing => _missing;
}
