using System.Net;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// The LinkedIn client against a fake HTTP handler: nothing here reaches LinkedIn. What matters is what each
/// answer means for a second attempt, that the text arrives as signed, and that the tokens stay in their file.
/// </summary>
public sealed class LinkedInClientTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly LinkedInOptions App = new()
    {
        ClientId = "client", ClientSecret = "secret", Version = "202610", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://api.test", OAuthBase = "https://oauth.test",
    };

    private static readonly LinkedInPostRequest Request = new(
        "urn:li:organization:42", "Read it (new) #cai @canine", "https://rc.example.com/first/", "First", "A dek.");

    private readonly string _data = Path.Combine(Path.GetTempPath(), $"imprint-linkedin-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_data))
        {
            Directory.Delete(_data, recursive: true);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Seen { get; } = [];
        public Queue<Func<HttpResponseMessage>> Answers { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Seen.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return Answers.Dequeue()();
        }
    }

    private (LinkedInClient Client, FakeHandler Http, LinkedInTokenStore Tokens) Client(LinkedInTokens? tokens)
    {
        var handler = new FakeHandler();
        var store = new LinkedInTokenStore(_data);
        if (tokens is not null)
        {
            store.Write(tokens);
        }

        return (new LinkedInClient(new HttpClient(handler), App, store, NullLogger<LinkedInClient>.Instance, new FixedClock(Now)),
            handler, store);
    }

    private static LinkedInTokens Valid() => new("access-1", Now.AddDays(30), "refresh-1", Now.AddDays(300), Now.AddDays(-30));

    private static HttpResponseMessage Status(HttpStatusCode code, string body = "{}") =>
        new(code) { Content = new StringContent(body) };

    [Fact]
    public async Task A_201_is_posted_with_the_urn_LinkedIn_returns_and_the_request_is_the_article_card()
    {
        var (client, http, _) = Client(Valid());
        http.Answers.Enqueue(() =>
        {
            var created = Status(HttpStatusCode.Created);
            created.Headers.Add("x-restli-id", "urn:li:share:7");
            return created;
        });

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Posted, result.Status);
        Assert.Equal("urn:li:share:7", result.Urn);
        var (sent, body) = Assert.Single(http.Seen);
        Assert.Equal("https://api.test/rest/posts", sent.RequestUri!.ToString());
        Assert.Equal("Bearer access-1", sent.Headers.Authorization!.ToString());
        Assert.Equal("202610", Assert.Single(sent.Headers.GetValues("Linkedin-Version")));
        Assert.Equal("2.0.0", Assert.Single(sent.Headers.GetValues("X-Restli-Protocol-Version")));
        var json = JsonNode.Parse(body)!;
        Assert.Equal("urn:li:organization:42", (string)json["author"]!);
        Assert.Equal(@"Read it \(new\) \#cai \@canine", (string)json["commentary"]!);
        Assert.Equal("PUBLIC", (string)json["visibility"]!);
        Assert.Equal("PUBLISHED", (string)json["lifecycleState"]!);
        Assert.Equal("https://rc.example.com/first/", (string)json["content"]!["article"]!["source"]!);
        Assert.Equal("First", (string)json["content"]!["article"]!["title"]!);
        Assert.Equal("A dek.", (string)json["content"]!["article"]!["description"]!);
    }

    [Fact]
    public async Task A_rate_limit_is_a_retryable_refusal_with_LinkedIns_wait()
    {
        var (client, http, _) = Client(Valid());
        http.Answers.Enqueue(() =>
        {
            var limited = Status(HttpStatusCode.TooManyRequests, """{"message":"slow down"}""");
            limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120));
            return limited;
        });

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.True(result.Retryable);
        Assert.Equal(TimeSpan.FromMinutes(2), result.RetryAfter);
        Assert.Contains("slow down", result.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_refusal_that_will_not_change_is_final()
    {
        var (client, http, _) = Client(Valid());
        http.Answers.Enqueue(() => Status(HttpStatusCode.Forbidden, """{"message":"ACCESS_DENIED"}"""));

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.False(result.Retryable);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task A_server_error_or_conflict_is_unknown_because_it_may_have_posted(HttpStatusCode code)
    {
        var (client, http, _) = Client(Valid());
        http.Answers.Enqueue(() => Status(code));

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task A_401_refreshes_the_token_once_retries_and_keeps_the_refresh_token()
    {
        var (client, http, tokens) = Client(Valid());
        http.Answers.Enqueue(() => Status(HttpStatusCode.Unauthorized));
        http.Answers.Enqueue(() => Status(HttpStatusCode.OK, """{"access_token":"access-2","expires_in":5184000}"""));
        http.Answers.Enqueue(() =>
        {
            var created = Status(HttpStatusCode.Created);
            created.Headers.Add("x-restli-id", "urn:li:share:8");
            return created;
        });

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Posted, result.Status);
        Assert.Equal(3, http.Seen.Count);
        Assert.Equal("https://oauth.test/accessToken", http.Seen[1].Request.RequestUri!.ToString());
        Assert.Contains("grant_type=refresh_token", http.Seen[1].Body, StringComparison.Ordinal);
        Assert.Equal("Bearer access-2", http.Seen[2].Request.Headers.Authorization!.ToString());
        var stored = tokens.Read()!;
        Assert.Equal("access-2", stored.AccessToken);
        Assert.Equal("refresh-1", stored.RefreshToken);
    }

    [Fact]
    public async Task An_expired_access_token_is_refreshed_before_sending()
    {
        var (client, http, _) = Client(Valid() with { AccessExpiresAt = Now.AddSeconds(-1) });
        http.Answers.Enqueue(() => Status(HttpStatusCode.OK, """{"access_token":"access-3","expires_in":60,"refresh_token":"refresh-2","refresh_token_expires_in":100}"""));
        http.Answers.Enqueue(() => Status(HttpStatusCode.Created));

        var result = await client.PostAsync(Request, CancellationToken.None);

        // 201 without the id header: it posted, but we cannot point at it, so the scheduler records unknown.
        Assert.Equal(LinkedInSendStatus.Posted, result.Status);
        Assert.Null(result.Urn);
        Assert.Equal("Bearer access-3", http.Seen[1].Request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Not_connected_is_a_refusal_and_sends_nothing()
    {
        var (client, http, _) = Client(null);

        var result = await client.PostAsync(Request, CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.False(result.Retryable);
        Assert.Empty(http.Seen);
        Assert.False(client.IsConfigured);
    }

    [Fact]
    public void The_token_file_is_readable_by_its_owner_only()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new LinkedInTokenStore(_data);
        store.Write(Valid());

        var mode = File.GetUnixFileMode(Path.Combine(_data, "secrets", "linkedin-token.json"));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode);
        Assert.Equal("refresh-1", store.Read()!.RefreshToken);
    }

    [Fact]
    public void The_authorize_url_asks_for_page_posting_and_returns_to_the_registered_callback()
    {
        var url = LinkedInOAuth.AuthorizeUrl(App, "abc");

        Assert.StartsWith("https://oauth.test/authorization?response_type=code&client_id=client", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fimprint.example.com%2Flinkedin%2Fcallback", url, StringComparison.Ordinal);
        Assert.Contains("scope=w_organization_social", url, StringComparison.Ordinal);
        Assert.Contains("state=abc", url, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "abd", false)]
    [InlineData(null, "abc", false)]
    [InlineData("abc", null, false)]
    [InlineData("", "", false)]
    public void The_callback_state_must_match_the_cookie_of_the_browser_that_started(string? cookie, string? given, bool ok) =>
        Assert.Equal(ok, LinkedInOAuth.StateMatches(cookie, given));
}
