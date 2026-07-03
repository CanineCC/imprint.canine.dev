namespace Imprint.E2E;

[Collection("editor")]
public sealed class SmokeTests(EditorFixture fixture)
{
    [Fact]
    public async Task Editor_boots_and_serves_the_app()
    {
        var page = await fixture.NewPage();
        var response = await page.GotoAsync("/");

        Assert.NotNull(response);
        Assert.True(response.Ok, $"GET / returned {response.Status}");
        await page.WaitForSelectorAsync("h1");
    }
}
