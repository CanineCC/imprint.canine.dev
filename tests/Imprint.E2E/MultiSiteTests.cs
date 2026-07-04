using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The multi-site SaaS shell, driven like a human: the dashboard lists sites and opens
/// each into ITS OWN editor; a new site is created and entered; and a site's settings
/// gear configures a publish folder and deploys the real static output into it.
/// </summary>
[Collection("editor")]
public sealed class MultiSiteTests(EditorFixture fixture)
{
    [Fact]
    public async Task Dashboard_creates_a_site_and_opens_it_by_card()
    {
        var page = await fixture.OpenEditor(); // ensures ≥1 site, lands in an editor

        // Reach the dashboard via the editor's "← All sites" back link.
        await page.ClickAsync("a.ed-back");
        await page.WaitForSelectorAsync(".dash-grid");
        Assert.Equal(1, await page.Locator(".dash-open-new").CountAsync());
        Assert.True(await page.Locator(".dash-card:not(.dash-card-new)").CountAsync() >= 1);

        // Create a uniquely-named site and confirm the editor opened onto IT.
        var name = "E2E Site " + Guid.NewGuid().ToString("N")[..6];
        await page.CreateSiteViaDashboard(name);
        Assert.Equal(name, (await page.Locator(".ed-site-name").InnerTextAsync()).Trim());

        // Back on the dashboard, the new site now has a card; clicking it re-opens that
        // exact site (per-site editing, not "always the first site").
        await page.ClickAsync("a.ed-back");
        await page.WaitForSelectorAsync(".dash-grid");
        var card = page.Locator(".dash-card", new PageLocatorOptions { HasTextString = name });
        Assert.Equal(1, await card.CountAsync());

        await card.Locator(".dash-open").ClickAsync();
        await page.WaitForURLAsync("**/edit/**", new PageWaitForURLOptions { Timeout = 30_000 });
        await page.WaitForInteractive();
        Assert.Equal(name, (await page.Locator(".ed-site-name").InnerTextAsync()).Trim());
    }

    [Fact]
    public async Task Configure_a_publish_folder_then_publish_the_site_into_it()
    {
        // A fresh site so its home page is an unpublished draft we can publish here.
        var page = await fixture.NewPage();
        var name = "Deploy Site " + Guid.NewGuid().ToString("N")[..6];
        await page.CreateSiteViaDashboard(name);

        // Publish the current page — settings deploys PUBLISHED content, so there must be some.
        await page.ClickAsync(".ed-publish button.ed-btn-primary");
        await page.WaitForSelectorAsync(".ed-badge-ok", new PageWaitForSelectorOptions { Timeout = 15_000 });

        // Open this site's settings via the top-bar gear.
        await page.ClickAsync("a.ed-gear");
        await page.WaitForSelectorAsync("h2:has-text('Publish folders')");
        await page.WaitForInteractive();

        // Configure one environment pointing at a fresh, empty folder.
        var envFolder = Path.Combine(fixture.DataDirectory, "env-" + Guid.NewGuid().ToString("N")[..8]);
        await page.ClickAsync("button:has-text('Add environment')");
        await page.FillAsync(".env-name", "Test");
        await page.FillAsync(".env-path", envFolder);
        await page.ClickAsync("button:has-text('Save publish folders')");

        // The deploy panel now offers "Publish to Test"; do it.
        await page.WaitForSelectorAsync("button:has-text('Publish to Test')");
        await page.ClickAsync("button:has-text('Publish to Test')");

        // The real static site materializes in the configured folder.
        var indexPath = Path.Combine(envFolder, "index.html");
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!File.Exists(indexPath) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(300);
        }

        Assert.True(File.Exists(indexPath), $"env publish did not write {indexPath}");
        var html = await File.ReadAllTextAsync(indexPath);
        Assert.Contains("<html", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-node-id", html, StringComparison.Ordinal); // static output, not editor markup
    }
}
