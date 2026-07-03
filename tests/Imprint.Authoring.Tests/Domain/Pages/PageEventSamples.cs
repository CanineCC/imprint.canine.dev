using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;

namespace Imprint.Authoring.Tests.Domain.Pages;

// Fully-populated serialization samples for every Page event. The round-trip harness
// fails when an event type exists without a sample here, so payload shape changes are
// always exercised against the real registry serialization.
public sealed class PageEventSamples : IEventSampleProvider
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");

    public IEnumerable<object> Samples
    {
        get
        {
            yield return new PageCreated(PageId.New(), SiteId.New(), "about-us", En, "About us");
            yield return new TitleChanged(Da, "Om os");
            yield return new SlugChanged("kontakt");
            yield return new MetaChanged(En, "About — Acme", "What Acme is, and why it exists.");
            yield return new MetaChanged(Da, null, null);
            yield return new NodeAdded(NodeId.Root, 0, RichSection());
            yield return new NodeAdded(NodeId.New(), 3, RichSection());
            yield return new NodeMoved(NodeId.New(), NodeId.New(), 2);
            yield return new NodeRemoved(NodeId.New());
            yield return new NodeDuplicated(NodeId.New(), RichSection());
            yield return new NodePropsChanged(new ColumnsNode
            {
                Id = NodeId.New(),
                Ratios = [1, 2, 1],
                CollapseBelow = CollapseBreakpoint.Px768,
                Gap = Gap.Loose,
                Children = NodeList.Of(
                    new StackNode { Id = NodeId.New() },
                    new StackNode { Id = NodeId.New(), Gap = Gap.Tight, Align = StackAlign.Center },
                    new StackNode { Id = NodeId.New() }),
            });
            yield return new TextChanged(NodeId.New(), "html", En, "<p>Hello <strong>world</strong> &amp; friends.</p>");
            yield return new TextChanged(NodeId.New(), "text", Da, "Overskrift");
            yield return new BlockOverrideSet(NodeId.New(), NodeId.New(), "label", Da, "Læs mere");
            yield return new BlockOverrideSet(NodeId.New(), NodeId.New(), "html", En, null);
            yield return new BlockInstanceDetached(NodeId.New(), RichSection());
            yield return new PagePublished(7);
            yield return new PageUnpublished();
            yield return new PageDeleted();
        }
    }

    // Exercises every node type, every enum away from its default, and multi-locale
    // text — the worst realistic payload the store will ever hold.
    private static SectionNode RichSection() => new()
    {
        Id = NodeId.New(),
        Width = SectionWidth.Wide,
        Background = SectionBackground.SurfaceAlt,
        Padding = SectionPadding.Large,
        Children = NodeList.Of(
            new ColumnsNode
            {
                Id = NodeId.New(),
                Ratios = [2, 1],
                CollapseBelow = CollapseBreakpoint.Px480,
                Gap = Gap.Loose,
                Children = NodeList.Of(
                    new StackNode
                    {
                        Id = NodeId.New(),
                        Gap = Gap.Tight,
                        Align = StackAlign.Center,
                        Children = NodeList.Of(
                            new HeadingNode
                            {
                                Id = NodeId.New(),
                                Level = 1,
                                Text = LocalizedText.Of(En, "Hello").With(Da, "Hej"),
                            },
                            new RichTextNode
                            {
                                Id = NodeId.New(),
                                Html = LocalizedText
                                    .Of(En, "<p>Body with <strong>bold</strong> and <a href=\"https://example.com\">a link</a>.</p>")
                                    .With(Da, "<ul><li>Første</li><li>Anden</li></ul>"),
                            },
                            new ButtonNode
                            {
                                Id = NodeId.New(),
                                Label = LocalizedText.Of(En, "Read more").With(Da, "Læs mere"),
                                LinkTo = new PageLink(PageId.New()),
                                Variant = ButtonVariant.Ghost,
                            },
                            new ButtonNode
                            {
                                Id = NodeId.New(),
                                Label = LocalizedText.Of(En, "Docs"),
                                LinkTo = new ExternalLink("https://example.com/docs"),
                                Variant = ButtonVariant.Secondary,
                            }),
                    },
                    new StackNode
                    {
                        Id = NodeId.New(),
                        Children = NodeList.Of(
                            new ImageNode
                            {
                                Id = NodeId.New(),
                                AssetId = AssetId.New(),
                                Alt = LocalizedText.Of(En, "A pier at dusk").With(Da, "En mole i skumringen"),
                                Aspect = ImageAspect.Wide16x9,
                                Rounded = true,
                            },
                            new SvgNode
                            {
                                Id = NodeId.New(),
                                AssetId = AssetId.New(),
                                MaxWidthPx = 240,
                                Alt = LocalizedText.Of(En, "The Acme logo"),
                            },
                            new VideoNode
                            {
                                Id = NodeId.New(),
                                AssetId = AssetId.New(),
                                Mode = VideoMode.Ambient,
                            }),
                    }),
            },
            new GridNode
            {
                Id = NodeId.New(),
                MinItemPx = 200,
                Gap = Gap.Tight,
                Children = NodeList.Of(
                    new WidgetNode
                    {
                        Id = NodeId.New(),
                        Tag = "x-countdown",
                        Props = PropBag.Empty.With("until", "2027-01-01").With("label", "Launch"),
                    },
                    new BlockInstanceNode
                    {
                        Id = NodeId.New(),
                        DefinitionId = BlockDefinitionId.New(),
                        Overrides = OverrideSet.Empty
                            .With(NodeId.New(), "text", En, "Custom headline")
                            .With(NodeId.New(), "alt", Da, "Tilpasset alternativ tekst"),
                    },
                    new DividerNode { Id = NodeId.New() },
                    new SpacerNode { Id = NodeId.New(), Size = SpacerSize.Large })
            }),
    };
}
