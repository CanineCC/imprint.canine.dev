using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Microsoft.Extensions.Logging;

namespace Imprint.Publishing;

/// <summary>What an environment folder currently holds — read from its own publish manifest, so it is always the truth on disk.</summary>
public sealed record EnvironmentDeployStatus(
    string Name,
    string ConfiguredPath,
    string ResolvedPath,
    bool Deployed,
    long SiteVersion,
    int PageCount,
    DateTimeOffset? DeployedAt,
    string? Error);

/// <summary>
/// The SaaS deploy plane: renders a site's published content into a named environment
/// folder, and promotes one environment's <em>exact rendered bytes</em> to the next.
/// Rendering is delegated to <see cref="SitePublisher"/> (each folder converges against
/// its own manifest); promotion is a directory mirror, which is sound precisely because
/// the static output is content-hashed and immutable — copying the files reproduces the
/// environment byte-for-byte, no re-render and no chance of the drafts having moved on.
/// Every folder passes through <see cref="DeployPathResolver"/> first, so a misconfigured
/// or malicious path can never write outside the sandbox.
/// </summary>
public sealed class SiteDeployService(
    SitePublisher publisher,
    SiteOverview sites,
    DeployPathResolver paths,
    PublishGate gate,
    ILogger<SiteDeployService> logger)
{
    /// <summary>Render the site's published content into the named environment's folder.</summary>
    public async Task<PublishReport> PublishToEnvironment(
        SiteId siteId, string environmentName, CancellationToken ct = default)
    {
        var (site, environment) = Resolve(siteId, environmentName);
        var folder = paths.Resolve(environment.Path);
        logger.LogInformation(
            "Publishing site {SiteId} to environment '{Environment}' at {Folder}.", siteId, environment.Name, folder);
        // The environment's own BaseUrl (when the operator set one) makes canonicals,
        // hreflang, sitemap locations and the robots sitemap pointer absolute against
        // THAT environment's public origin. Without one, output stays root-relative —
        // origin-portable, the long-standing default. Never a global BaseUrl, which
        // would be wrong for every site but one.
        return await publisher.Synchronize(new PublishTarget(site, folder, environment.BaseUrl), ct);
    }

    /// <summary>
    /// Copy the exact rendered bytes of <paramref name="fromEnvironment"/> onto
    /// <paramref name="toEnvironment"/> — the promotion. The target becomes a mirror of
    /// the source (extraneous files removed), so what was verified on Test is precisely
    /// what goes to Staging or Production. Byte-copy means the source's canonical origin
    /// travels with it: environments in one promotion pipeline should either share a
    /// site address or leave it unset (root-relative output is origin-portable).
    /// </summary>
    public async Task Promote(
        SiteId siteId, string fromEnvironment, string toEnvironment, CancellationToken ct = default)
    {
        var (_, from) = Resolve(siteId, fromEnvironment);
        var (_, to) = Resolve(siteId, toEnvironment);
        var fromFolder = paths.Resolve(from.Path);
        var toFolder = paths.Resolve(to.Path);

        // Same OR nested folders are both fatal to the mirror: if one is inside the other,
        // the copy re-reads its own output and the sweep deletes live files in the shared
        // subtree. Only fully-disjoint folders can be promoted between.
        if (SameOrNested(fromFolder, toFolder))
        {
            throw new InvalidOperationException(
                $"'{from.Name}' and '{to.Name}' point at the same folder or one nested inside the other; " +
                "a promotion needs two separate folders.");
        }

        if (!Directory.Exists(fromFolder))
        {
            throw new InvalidOperationException(
                $"'{from.Name}' has not been published yet, so there is nothing to promote to '{to.Name}'.");
        }

        logger.LogInformation(
            "Promoting site {SiteId} from '{From}' to '{To}' ({FromFolder} → {ToFolder}).",
            siteId, from.Name, to.Name, fromFolder, toFolder);

        // Under the shared publish gate: the mirror reads the source folder, which the
        // auto-sync background pass continuously rewrites, and writes the destination — two
        // writers over one folder must not overlap, or a promotion captures a torn
        // half-rendered snapshot (or crashes on a file swept mid-copy).
        await gate.RunExclusive(() => Task.Run(() => Mirror(fromFolder, toFolder, ct), ct), ct);
    }

    /// <summary>The live status of every environment configured on a site, in promotion order.</summary>
    public IReadOnlyList<EnvironmentDeployStatus> StatusOf(SiteId siteId)
    {
        var site = sites.Get(siteId);
        if (site is null)
        {
            return [];
        }

        var statuses = new List<EnvironmentDeployStatus>(site.Environments.Count);
        foreach (var environment in site.Environments)
        {
            string resolved;
            try
            {
                resolved = paths.Resolve(environment.Path);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                statuses.Add(new EnvironmentDeployStatus(
                    environment.Name, environment.Path, environment.Path, Deployed: false, 0, 0, null, e.Message));
                continue;
            }

            var manifestPath = Path.Combine(resolved, PublishManifest.FileName);
            try
            {
                var manifest = PublishManifest.Load(manifestPath);
                statuses.Add(manifest is null
                    ? new EnvironmentDeployStatus(
                        environment.Name, environment.Path, resolved, Deployed: false, 0, 0, null, null)
                    : new EnvironmentDeployStatus(
                        environment.Name,
                        environment.Path,
                        resolved,
                        Deployed: true,
                        manifest.SiteVersion,
                        manifest.Pages.Count,
                        File.GetLastWriteTimeUtc(manifestPath),
                        null));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // A present-but-unreadable manifest (permission bits, a locked file) must
                // degrade to a per-environment error, never take the whole status board
                // down — one bad folder must not blind the operator to the others.
                statuses.Add(new EnvironmentDeployStatus(
                    environment.Name, environment.Path, resolved, Deployed: false, 0, 0, null,
                    $"Could not read this environment's status: {e.Message}"));
            }
        }

        return statuses;
    }

    private (Site Site, DeployEnvironment Environment) Resolve(SiteId siteId, string environmentName)
    {
        var site = sites.Get(siteId)
            ?? throw new InvalidOperationException($"No site {siteId} exists.");
        var environment = site.Environments
            .FirstOrDefault(e => string.Equals(e.Name, environmentName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Site '{site.Name}' has no environment named '{environmentName}'.");
        return (site, environment);
    }

    /// <summary>True when a and b are the same folder, or one is an ancestor of the other.</summary>
    private static bool SameOrNested(string a, string b)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var na = Path.TrimEndingDirectorySeparator(Path.GetFullPath(a));
        var nb = Path.TrimEndingDirectorySeparator(Path.GetFullPath(b));
        return string.Equals(na, nb, comparison)
            || na.StartsWith(nb + Path.DirectorySeparatorChar, comparison)
            || nb.StartsWith(na + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>Make <paramref name="destination"/> an exact copy of <paramref name="source"/>: copy every file, delete anything extra.</summary>
    private static void Mirror(string source, string destination, CancellationToken ct)
    {
        Directory.CreateDirectory(destination);

        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(source, file);
            wanted.Add(relative);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        // Sweep files that exist in the destination but not the source (a page removed
        // since the last promotion, a rotated content hash).
        foreach (var file in Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories))
        {
            if (!wanted.Contains(Path.GetRelativePath(destination, file)))
            {
                File.Delete(file);
            }
        }

        // Collapse now-empty directories, deepest first.
        foreach (var directory in Directory
                     .EnumerateDirectories(destination, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
    }
}
