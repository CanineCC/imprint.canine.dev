using Imprint.Editor.Contact;
using Microsoft.Extensions.Configuration;

namespace Imprint.Editor.Tests;

/// <summary>
/// The /api/contact recipient precedence: the submitting site's contact-form widget
/// prop wins, <c>Contact:Recipients</c> config is the fallback, and with neither the
/// resolution is None (the intake journals the lead instead of emailing it).
/// </summary>
public sealed class ContactRecipientResolverTests
{
    private static IConfiguration Config(string? recipients = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(recipients is null
                ? []
                : new Dictionary<string, string?> { ["Contact:Recipients"] = recipients })
            .Build();

    [Fact]
    public void Widget_prop_recipients_beat_the_config_fallback()
    {
        var resolver = new ContactRecipientResolver(Config("fallback@canine.dev"), _ => "sales@canine.dev");

        var (recipients, source) = resolver.Resolve("canine.dev");

        Assert.Equal(ContactRecipientResolver.Source.WidgetProp, source);
        Assert.Equal(["sales@canine.dev"], recipients);
    }

    [Fact]
    public void Unmatched_site_falls_back_to_config_recipients()
    {
        // The lookup returns null for a host no site claims — config still delivers.
        var resolver = new ContactRecipientResolver(Config("fallback@canine.dev"), _ => null);

        var (recipients, source) = resolver.Resolve("unknown.example");

        Assert.Equal(ContactRecipientResolver.Source.Configuration, source);
        Assert.Equal(["fallback@canine.dev"], recipients);
    }

    [Fact]
    public void Blank_widget_prop_falls_back_to_config_recipients()
    {
        // An editor can empty the prop; blank means "unset", not "email nobody".
        var resolver = new ContactRecipientResolver(Config("fallback@canine.dev"), _ => "   ");

        var (recipients, source) = resolver.Resolve("canine.dev");

        Assert.Equal(ContactRecipientResolver.Source.Configuration, source);
        Assert.Equal(["fallback@canine.dev"], recipients);
    }

    [Fact]
    public void No_widget_lookup_wired_uses_config_recipients()
    {
        var resolver = new ContactRecipientResolver(Config("a@canine.dev, b@canine.dev"));

        var (recipients, source) = resolver.Resolve("canine.dev");

        Assert.Equal(ContactRecipientResolver.Source.Configuration, source);
        Assert.Equal(["a@canine.dev", "b@canine.dev"], recipients);
    }

    [Fact]
    public void Comma_separated_widget_prop_is_split_and_trimmed()
    {
        var resolver = new ContactRecipientResolver(Config(), _ => " sales@canine.dev ,, ceo@canine.dev ");

        var (recipients, source) = resolver.Resolve("canine.dev");

        Assert.Equal(ContactRecipientResolver.Source.WidgetProp, source);
        Assert.Equal(["sales@canine.dev", "ceo@canine.dev"], recipients);
    }

    [Fact]
    public void Nothing_configured_anywhere_resolves_to_none()
    {
        var resolver = new ContactRecipientResolver(Config(), _ => null);

        var (recipients, source) = resolver.Resolve("canine.dev");

        Assert.Equal(ContactRecipientResolver.Source.None, source);
        Assert.Empty(recipients);
    }

    [Fact]
    public void The_submitted_site_is_handed_to_the_widget_lookup_verbatim()
    {
        string? seen = "unset";
        var resolver = new ContactRecipientResolver(Config(), site => { seen = site; return null; });

        resolver.Resolve("www.canine.dev");

        Assert.Equal("www.canine.dev", seen);
    }
}
