using Imprint.Authoring.Domain.Pages;
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

    // Top-level nav entries and per-group children are capped separately: a grouped menu
    // (dropdowns) legitimately holds far more links than a flat bar, so the flat 20 became
    // 20 top-level entries × up to 12 children each (matches the marketing header's shape).
    public const int MaxNavigationItems = 20;
    public const int MaxNavigationChildren = 12;

    public const int MaxFooterGroups = 8;
    public const int MaxFooterLinksPerGroup = 20;
    public const int MaxEnvironments = 8;
    public const int MaxEnvironmentNameLength = 40;
    public const int MaxCollaborators = 20;
    public const int MaxCollaboratorEmailLength = 254;

    private readonly List<Locale> _locales = [];
    private readonly List<string> _collaborators = [];
    private IReadOnlyList<NavigationItem> _navigation = [];
    private IReadOnlyList<FooterLinkGroup> _footerGroups = [];
    private IReadOnlyList<DeployEnvironment> _environments = [];

    public SiteId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Insertion order is preserved deliberately: the first locale is the created
    // default, and the editor lists locales in the order editors added them.
    public IReadOnlyList<Locale> Locales => _locales;
    public Locale DefaultLocale { get; private set; }
    public Theme Theme { get; private set; } = Theme.Default;
    public IReadOnlyList<NavigationItem> Navigation => _navigation;

    // Marketing chrome around the page content. All optional: a site with none renders the
    // plain shell (site name → home, flat nav, minimal footer), exactly as before.
    public IReadOnlyList<FooterLinkGroup> FooterGroups => _footerGroups;
    public HeaderAction? HeaderCta { get; private set; }
    public HeaderAction? HeaderQuiet { get; private set; }
    public CopyLine? CopyLine { get; private set; }

    // Emails of the people who may edit this site besides its owner, in the order they
    // were added. The owner is not in this list — it lives on the site.created envelope
    // actor (see SiteOverview), so the two never drift.
    public IReadOnlyList<string> Collaborators => _collaborators;

    // The site's deploy targets, in promotion order (e.g. Test → Staging → Production).
    // A site with none has never been given a publish destination; the dashboard's gear
    // is where they are configured.
    public IReadOnlyList<DeployEnvironment> Environments => _environments;

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

        foreach (var item in items)
        {
            ValidateNavigationItem(item);
        }

        // A same-site page may appear at most once as a top-level DIRECT link — that is
        // what makes navigation order and the home page (nav-first page) well-defined.
        // Group children and external links carry no page identity, so they are exempt.
        var topLevelPages = items.Select(i => i.PageId).OfType<PageId>().ToList();
        if (topLevelPages.Distinct().Count() != topLevelPages.Count)
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

    private static void ValidateNavigationItem(NavigationItem item)
    {
        if (item.IsGroup)
        {
            // A group is a heading + its dropdown children: it needs a label and at least
            // one child, and neither a direct link (it is not one) nor too many children.
            if (item.Link is not null)
            {
                throw new DomainException("A navigation group cannot also be a direct link.");
            }

            if (item.Label is null || item.Label.IsEmpty)
            {
                throw new DomainException("A navigation group must have a label.");
            }

            if (item.Children.Count > MaxNavigationChildren)
            {
                throw new DomainException($"A navigation group can hold at most {MaxNavigationChildren} links.");
            }

            foreach (var child in item.Children)
            {
                // An external child link's label is the only text it has, so require it;
                // a page child may omit the label and inherit the page title.
                if (child.Link is ExternalLink && (child.Label is null || child.Label.IsEmpty))
                {
                    throw new DomainException("An external navigation link must have a label.");
                }
            }

            return;
        }

        // A direct entry must have a link; an external one must carry its own label.
        if (item.Link is null)
        {
            throw new DomainException("A navigation entry must be either a link or a group with children.");
        }

        if (item.Link is ExternalLink && (item.Label is null || item.Label.IsEmpty))
        {
            throw new DomainException("An external navigation link must have a label.");
        }
    }

    /// <summary>Replace the footer's named link columns. Empty clears the footer.</summary>
    public void SetFooter(IReadOnlyList<FooterLinkGroup> groups)
    {
        if (groups.Count > MaxFooterGroups)
        {
            throw new DomainException($"The footer can hold at most {MaxFooterGroups} link groups.");
        }

        foreach (var group in groups)
        {
            if (group.Heading.IsEmpty)
            {
                throw new DomainException("A footer link group must have a heading.");
            }

            if (group.Links.Count > MaxFooterLinksPerGroup)
            {
                throw new DomainException($"A footer link group can hold at most {MaxFooterLinksPerGroup} links.");
            }

            foreach (var link in group.Links)
            {
                if (link.Link is ExternalLink && (link.Label is null || link.Label.IsEmpty))
                {
                    throw new DomainException("An external footer link must have a label.");
                }
            }
        }

        if (_footerGroups.SequenceEqual(groups))
        {
            return;
        }

        Raise(new SiteFooterChanged([.. groups]));
    }

    /// <summary>
    /// Set (or clear, with two nulls) the header's primary CTA and quiet link. They share
    /// a slot and the editor sets them together, so one call carries both.
    /// </summary>
    public void SetHeaderActions(HeaderAction? cta, HeaderAction? quiet)
    {
        foreach (var action in new[] { cta, quiet })
        {
            if (action is not null && action.Label.IsEmpty)
            {
                throw new DomainException("A header action must have a label.");
            }
        }

        if (Equals(HeaderCta, cta) && Equals(HeaderQuiet, quiet))
        {
            return;
        }

        Raise(new SiteHeaderActionsChanged(cta, quiet));
    }

    /// <summary>Set (or clear, with null) the footer's fine-print copy line.</summary>
    public void SetCopyLine(CopyLine? copyLine)
    {
        if (copyLine is not null && copyLine.Text.IsEmpty)
        {
            throw new DomainException("The copy line cannot be empty (pass null to clear it).");
        }

        if (Equals(CopyLine, copyLine))
        {
            return;
        }

        Raise(new SiteCopyLineChanged(copyLine));
    }

    /// <summary>
    /// Replace the site's ordered deploy targets. Names identify an environment in the UI
    /// and in promotion ("promote Test → Staging"), so they must be present and unique;
    /// the path is where static output is written. Whether the path is a <em>safe</em>
    /// place to write (sandbox root, traversal) is a filesystem-policy question the deploy
    /// infrastructure answers — the aggregate only guarantees the config is well-formed.
    /// </summary>
    public void SetEnvironments(IReadOnlyList<DeployEnvironment> environments)
    {
        if (environments.Count > MaxEnvironments)
        {
            throw new DomainException($"A site can have at most {MaxEnvironments} deploy environments.");
        }

        var normalized = new List<DeployEnvironment>(environments.Count);
        foreach (var environment in environments)
        {
            var name = environment.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new DomainException("A deploy environment must have a name.");
            }

            if (name.Length > MaxEnvironmentNameLength)
            {
                throw new DomainException(
                    $"A deploy environment name must be {MaxEnvironmentNameLength} characters or fewer.");
            }

            var path = environment.Path?.Trim() ?? string.Empty;
            if (path.Length == 0)
            {
                throw new DomainException($"The '{name}' environment must have a publish folder.");
            }

            normalized.Add(new DeployEnvironment(name, path));
        }

        if (normalized.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
        {
            throw new DomainException("Deploy environment names must be unique (case-insensitive).");
        }

        if (_environments.SequenceEqual(normalized))
        {
            return;
        }

        Raise(new SiteEnvironmentsChanged(normalized));
    }

    /// <summary>
    /// Grant another person edit access by the email they sign in with. The aggregate
    /// validates shape only — whether the address belongs to a real, reachable person is
    /// the operator's concern (access simply never matches a mistyped email).
    /// </summary>
    public void AddCollaborator(string email)
    {
        var address = ValidCollaboratorEmail(email);
        if (_collaborators.Contains(address, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException($"'{address}' is already a collaborator on this site.");
        }

        if (_collaborators.Count >= MaxCollaborators)
        {
            throw new DomainException($"A site can have at most {MaxCollaborators} collaborators.");
        }

        Raise(new SiteCollaboratorAdded(address));
    }

    public void RemoveCollaborator(string email)
    {
        var address = ValidCollaboratorEmail(email);
        if (!_collaborators.Contains(address, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException($"'{address}' is not a collaborator on this site.");
        }

        Raise(new SiteCollaboratorRemoved(address));
    }

    private static string ValidCollaboratorEmail(string email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new DomainException("The collaborator email cannot be empty.");
        }

        if (trimmed.Length > MaxCollaboratorEmailLength)
        {
            throw new DomainException(
                $"A collaborator email must be {MaxCollaboratorEmailLength} characters or fewer.");
        }

        // Deliberately loose: one '@' with something on both sides, no whitespace. The
        // check exists to catch obvious typos, not to adjudicate RFC 5322 — the IdP is
        // the authority on what a valid login email is.
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1 || trimmed.IndexOf('@', at + 1) >= 0 ||
            trimmed.Any(char.IsWhiteSpace))
        {
            throw new DomainException($"'{trimmed}' does not look like an email address.");
        }

        return trimmed;
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
            case SiteFooterChanged e:
                _footerGroups = [.. e.Groups];
                break;
            case SiteHeaderActionsChanged e:
                HeaderCta = e.Cta;
                HeaderQuiet = e.Quiet;
                break;
            case SiteCopyLineChanged e:
                CopyLine = e.CopyLine;
                break;
            case SiteEnvironmentsChanged e:
                _environments = [.. e.Environments];
                break;
            case SiteCollaboratorAdded e:
                _collaborators.Add(e.Email);
                break;
            case SiteCollaboratorRemoved e:
                _collaborators.RemoveAll(c => string.Equals(c, e.Email, StringComparison.OrdinalIgnoreCase));
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
