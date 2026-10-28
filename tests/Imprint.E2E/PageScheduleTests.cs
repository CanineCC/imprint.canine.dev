using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Imprint.E2E;

/// <summary>
/// Scheduling a page from the publish bar, the way a person does it: Schedule…, a date and a time,
/// Schedule. The bar then says when the page goes live and offers to cancel; cancelling puts the
/// Schedule… button back.
/// </summary>
[Collection("editor")]
public sealed class PageScheduleTests(EditorFixture fixture)
{
    [Fact]
    public async Task A_page_is_scheduled_from_the_publish_bar_and_cancelled_again()
    {
        var page = await fixture.OpenEditor();

        await page.ClickAsync("[data-testid='page-schedule']");
        await page.FillAsync("[data-testid='page-schedule-at']", "2027-04-15T08:30");
        await page.ClickAsync("[data-testid='page-schedule-confirm']");

        await Expect(page.Locator("[data-testid='page-scheduled']")).ToHaveTextAsync("Scheduled 15 Apr 2027, 08:30");
        if (Environment.GetEnvironmentVariable("IMPRINT_E2E_SHOTS") is { Length: > 0 } shots)
        {
            await page.Locator(".ed-publish").ScreenshotAsync(new() { Path = Path.Combine(shots, "page-scheduled.png") });
        }

        await page.ClickAsync("[data-testid='page-schedule-cancel']");
        await Expect(page.Locator("[data-testid='page-schedule']")).ToBeVisibleAsync();
        await Expect(page.Locator("[data-testid='page-scheduled']")).ToHaveCountAsync(0);
    }
}
