namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// A named publish destination for a site — e.g. "Test" or "Production" — and the folder
/// its static output is written to. Environments are ordered within a site, so a
/// promotion pipeline (Test → Staging → Production) has a well-defined "next". The
/// <see cref="Path"/> is stored verbatim as the operator typed it; the filesystem policy
/// that decides which paths are allowed to be written (sandbox root, traversal) lives in
/// the deploy infrastructure, not the domain.
/// </summary>
public sealed record DeployEnvironment(string Name, string Path);
