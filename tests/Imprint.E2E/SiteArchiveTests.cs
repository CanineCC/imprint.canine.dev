using System.Text.Json.Nodes;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// A retired site is archived from its settings: it leaves the dashboard shelves for a closed "Archived" list and
/// list_sites, its settings say so, and Restore brings it back.
/// </summary>
[Collection("editor")]
public sealed class SiteArchiveTests(EditorFixture fixture)
{
    [Fact]
    public async Task A_site_archived_in_settings_leaves_the_lists_and_can_be_restored()
    {
        // The fixture makes its own site on first open; ours comes after, so the editor's first site stays its.
        var page = await fixture.OpenEditor();
        using var mcp = new McpSession(fixture.BaseUrl);
        var name = $"Retired {Guid.NewGuid():N}"[..20];
        var created = await mcp.Call("create_site", new JsonObject { ["name"] = name });
        var siteId = created["siteId"]!.GetValue<string>();

        await page.GotoAsync($"/sites/{siteId}/settings");
        await page.WaitForInteractive();
        await page.Locator("[data-testid='site-archive']").ClickAsync();
        await page.Locator("[data-testid='site-archive-confirm']").ClickAsync();
        await Expect(page.Locator("[data-testid='site-archived']")).ToContainTextAsync("This site is archived");
        await Shot(page, "site-archived-settings.png");

        await page.GotoAsync("/");
        await page.WaitForInteractive();
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Sites" })).Not.ToContainTextAsync(name);
        var archived = page.Locator("[data-testid='archived-sites']");
        await archived.Locator("summary").ClickAsync();
        await Expect(archived).ToContainTextAsync(name);
        await Shot(page, "site-archived-dashboard.png");

        var listed = await mcp.CallArray("list_sites", new JsonObject());
        Assert.DoesNotContain(listed, s => s!["id"]!.GetValue<string>() == siteId);

        await archived.GetByRole(AriaRole.Link, new() { Name = name }).ClickAsync();
        await page.WaitForInteractive();
        await page.Locator("[data-testid='site-restore']").ClickAsync();
        await Expect(page.Locator("[data-testid='site-archived']")).ToHaveCountAsync(0);

        listed = await mcp.CallArray("list_sites", new JsonObject());
        Assert.Contains(listed, s => s!["id"]!.GetValue<string>() == siteId);
    }

    private static async Task Shot(IPage page, string name)
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, name), FullPage = true });
        }
    }
}
