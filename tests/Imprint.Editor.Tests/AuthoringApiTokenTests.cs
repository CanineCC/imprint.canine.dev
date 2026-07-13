using Imprint.Editor.Api;
using Microsoft.AspNetCore.Http;

namespace Imprint.Editor.Tests;

/// <summary>
/// The authoring API's bearer-token gate — the sole authorization boundary of a headless write path
/// into the CMS, so it must fail closed. Verifies: the correct token (via either accepted header)
/// passes through to the endpoint; a missing, blank, malformed, or wrong token is rejected with 401
/// and NEVER invokes the endpoint.
/// </summary>
public sealed class AuthoringApiTokenTests
{
    private const string Secret = "s3cret-authoring-token-value";

    private static async Task<(bool passed, object? result)> Invoke(Action<HttpRequest> setup)
    {
        var filter = new BearerTokenFilter(Secret);
        var http = new DefaultHttpContext();
        setup(http.Request);
        var ctx = EndpointFilterInvocationContext.Create(http);
        var passed = false;
        var result = await filter.InvokeAsync(ctx, _ =>
        {
            passed = true;
            return ValueTask.FromResult<object?>("ENDPOINT-RAN");
        });
        return (passed, result);
    }

    [Fact]
    public async Task Correct_bearer_token_passes_through()
    {
        var (passed, result) = await Invoke(r => r.Headers.Authorization = $"Bearer {Secret}");
        Assert.True(passed);
        Assert.Equal("ENDPOINT-RAN", result);
    }

    [Fact]
    public async Task Correct_custom_header_token_passes_through()
    {
        var (passed, result) = await Invoke(r => r.Headers["X-Imprint-Authoring-Token"] = Secret);
        Assert.True(passed);
        Assert.Equal("ENDPOINT-RAN", result);
    }

    [Fact]
    public async Task No_token_is_rejected_and_endpoint_never_runs()
    {
        var (passed, result) = await Invoke(_ => { });
        Assert.False(passed);
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Theory]
    [InlineData("Bearer wrong-token")]
    [InlineData("Bearer ")]
    [InlineData("s3cret-authoring-token-value")]      // missing the "Bearer " scheme prefix
    [InlineData("Basic czNjcmV0")]                     // wrong scheme
    public async Task Bad_authorization_header_is_rejected(string header)
    {
        var (passed, result) = await Invoke(r => r.Headers.Authorization = header);
        Assert.False(passed);
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Wrong_custom_header_token_is_rejected()
    {
        var (passed, result) = await Invoke(r => r.Headers["X-Imprint-Authoring-Token"] = "not-the-token");
        Assert.False(passed);
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }
}
