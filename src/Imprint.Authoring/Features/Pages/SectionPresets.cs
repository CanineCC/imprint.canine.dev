using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Features.Pages;

/// <summary>
/// The section starters offered by the editor's insert picker. A preset is a factory,
/// not a value: every call mints fresh node ids, so the resulting event is
/// self-contained and replay-deterministic. Text lands in whatever locale the caller
/// passes (the site's default).
/// </summary>
public sealed record SectionPreset(string Key, string Name, string Description, Func<Locale, SectionNode> Build);

public static class SectionPresets
{
    public static readonly IReadOnlyList<SectionPreset> All =
    [
        new("hero", "Hero", "Big headline, supporting line and a call to action.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Padding = SectionPadding.Large,
                Children = NodeList.Of(new StackNode
                {
                    Id = NodeId.New(),
                    Align = StackAlign.Center,
                    Gap = Gap.Loose,
                    Children = NodeList.Of(
                        new HeadingNode { Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(locale, "Make something people remember") },
                        new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, "<p>One honest sentence about what you do and who it helps.</p>") },
                        new ButtonNode { Id = NodeId.New(), Label = LocalizedText.Of(locale, "Get started"), Variant = ButtonVariant.Primary }),
                }),
            }),

        new("feature-grid", "Feature grid", "Three cards that each sell one idea.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Background = SectionBackground.Surface,
                Children = NodeList.Of(new GridNode
                {
                    Id = NodeId.New(),
                    MinItemPx = 260,
                    Children = NodeList.Of(
                        FeatureCard(locale, "Fast", "Static pages, one stylesheet, no framework in sight."),
                        FeatureCard(locale, "Honest", "Every change is an event. Nothing is ever lost."),
                        FeatureCard(locale, "Yours", "Apache-2.0, no lock-in, boring dependencies.")),
                }),
            }),

        new("split", "Text + image", "Two columns: words on one side, a picture on the other.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new ColumnsNode
                {
                    Id = NodeId.New(),
                    Ratios = [1, 1],
                    CollapseBelow = CollapseBreakpoint.Px640,
                    Children = NodeList.Of(
                        new StackNode
                        {
                            Id = NodeId.New(),
                            Gap = Gap.Normal,
                            Children = NodeList.Of(
                                new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(locale, "Show, then tell") },
                                new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, "<p>Pair a strong image with a short story. Keep the paragraph tight; let the picture breathe.</p>") }),
                        },
                        new StackNode
                        {
                            Id = NodeId.New(),
                            Children = NodeList.Of(new ImageNode { Id = NodeId.New(), Aspect = ImageAspect.Wide16x9, Rounded = true }),
                        }),
                }),
            }),

        new("text", "Text", "A plain writing section.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new StackNode
                {
                    Id = NodeId.New(),
                    Children = NodeList.Of(
                        new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(locale, "A heading") },
                        new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, "<p>Start writing here.</p>") }),
                }),
            }),

        new("gallery", "Gallery", "A responsive grid of images.",
            _ => new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new GridNode
                {
                    Id = NodeId.New(),
                    MinItemPx = 220,
                    Gap = Gap.Tight,
                    Children = NodeList.Of(
                        new ImageNode { Id = NodeId.New(), Aspect = ImageAspect.Square, Rounded = true },
                        new ImageNode { Id = NodeId.New(), Aspect = ImageAspect.Square, Rounded = true },
                        new ImageNode { Id = NodeId.New(), Aspect = ImageAspect.Square, Rounded = true },
                        new ImageNode { Id = NodeId.New(), Aspect = ImageAspect.Square, Rounded = true }),
                }),
            }),

        new("cta", "Call to action", "A highlighted band with one clear ask.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Background = SectionBackground.Primary,
                Padding = SectionPadding.Large,
                Children = NodeList.Of(new StackNode
                {
                    Id = NodeId.New(),
                    Align = StackAlign.Center,
                    Children = NodeList.Of(
                        new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(locale, "Ready when you are") },
                        new ButtonNode { Id = NodeId.New(), Label = LocalizedText.Of(locale, "Talk to us"), Variant = ButtonVariant.Secondary }),
                }),
            }),

        new("footer", "Footer", "Closing section with small print.",
            locale => new SectionNode
            {
                Id = NodeId.New(),
                Background = SectionBackground.SurfaceAlt,
                Children = NodeList.Of(new StackNode
                {
                    Id = NodeId.New(),
                    Gap = Gap.Tight,
                    Children = NodeList.Of(
                        new DividerNode { Id = NodeId.New() },
                        new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, "<p>Built with Imprint. All rights reserved.</p>") }),
                }),
            }),
    ];

    public static SectionPreset? Find(string key) => All.FirstOrDefault(preset => preset.Key == key);

    private static StackNode FeatureCard(Locale locale, string title, string body) => new()
    {
        Id = NodeId.New(),
        Gap = Gap.Tight,
        Children = NodeList.Of(
            new HeadingNode { Id = NodeId.New(), Level = 3, Text = LocalizedText.Of(locale, title) },
            new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, $"<p>{body}</p>") }),
    };
}
