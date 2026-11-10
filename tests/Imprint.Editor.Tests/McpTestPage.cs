using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>Page connections for MCP tests that read a site: one on an install where LinkedIn pages are not set up,
/// and one a page admin has connected.</summary>
internal static class McpTestPage
{
    /// <summary>No page app and no stored token.</summary>
    public static LinkedInPageConnection NotSetUp { get; } = new(
        new HttpClient(), new LinkedInOptions(),
        new LinkedInTokenStore(Path.Combine(Path.GetTempPath(), $"imprint-no-page-{Guid.NewGuid():N}")), NullLogger.Instance);

    /// <summary>A page app, and tokens stored in <paramref name="dataDirectory"/> as a page admin's Connect stores them.</summary>
    public static LinkedInPageConnection Connected(string dataDirectory, LinkedInTokens tokens, TimeProvider clock)
    {
        var store = new LinkedInTokenStore(dataDirectory);
        store.Write(tokens);
        return new LinkedInPageConnection(
            new HttpClient(), new LinkedInOptions { ClientId = "page-client", ClientSecret = "page-secret" }, store, NullLogger.Instance, clock);
    }
}
