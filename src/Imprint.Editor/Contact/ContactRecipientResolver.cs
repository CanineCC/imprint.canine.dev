namespace Imprint.Editor.Contact;

/// <summary>
/// Decides where a contact submission is emailed, at request time. Precedence: the
/// submitting site's contact-form <c>recipients</c> widget prop (a comma list, looked up
/// live through <paramref name="widgetRecipients"/> so an editor change takes effect on
/// the very next submission — no republish), then the <c>Contact:Recipients</c> server
/// config, then none (the lead is journalled, never lost). The widget prop is declared
/// <c>private</c> in the manifest, so the address an editor types here is exactly the
/// address that never reaches published markup.
/// </summary>
public sealed class ContactRecipientResolver(IConfiguration configuration, Func<string?, string?>? widgetRecipients = null)
{
    public enum Source
    {
        /// <summary>The submitting site's contact-form <c>recipients</c> prop.</summary>
        WidgetProp,

        /// <summary>The <c>Contact:Recipients</c> configuration fallback.</summary>
        Configuration,

        /// <summary>Nothing configured anywhere — journal-only delivery.</summary>
        None,
    }

    public (IReadOnlyList<string> Recipients, Source Source) Resolve(string? site)
    {
        var fromWidget = Split(widgetRecipients?.Invoke(site));
        if (fromWidget.Count > 0)
        {
            return (fromWidget, Source.WidgetProp);
        }

        var fromConfiguration = Split(configuration["Contact:Recipients"]);
        return fromConfiguration.Count > 0
            ? (fromConfiguration, Source.Configuration)
            : ([], Source.None);
    }

    private static IReadOnlyList<string> Split(string? list) =>
        (list ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
