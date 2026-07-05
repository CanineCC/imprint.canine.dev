namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// A named publish destination for a site — e.g. "Test" or "Production" — and the folder
/// its static output is written to. Environments are ordered within a site, so a
/// promotion pipeline (Test → Staging → Production) has a well-defined "next". The
/// <see cref="Path"/> is stored verbatim as the operator typed it; the filesystem policy
/// that decides which paths are allowed to be written (sandbox root, traversal) lives in
/// the deploy infrastructure, not the domain.
///
/// <see cref="BaseUrl"/> is the environment's public origin (e.g.
/// <c>https://example.com</c>): when set, a publish to this environment renders
/// canonical/hreflang links, sitemap locations and the robots sitemap pointer as
/// absolute URLs against it. Null keeps the long-standing default — root-relative,
/// origin-portable output. The property is additive on the stored
/// <c>site.environments-changed</c> payload, so events written before it existed
/// deserialize with null and behave exactly as they always did.
/// </summary>
public sealed record DeployEnvironment(string Name, string Path, string? BaseUrl = null);
