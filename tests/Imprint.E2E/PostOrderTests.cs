using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// The articles list is the publishing plan: unpublished and published on two tabs, each article with its products,
/// in the site's order, which a person changes by dragging a row (or with its arrows) and which outlives a reload.
/// </summary>
[Collection("editor")]
public sealed class PostOrderTests(EditorFixture fixture)
{
    [Fact]
    public async Task Articles_are_split_by_tab_show_their_products_and_are_reordered_by_dragging()
    {
        var page = await fixture.OpenEditor();
        using var mcp = new McpSession(fixture.BaseUrl);
        var site = (await mcp.Call("create_blog", new JsonObject { ["name"] = "Ordered center", ["kind"] = "resource-center" }))["siteId"]!.GetValue<string>();
        var ids = new Dictionary<string, string>();
        foreach (var (slug, product) in new[] { ("alpha", "assert"), ("beta", "assay"), ("gamma", "cca"), ("live-one", "assure") })
        {
            ids[slug] = (await mcp.Call("create_post", new JsonObject
            {
                ["siteId"] = site, ["title"] = slug, ["slug"] = slug, ["locale"] = "en", ["kind"] = "essay",
                ["products"] = new JsonArray(product),
            }))["postId"]!.GetValue<string>();
            await mcp.Call("set_post_body", new JsonObject { ["postId"] = ids[slug], ["markdown"] = "# Hi\n\nWords.\n", ["locale"] = "en" });
        }

        await Publish(ids["live-one"]);
        await mcp.Call("set_post_order", new JsonObject
        {
            ["siteId"] = site, ["postIds"] = new JsonArray(ids["alpha"], ids["beta"], ids["gamma"], ids["live-one"]),
        });

        await page.GotoAsync($"/site/{site}/posts");
        await page.WaitForInteractive();
        var rows = page.Locator("[data-testid='post-row']");
        await Expect(page.Locator("[data-testid='posts-tab-unpublished']")).ToContainTextAsync("3");
        await Expect(rows).ToHaveCountAsync(3);
        await Expect(rows.Nth(0).Locator("[data-testid='post-products']")).ToHaveTextAsync("Assert");
        await Shot(page, "posts-order-unpublished.png");

        // Drag gamma onto alpha: gamma goes first.
        await rows.Nth(2).DragToAsync(rows.Nth(0));
        await Expect(rows.Nth(0)).ToHaveAttributeAsync("data-slug", "gamma");

        await page.ReloadAsync();
        await page.WaitForInteractive();
        Assert.Equal(["gamma", "alpha", "beta"], await Slugs(page));

        // The arrows do the same for a keyboard.
        await rows.Nth(0).Locator("[data-testid='post-down']").ClickAsync();
        await Expect(rows.Nth(0)).ToHaveAttributeAsync("data-slug", "alpha");

        await page.Locator("[data-testid='posts-tab-published']").ClickAsync();
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(rows.Nth(0)).ToHaveAttributeAsync("data-slug", "live-one");
        await Expect(rows.Nth(0).Locator("[data-testid='post-products']")).ToHaveTextAsync("Assure");
        await Shot(page, "posts-order-published.png");
    }

    private static async Task<string[]> Slugs(IPage page) =>
        [.. (await page.Locator("[data-testid='post-row']").EvaluateAllAsync<string[]>("rows => rows.map(r => r.dataset.slug)"))];

    private async Task Publish(string post)
    {
        using var http = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EditorFixture.AuthoringToken);
        using var response = await http.PostAsync($"/api/authoring/posts/{post}/publish", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task Shot(IPage page, string name)
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, name), FullPage = true });
        }
    }
}
