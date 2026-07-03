using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RenderMode = Imprint.Rendering.RenderMode;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// Renders node views to HTML strings through the real <see cref="HtmlRenderer"/> —
/// the same rendering path the static publisher uses, so these tests exercise the
/// actual markup contract rather than a bUnit-style approximation.
/// </summary>
internal static class RenderHarness
{
    public static readonly Locale En = new("en");
    public static readonly Locale Da = new("da");

    public static RenderContext Context(RenderMode mode) => new()
    {
        Mode = mode,
        Locale = En,
        DefaultLocale = En,
        ResolveAsset = _ => null,
        ResolvePagePath = _ => null,
        ResolveBlock = _ => null,
        ResolveWidget = _ => null,
    };

    public static AssetRenderInfo ImageAsset() => new(
        AssetKind.Image,
        AssetStatus.Ready,
        "/assets/img-960.webp",
        [
            new ImageSource("/assets/img-480.webp", 480, 320),
            new ImageSource("/assets/img-960.webp", 960, 640),
            new ImageSource("/assets/img-1440.webp", 1440, 960),
        ],
        1440,
        960,
        null,
        LocalizedText.Of(En, "Library alt"));

    public static AssetRenderInfo VideoAsset() => new(
        AssetKind.Video, AssetStatus.Ready, "/assets/clip.webm", [], null, null, null, LocalizedText.Empty);

    public static AssetRenderInfo SvgAsset(string? defaultAlt = null) => new(
        AssetKind.Vector,
        AssetStatus.Ready,
        "/assets/gfx.svg",
        [],
        null,
        null,
        "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\"/></svg>",
        defaultAlt is null ? LocalizedText.Empty : LocalizedText.Of(En, defaultAlt));

    public static Task<string> RenderNode(RenderContext ctx, Node node) =>
        Render(WrapInContext(ctx, builder =>
        {
            builder.OpenComponent<NodeView>(0);
            builder.AddComponentParameter(1, nameof(NodeView.Node), node);
            builder.CloseComponent();
        }));

    public static Task<string> RenderPage(RenderContext ctx, params Node[] roots) =>
        Render(WrapInContext(ctx, builder =>
        {
            builder.OpenComponent<PageView>(0);
            builder.AddComponentParameter(1, nameof(PageView.Roots), (IReadOnlyList<Node>)roots);
            builder.CloseComponent();
        }));

    private static RenderFragment WrapInContext(RenderContext ctx, RenderFragment content) => builder =>
    {
        builder.OpenComponent<CascadingValue<RenderContext>>(0);
        builder.AddComponentParameter(1, "Value", ctx);
        builder.AddComponentParameter(2, "ChildContent", content);
        builder.CloseComponent();
    };

    private static async Task<string> Render(RenderFragment fragment)
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(FragmentHost.Content)] = fragment });
            var root = await renderer.RenderComponentAsync<FragmentHost>(parameters);
            return root.ToHtmlString();
        });
    }
}

internal sealed class FragmentHost : ComponentBase
{
    [Parameter] public RenderFragment? Content { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Content);
}
