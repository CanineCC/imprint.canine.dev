using Imprint.Authoring.Domain.Assets;

namespace Imprint.Editor.Components.Panels.Assets;

/// <summary>Tiny display helpers shared by the assets panel and the asset picker.</summary>
internal static class AssetUi
{
    public static string Glyph(AssetKind kind) => kind switch
    {
        AssetKind.Image => "🖼",
        AssetKind.Vector => "✒",
        AssetKind.Video => "🎞",
        _ => "📄",
    };

    public static string KindLabel(AssetKind kind) => kind switch
    {
        AssetKind.Image => "Image",
        AssetKind.Vector => "Graphic",
        AssetKind.Video => "Video",
        _ => "File",
    };

    public static string Bytes(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):0.#} GB",
        >= 1024L * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        >= 1024L => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes} B",
    };
}
