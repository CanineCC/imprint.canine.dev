using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing;

/// <summary>
/// The last bake that worked, for every widget fragment this site publishes — so a fetch that fails
/// keeps the page it already had instead of replacing it with a placeholder.
/// </summary>
/// <remarks>
/// <para>★★ THE FAILURE THIS EXISTS TO END, IN FULL. On 2026-09-23 a deploy queued during an outage
/// ran the moment the runner came back and restarted the publisher, which synchronises everything at
/// startup. The service those pages bake from was still forty minutes from returning, so every fetch
/// returned null, and a null fetch meant the bake key was simply absent from the result — which
/// publishes the widget's fallback. Six pages were rewritten in one second, and
/// <c>watchdog.canine.dev/pricing</c> served "Pricing is published here" with no prices for two and a
/// half hours, to everyone, because a bake is a STATIC FILE: it is rendered once and served until
/// something renders it again.</para>
///
/// <para>★★ "THE SITE IS CORRECT WITHOUT IT" WAS THE WRONG ASSUMPTION. The fetcher documents the
/// trade-off it was built on — a publish must never fail because a fragment did not arrive — and that
/// is right for a page that has NEVER had a bake, where a placeholder is the honest answer. It is
/// wrong for a page that HAS one: there the choice is not between a bake and a placeholder, it is
/// between yesterday's prices and no prices, and yesterday's prices win every time. Stale is a
/// smaller lie than absent, and it is the same rule the standard's own sweep already follows — a
/// listing it could not read withdraws nothing.</para>
///
/// <para>★★ ON DISK, BECAUSE THE FAILURE WAS A RESTART. A cache in memory would have been empty at
/// exactly the moment this was needed: the publisher had just started. So the last good bake outlives
/// the process, the deploy and the machine.</para>
///
/// <para>★ BESIDE THE OUTPUT, NEVER INSIDE IT. The output root is served to the public verbatim, so a
/// state directory under it would be a URL. This sits in a sibling <c>.bakes/</c> directory, one
/// folder per site, which no site root contains.</para>
/// </remarks>
public sealed class BakeMemory
{
    private readonly string? _root;
    private readonly ILogger? _logger;

    /// <summary>Remembers bakes beside <paramref name="outputRoot"/>, under a sibling directory.</summary>
    /// <param name="outputRoot">The site's published output directory.</param>
    /// <param name="logger">Optional; a memory that cannot be read or written says so and carries on.</param>
    public BakeMemory(string outputRoot, ILogger? logger = null)
    {
        _logger = logger;
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            return;
        }

        var full = Path.GetFullPath(outputRoot);
        var parent = Path.GetDirectoryName(full);
        var site = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar));
        _root = parent is null or "" || site is null or ""
            ? null
            : Path.Combine(parent, ".bakes", site);
    }

    /// <summary>The last bake that worked for <paramref name="bakeKey"/>, or null when there is none.</summary>
    /// <remarks>★ Never throws: a memory that cannot be read is a memory that has nothing, which is
    /// exactly the state a first publish is in, and the caller already handles it.</remarks>
    public string? Recall(string bakeKey)
    {
        var path = PathFor(bakeKey);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var remembered = File.ReadAllText(path, Encoding.UTF8);
            return remembered.Length == 0 ? null : remembered;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _logger?.LogWarning(e, "Could not read the remembered bake at {Path}.", path);
            return null;
        }
    }

    /// <summary>Records a bake that worked, replacing whatever was remembered before.</summary>
    public void Remember(string bakeKey, string html)
    {
        var path = PathFor(bakeKey);
        if (path is null || string.IsNullOrEmpty(html))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // ★ Written to a temporary file and moved, so a publish interrupted mid-write leaves the
            //   PREVIOUS memory intact rather than a truncated one. A half-written memory would be
            //   worse than none: it would be recalled and published.
            var scratch = path + ".tmp";
            File.WriteAllText(scratch, html, Encoding.UTF8);
            File.Move(scratch, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _logger?.LogWarning(e, "Could not remember the bake at {Path}.", path);
        }
    }

    /// <summary>One file per bake key, named by its hash — a key is a URL and a URL is not a filename.</summary>
    private string? PathFor(string bakeKey)
    {
        if (_root is null || string.IsNullOrEmpty(bakeKey))
        {
            return null;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(bakeKey)));
        return Path.Combine(_root, hash + ".html");
    }
}
