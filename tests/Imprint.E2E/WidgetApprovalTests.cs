using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// The widget request/approval loop, driven like a human: an editor requests a widget from
/// the insert picker, a server owner reviews and approves it on /admin/widgets, and it then
/// appears in the picker for editors — live, no restart ("once approved, available to all").
/// </summary>
[Collection("editor")]
public sealed class WidgetApprovalTests(EditorFixture fixture)
{
    [Fact]
    public async Task Requesting_a_widget_then_approving_it_makes_it_available_in_the_picker()
    {
        var page = await fixture.OpenEditor();

        // ---- editor: open the insert picker and choose "Request a new widget…"
        await page.Select(page.Node("heading", 0));
        await page.Keyboard.PressAsync("/");
        await page.WaitForSelectorAsync(".ed-picker");
        await page.ClickAsync(".ed-picker-item:has-text('Request a new widget')");

        // ---- fill the request modal and submit for review (nothing runs yet)
        await page.WaitForSelectorAsync(".wr-modal");
        await page.FillAsync("#wr-tag", "x-e2e-hello");
        await page.FillAsync("#wr-name", "E2E Hello");
        await page.FillAsync(
            "#wr-bundle",
            "export default class extends HTMLElement { connectedCallback() { this.textContent = 'hi'; } }");
        await page.ClickAsync("button:has-text('Submit for review')");
        await page.WaitForSelectorAsync("text=Submitted for review");

        // ---- admin: the submission is pending; approving it publishes it to editors
        await page.GotoAsync("/admin/widgets");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync("text=x-e2e-hello");
        await page.ClickAsync("button:has-text('Approve')");
        await page.WaitForSelectorAsync(".aw-message:has-text('Approved')");

        // ---- editor again: the approved widget is now offered in the picker, no restart
        var editor = await fixture.OpenEditor();
        await editor.Select(editor.Node("heading", 0));
        await editor.Keyboard.PressAsync("/");
        await editor.WaitForSelectorAsync(".ed-picker");
        await editor.FillAsync(".ed-picker .ed-input", "E2E Hello");
        await editor.WaitForSelectorAsync(".ed-picker-item:has-text('E2E Hello')");
    }

    [Fact]
    public async Task A_rejected_request_never_becomes_available()
    {
        var page = await fixture.OpenEditor();

        await page.Select(page.Node("heading", 0));
        await page.Keyboard.PressAsync("/");
        await page.WaitForSelectorAsync(".ed-picker");
        await page.ClickAsync(".ed-picker-item:has-text('Request a new widget')");

        await page.WaitForSelectorAsync(".wr-modal");
        await page.FillAsync("#wr-tag", "x-e2e-nope");
        await page.FillAsync("#wr-name", "E2E Nope");
        await page.FillAsync("#wr-bundle", "export default class extends HTMLElement {}");
        await page.ClickAsync("button:has-text('Submit for review')");
        await page.WaitForSelectorAsync("text=Submitted for review");

        // ---- admin: reject with a reason
        await page.GotoAsync("/admin/widgets");
        await page.WaitForInteractive();
        await page.WaitForSelectorAsync("text=x-e2e-nope");
        await page.FillAsync(".aw-reason", "Not needed for the demo.");
        await page.ClickAsync("button:has-text('Reject')");
        await page.WaitForSelectorAsync(".aw-message:has-text('Rejected')");

        // ---- editor: a rejected tag is never offered for insertion
        var editor = await fixture.OpenEditor();
        await editor.Select(editor.Node("heading", 0));
        await editor.Keyboard.PressAsync("/");
        await editor.WaitForSelectorAsync(".ed-picker");
        await editor.FillAsync(".ed-picker .ed-input", "E2E Nope");
        await editor.WaitForSelectorAsync(".ed-picker-empty");
        Assert.Equal(0, await editor.Locator(".ed-picker-item:has-text('E2E Nope')").CountAsync());
    }
}
