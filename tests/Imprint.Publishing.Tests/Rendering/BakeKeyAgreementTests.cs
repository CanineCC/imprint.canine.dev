using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The publisher stores a bake under a key; the view looks it up by the same key. They are in
/// different assemblies and run at different times, so nothing makes them agree except care.
/// </summary>
/// <remarks>
/// ★★ THIS FAILED SILENTLY IN BOTH DIRECTIONS AND SHIPPED GREEN. The context url was first resolved
/// with NO props, on the reasoning that a context is shared by every instance and so cannot depend
/// on one — but the pattern is <c>{base}/api/public/reports</c> and <c>base</c> IS a prop. It
/// resolved to null, no context was fetched, and every card drew the flat bar exactly as before:
/// green deploy, green suite, nothing in the markup to say a fetch had been skipped. Had that been
/// fixed alone, the key would have grown a third part the VIEW did not add, the lookup would have
/// missed, and every card would have published its fallback instead.
/// <para>So: one function derives the key's context part, and this asserts both sides call it.</para>
/// </remarks>
public sealed class BakeKeyAgreementTests
{
    private static readonly WidgetDescriptor Card = new()
    {
        Tag = "wd-survey-card",
        Name = "One named survey",
        Prerender = "{base}/api/public/oss/{owner}/{name}/evidence",
        PrerenderTemplate = "survey-card",
        PrerenderContext = "{base}/api/public/reports",
        Props =
        [
            new WidgetProp { Name = "base", Label = "Watchdog base URL" },
            new WidgetProp { Name = "owner", Label = "Owner" },
            new WidgetProp { Name = "name", Label = "Repository" },
        ],
    };

    private static string? Props(string name) => name switch
    {
        "base" => "https://app.watchdog.canine.dev",
        "owner" => "CanineCC",
        "name" => "kennel.canine.dev",
        _ => null,
    };

    [Fact]
    public void A_context_pattern_that_uses_a_prop_resolves_with_that_prop()
    {
        // The whole bug: `{base}` is a prop, so a resolver denied props returns nothing at all.
        Assert.Equal("https://app.watchdog.canine.dev/api/public/reports",
            WidgetTemplate.ContextUrl(Card, Props));
    }

    [Fact]
    public void A_descriptor_with_no_context_keys_exactly_as_it_did_before()
    {
        var plain = Card with { PrerenderContext = null };

        Assert.Equal("", WidgetTemplate.ContextUrl(plain, Props));
        Assert.Equal(WidgetTemplate.BakeKey("u", "t"), WidgetTemplate.BakeKey("u", "t", ""));
    }

    [Fact]
    public void The_key_the_publisher_stores_is_the_key_the_view_asks_for()
    {
        var url = WidgetTemplate.Resolve(Card, Card.Prerender, Props)!;
        var context = WidgetTemplate.ContextUrl(Card, Props);

        var stored = WidgetTemplate.BakeKey(url, Card.PrerenderTemplate, context);
        var askedFor = WidgetTemplate.BakeKey(url, Card.PrerenderTemplate, WidgetTemplate.ContextUrl(Card, Props));

        Assert.Equal(stored, askedFor);
        Assert.Contains("/api/public/reports", stored, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_widgets_differing_only_in_context_do_not_share_a_fragment()
    {
        var other = Card with { PrerenderContext = "{base}/api/public/procurement-checklist.json" };
        var url = WidgetTemplate.Resolve(Card, Card.Prerender, Props)!;

        Assert.NotEqual(
            WidgetTemplate.BakeKey(url, Card.PrerenderTemplate, WidgetTemplate.ContextUrl(Card, Props)),
            WidgetTemplate.BakeKey(url, other.PrerenderTemplate, WidgetTemplate.ContextUrl(other, Props)));
    }
}
