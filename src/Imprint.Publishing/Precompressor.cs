using System.IO.Compression;

namespace Imprint.Publishing;

/// <summary>
/// Precompression for the text outputs: every published <c>.html/.css/.js/.xml/.txt/.svg</c>
/// gets <c>.br</c> and <c>.gz</c> siblings, compressed once at publish time at maximum
/// quality — static output is written rarely and served often, so spending brotli
/// quality 11 here is the whole point of precompressing.
/// </summary>
internal static class Precompressor
{
    private static readonly HashSet<string> TextExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".html", ".css", ".js", ".xml", ".txt", ".svg" };

    public static bool IsCompressible(string relativePath) =>
        TextExtensions.Contains(Path.GetExtension(relativePath));

    public static byte[] Brotli(byte[] content)
    {
        using var buffer = new MemoryStream();
        // SmallestSize maps to brotli quality 11.
        using (var brotli = new BrotliStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            brotli.Write(content);
        }

        return buffer.ToArray();
    }

    public static byte[] Gzip(byte[] content)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(content);
        }

        return buffer.ToArray();
    }
}
