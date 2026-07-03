using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// Site identity, locales, theme and navigation. The stream version doubles as the
/// "chrome version" in the publish manifest — any theme or navigation change marks
/// every published page stale — which is why unchanged-value edits deliberately raise
/// nothing: a no-op must not trigger a full republish.
/// </summary>
public sealed class Site : AggregateRoot
{
    public const int MaxNameLength = 100;
    public const int MaxLocales = 10;
    public const int MaxNavigationItems = 20;

    private readonly List<Locale> _locales = [];
    private IReadOnlyList<NavigationItem> _navigation = [];

    public SiteId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Insertion order is preserved deliberately: the first locale is the created
    // default, and the editor lists locales in the order editors added them.
    public IReadOnlyList<Locale> Locales => _locales;
    public Locale DefaultLocale { get; private set; }
    public Theme Theme { get; private set; } = Theme.Default;
    public IReadOnlyList<NavigationItem> Navigation => _navigation;

    public override string StreamId => Id.Stream;

    public static Site Create(SiteId id, string name, Locale defaultLocale)
    {
        var site = new Site();
        site.Raise(new SiteCreated(id, ValidName(name), defaultLocale));
        return site;
    }

    public void Rename(string name)
    {
        var newName = ValidName(name);
        if (newName == Name)
        {
            return;
        }

        Raise(new SiteRenamed(newName));
    }

    public void AddLocale(Locale locale)
    {
        if (_locales.Contains(locale))
        {
            throw new DomainException($"The locale '{locale}' is already on this site.");
        }

        if (_locales.Count >= MaxLocales)
        {
            throw new DomainException($"A site can have at most {MaxLocales} locales.");
        }

        Raise(new SiteLocaleAdded(locale));
    }

    public void RemoveLocale(Locale locale)
    {
        if (!_locales.Contains(locale))
        {
            throw new DomainException($"The locale '{locale}' is not on this site.");
        }

        if (locale == DefaultLocale)
        {
            throw new DomainException(
                $"'{locale}' is the default locale and cannot be removed. Make another locale the default first.");
        }

        // Content already written in this locale stays in the page streams — that is
        // event sourcing working as intended (history is never rewritten), not an
        // oversight: re-adding the locale later brings every translation back. It is
        // also fine that navigation label overrides may still carry this locale —
        // labels resolve with fallback, so those values simply stop being used.
        Raise(new SiteLocaleRemoved(locale));
    }

    public void ChangeDefaultLocale(Locale locale)
    {
        if (!_locales.Contains(locale))
        {
            throw new DomainException(
                $"'{locale}' is not one of this site's locales. Add it before making it the default.");
        }

        Raise(new SiteDefaultLocaleChanged(locale));
    }

    public void SetThemeToken(string token, string light, string dark)
    {
        if (!ThemeTokens.IsKnown(token))
        {
            throw new DomainException(
                $"'{token}' is not a theme token. Known tokens: {string.Join(", ", ThemeTokens.All)}.");
        }

        var lightValue = light.Trim();
        var darkValue = dark.Trim();
        if (!CssColor.IsValid(lightValue))
        {
            throw new DomainException($"'{light}' is not a valid CSS color for the light value of '{token}'.");
        }

        if (!CssColor.IsValid(darkValue))
        {
            throw new DomainException($"'{dark}' is not a valid CSS color for the dark value of '{token}'.");
        }

        if (new ThemeToken(lightValue, darkValue) == Theme.Tokens.Get(token))
        {
            return;
        }

        Raise(new SiteThemeTokenChanged(token, lightValue, darkValue));
    }

    public void SetTypography(Typography typography)
    {
        if (!typography.IsValid)
        {
            throw new DomainException(
                $"Typography is out of range: base size {Typography.MinBaseSizePx}–{Typography.MaxBaseSizePx} px, " +
                $"scale ratio {Typography.MinScaleRatio}–{Typography.MaxScaleRatio}, radius 0–{Typography.MaxRadiusPx} px.");
        }

        if (typography == Theme.Typography)
        {
            return;
        }

        Raise(new SiteTypographyChanged(typography));
    }

    public void SetNavigation(IReadOnlyList<NavigationItem> items)
    {
        if (items.Count > MaxNavigationItems)
        {
            throw new DomainException($"Navigation can hold at most {MaxNavigationItems} items.");
        }

        if (items.Select(i => i.PageId).Distinct().Count() != items.Count)
        {
            throw new DomainException("Navigation cannot contain the same page twice.");
        }

        // Whether each PageId points at an existing, undeleted page is a cross-aggregate
        // question, checked by the slice against the PageList read model. Accepted race:
        // a page deleted in the same instant leaves a dangling item, which the renderer
        // skips — never a broken link in the published output.
        if (_navigation.SequenceEqual(items))
        {
            return;
        }

        Raise(new SiteNavigationChanged([.. items]));
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case SiteCreated e:
                Id = e.SiteId;
                Name = e.Name;
                DefaultLocale = e.DefaultLocale;
                _locales.Add(e.DefaultLocale);
                Theme = Theme.Default;
                break;
            case SiteRenamed e:
                Name = e.Name;
                break;
            case SiteLocaleAdded e:
                _locales.Add(e.Locale);
                break;
            case SiteLocaleRemoved e:
                _locales.Remove(e.Locale);
                break;
            case SiteDefaultLocaleChanged e:
                DefaultLocale = e.Locale;
                break;
            case SiteThemeTokenChanged e:
                Theme = Theme with { Tokens = Theme.Tokens.With(e.Token, new ThemeToken(e.Light, e.Dark)) };
                break;
            case SiteTypographyChanged e:
                Theme = Theme with { Typography = e.Typography };
                break;
            case SiteNavigationChanged e:
                _navigation = [.. e.Items];
                break;
            default:
                throw new InvalidOperationException($"Site cannot apply unknown event {@event.GetType().Name}.");
        }
    }

    private static string ValidName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new DomainException("The site name cannot be empty.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"The site name must be {MaxNameLength} characters or fewer.");
        }

        return trimmed;
    }
}
