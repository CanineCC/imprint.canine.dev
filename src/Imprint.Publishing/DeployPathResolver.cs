namespace Imprint.Publishing;

/// <summary>
/// Turns a site's configured environment folder (operator-typed text) into the safe
/// absolute path a publish pass may write into. Two modes, selected by
/// <see cref="PublishingOptions.DeployRoot"/>:
/// <list type="bullet">
/// <item>Root set (multi-tenant SaaS): the folder is a path <em>relative to the root</em>.
/// Leading separators are stripped so an absolute-looking value cannot escape, and any
/// value that still resolves outside the root (via <c>..</c>) is rejected — one tenant
/// can never reach another's files, or the system's.</item>
/// <item>Root null (single trusted operator): the folder is used as an absolute path as
/// typed, so the operator can publish straight into their own web roots.</item>
/// </list>
/// The resolver is pure and side-effect free; it never creates directories.
/// </summary>
public sealed class DeployPathResolver(PublishingOptions options)
{
    public string Resolve(string environmentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentPath);

        if (string.IsNullOrWhiteSpace(options.DeployRoot))
        {
            // Trust mode: the operator owns the target; take the path as given.
            return Path.GetFullPath(environmentPath);
        }

        var root = Path.GetFullPath(options.DeployRoot);

        // Treat the value as relative even when it looks rooted — Path.Combine honors a
        // rooted second argument and would let "/etc" or "C:\" escape the sandbox, so the
        // leading separators are removed first.
        var relative = environmentPath.Replace('\\', '/').TrimStart('/');
        var candidate = Path.GetFullPath(Path.Combine(root, relative));

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (candidate != root && !candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The publish folder '{environmentPath}' resolves outside the configured deploy root and was rejected.");
        }

        return candidate;
    }
}
