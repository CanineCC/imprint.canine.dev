using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks.Events;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Tests.Domain.Blocks;

public sealed class BlockEventSamples : IEventSampleProvider
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");

    public IEnumerable<object> Samples =>
    [
        new BlockDefined(BlockDefinitionId.New(), "Call to action", Spec()),
        new BlockRenamed("Hero call to action"),
        new BlockSpecChanged(Spec()),
        new BlockDeleted(),
    ];

    // A realistic symbol subtree so the sample exercises polymorphic node payloads,
    // localized text, links and the columns invariants — not just an empty stack.
    private static Node Spec() => new ColumnsNode
    {
        Id = NodeId.New(),
        Ratios = [2, 1],
        CollapseBelow = CollapseBreakpoint.Px768,
        Gap = Gap.Loose,
        Children = NodeList.Of(
            new StackNode
            {
                Id = NodeId.New(),
                Gap = Gap.Tight,
                Align = StackAlign.Start,
                Children = NodeList.Of(
                    new HeadingNode
                    {
                        Id = NodeId.New(),
                        Level = 2,
                        Text = LocalizedText.Of(En, "Ready to start?").With(Da, "Klar til at starte?"),
                    },
                    new RichTextNode
                    {
                        Id = NodeId.New(),
                        Html = LocalizedText.Of(En, "<p>Join <strong>today</strong>.</p>"),
                    }),
            },
            new StackNode
            {
                Id = NodeId.New(),
                Gap = Gap.Normal,
                Align = StackAlign.Center,
                Children = NodeList.Of(
                    new ButtonNode
                    {
                        Id = NodeId.New(),
                        Label = LocalizedText.Of(En, "Sign up"),
                        LinkTo = new ExternalLink("https://example.com/signup"),
                        Variant = ButtonVariant.Primary,
                    }),
            }),
    };
}
