using Imprint.Authoring.Features.Pages;

namespace Imprint.Authoring.Tests.Features.Pages;

/// <summary>Widget manifest fake: a fixed tag → declared-prop-names table per host.</summary>
internal sealed class FakeWidgetCatalog : IWidgetCatalog
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>();

    private readonly Dictionary<string, IReadOnlySet<string>> _widgets = new(StringComparer.Ordinal);

    public FakeWidgetCatalog Declare(string tag, params string[] propNames)
    {
        _widgets[tag] = propNames.ToHashSet(StringComparer.Ordinal);
        return this;
    }

    public bool Exists(string tag) => _widgets.ContainsKey(tag);

    public IReadOnlySet<string> PropNames(string tag) => _widgets.GetValueOrDefault(tag, None);
}
