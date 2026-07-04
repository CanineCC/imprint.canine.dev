using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

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

    [Fact]
    public async Task Promote_copies_the_site_from_one_environment_to_the_next()
    {
        var page = await fixture.NewPage();
        var name = "Promote Site " + Guid.NewGuid().ToString("N")[..6];
        await page.CreateSiteViaDashboard(name);

        // Publish a page so there is content to deploy and then promote.
        await page.ClickAsync(".ed-publish button.ed-btn-primary");
        await page.WaitForSelectorAsync(".ed-badge-ok", new PageWaitForSelectorOptions { Timeout = 15_000 });

        await page.ClickAsync("a.ed-gear");
        await page.WaitForSelectorAsync("h2:has-text('Publish folders')");
        await page.WaitForInteractive();

        var testFolder = Path.Combine(fixture.DataDirectory, "prom-test-" + Guid.NewGuid().ToString("N")[..6]);
        var prodFolder = Path.Combine(fixture.DataDirectory, "prom-prod-" + Guid.NewGuid().ToString("N")[..6]);
        await AddEnvironment(page, index: 0, "Test", testFolder);
        await AddEnvironment(page, index: 1, "Prod", prodFolder);
        await page.ClickAsync("button:has-text('Save publish folders')");

        // Deploy to Test, then promote Test → Prod.
        await page.WaitForSelectorAsync("button:has-text('Publish to Test')");
        await page.ClickAsync("button:has-text('Publish to Test')");
        var testIndex = Path.Combine(testFolder, "index.html");
        await WaitForFile(testIndex);

        await page.ClickAsync("button:has-text('Promote')");
        var prodIndex = Path.Combine(prodFolder, "index.html");
        await WaitForFile(prodIndex);

        // Prod is a byte-exact copy of Test — the promotion ships precisely what was verified.
        Assert.Equal(await File.ReadAllBytesAsync(testIndex), await File.ReadAllBytesAsync(prodIndex));
    }

    [Fact]
    public async Task Environment_rows_reorder_and_the_new_order_persists()
    {
        var page = await fixture.NewPage();
        var name = "Reorder Site " + Guid.NewGuid().ToString("N")[..6];
        await page.CreateSiteViaDashboard(name);

        await page.ClickAsync("a.ed-gear");
        await page.WaitForSelectorAsync("h2:has-text('Publish folders')");
        await page.WaitForInteractive();

        await AddEnvironment(page, index: 0, "Alpha", Path.Combine(fixture.DataDirectory, "reorder-a"));
        await AddEnvironment(page, index: 1, "Beta", Path.Combine(fixture.DataDirectory, "reorder-b"));

        // Move Beta (row 2) up: it becomes row 1, Alpha drops to row 2. Expect() auto-
        // retries, so it waits out the Blazor Server re-render round-trip.
        await page.Locator(".env-row").Nth(1).Locator("button[aria-label='Move up']").ClickAsync();
        await Expect(page.Locator(".env-name").Nth(0)).ToHaveValueAsync("Beta");
        await Expect(page.Locator(".env-name").Nth(1)).ToHaveValueAsync("Alpha");

        // Save, reload, and confirm ConfigureEnvironments stored the reordered list.
        await page.ClickAsync("button:has-text('Save publish folders')");
        await page.WaitForSelectorAsync("button:has-text('Publish to Beta')");
        await page.ReloadAsync();
        await page.WaitForInteractive();
        await Expect(page.Locator(".env-name").Nth(0)).ToHaveValueAsync("Beta");
        await Expect(page.Locator(".env-name").Nth(1)).ToHaveValueAsync("Alpha");
    }

    private static async Task AddEnvironment(IPage page, int index, string name, string path)
    {
        await page.ClickAsync("button:has-text('Add environment')");
        await page.Locator(".env-name").Nth(index).FillAsync(name);
        await page.Locator(".env-path").Nth(index).FillAsync(path);
    }

    private static async Task WaitForFile(string path, int seconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(300);
        }

        Assert.True(File.Exists(path), $"expected file to be written: {path}");
    }
}
