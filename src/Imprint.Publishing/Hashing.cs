using System.Security.Cryptography;

namespace Imprint.Publishing;

/// <summary>
/// Content hashing for published file names and manifest staleness keys:
/// <c>hash16</c> = the first 16 hex characters of SHA-256 over the content. 64 bits of
/// a cryptographic hash is far beyond collision risk at CMS scale while keeping file
/// names readable; being content-derived is what makes the whole output byte-stable.
/// </summary>
internal static class Hashing
{
    public static string Hash16(ReadOnlySpan<byte> content)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(content, hash);
        return Convert.ToHexStringLower(hash[..8]);
    }

    public static async Task<string> Hash16(Stream content, CancellationToken ct)
    {
        var hash = await SHA256.HashDataAsync(content, ct);
        return Convert.ToHexStringLower(hash.AsSpan(0, 8));
    }
}
