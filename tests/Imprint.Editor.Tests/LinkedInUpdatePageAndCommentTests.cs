using System.Net;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;
using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// A page's update and a first comment against a fake HTTP handler: nothing here reaches LinkedIn. A page sender goes
/// through the page connection (its app, its token, its refresh), with the images owned by the page; a member keeps
/// their own connection even when a page is connected; a comment goes to the post as the same author.
/// </summary>
public sealed class LinkedInUpdatePageAndCommentTests : IDisposable
{
    private const string Person = "urn:li:person:Ab3";
    private const string Page = "urn:li:organization:141183954";
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);

    private static readonly LinkedInOptions MemberApp = LinkedInOptions.ForMembers(new()
    {
        ClientId = "member-client", ClientSecret = "member-secret", Version = "202610", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://api.test", OAuthBase = "https://oauth.test",
    });

    // The page app on its own address and version, so a call that went through the wrong app would show.
    private static readonly LinkedInOptions PageApp = new()
    {
        ClientId = "page-client", ClientSecret = "page-secret", Version = "202609", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://pages.test", OAuthBase = "https://page-oauth.test",
    };

    private readonly string _data = Path.Combine(Path.GetTempPath(), $"imprint-linkedin-page-{Guid.NewGuid():N}");

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
        public List<(HttpMethod Method, string Url, string? Authorization, string? Version, string Body)> Seen { get; } = [];
        public Queue<Func<HttpResponseMessage>> Answers { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Seen.Add((request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(),
                request.Headers.TryGetValues("Linkedin-Version", out var v) ? string.Join(",", v) : null,
                request.Content is null ? "" : request.Content is StringContent or FormUrlEncodedContent
                    ? await request.Content.ReadAsStringAsync(ct)
                    : $"{(await request.Content.ReadAsByteArrayAsync(ct)).Length} bytes"));
            return Answers.Dequeue()();
        }
    }

    private static LinkedInTokens PageTokens(DateTimeOffset? accessExpires = null, DateTimeOffset? refreshExpires = null, string? refresh = "page-refresh") =>
        new("page-access", accessExpires ?? Now.AddDays(30), refresh, refreshExpires ?? Now.AddDays(300), Now.AddDays(-30));

    private (LinkedInUpdateClient Client, FakeHandler Http, LinkedInTokenStore PageStore) Client(LinkedInTokens? page, bool member = true)
    {
        var handler = new FakeHandler();
        var http = new HttpClient(handler);
        var clock = new FixedClock(Now);
        var members = new LinkedInMemberTokenStore(_data);
        if (member)
        {
            members.Write(new LinkedInMember(Person, "Jimmy Borch", new LinkedInTokens("member-access", Now.AddDays(30), null, null, Now.AddDays(-1))));
        }

        var pageStore = new LinkedInTokenStore(_data);
        if (page is not null)
        {
            pageStore.Write(page);
        }

        var connection = new LinkedInPageConnection(http, PageApp, pageStore, NullLogger.Instance, clock);
        return (new LinkedInUpdateClient(http, MemberApp, members, NullLogger<LinkedInUpdateClient>.Instance, clock, connection), handler, pageStore);
    }

    private static Func<HttpResponseMessage> Init(int n) => () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"value\":{\"uploadUrl\":\"https://upload.test/" + n + "\",\"image\":\"urn:li:image:" + n + "\"}}"),
    };

    private static Func<HttpResponseMessage> Status(HttpStatusCode code, string body = "{}") => () => new HttpResponseMessage(code) { Content = new StringContent(body) };

    private static Func<HttpResponseMessage> Created(string urn, string body = "") => () =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(body) };
        response.Headers.Add("x-restli-id", urn);
        return response;
    };

    private static LinkedInUpdateRequest Update(string author, int images) => new(
        author, "Your AI says it's fixed. #Assure",
        [.. Enumerable.Range(1, images).Select(i => new LinkedInUpdateImageFile([1, 2, 3], "image/png", $"Board {i}"))]);

    [Fact]
    public async Task A_pages_update_uploads_its_images_as_the_page_and_posts_as_the_page_with_the_page_token()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Status(HttpStatusCode.Created));
        http.Answers.Enqueue(Created("urn:li:share:7"));

        var result = await client.PostUpdateAsync(Update(Page, 1), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:share:7"), (result.Status, result.Urn));
        Assert.Equal(3, http.Seen.Count);
        Assert.Equal((HttpMethod.Post, "https://pages.test/rest/images?action=initializeUpload", "Bearer page-access", "202609"),
            (http.Seen[0].Method, http.Seen[0].Url, http.Seen[0].Authorization, http.Seen[0].Version));
        Assert.Equal(Page, JsonNode.Parse(http.Seen[0].Body)!["initializeUploadRequest"]!["owner"]!.GetValue<string>());
        Assert.Equal((HttpMethod.Put, "https://upload.test/1", "Bearer page-access"), (http.Seen[1].Method, http.Seen[1].Url, http.Seen[1].Authorization));
        Assert.Equal((HttpMethod.Post, "https://pages.test/rest/posts", "Bearer page-access", "202609"),
            (http.Seen[2].Method, http.Seen[2].Url, http.Seen[2].Authorization, http.Seen[2].Version));
        var post = JsonNode.Parse(http.Seen[2].Body)!;
        Assert.Equal(Page, post["author"]!.GetValue<string>());
        Assert.Equal("urn:li:image:1", post["content"]!["media"]!["id"]!.GetValue<string>());
        Assert.Equal(@"Your AI says it's fixed. {hashtag|\#|Assure}", post["commentary"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_member_keeps_their_own_connection_when_a_page_is_connected_too()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Status(HttpStatusCode.Created));
        http.Answers.Enqueue(Created("urn:li:share:8"));

        await client.PostUpdateAsync(Update(Person, 1), CancellationToken.None);

        Assert.All(http.Seen, seen => Assert.Equal("Bearer member-access", seen.Authorization));
        Assert.Equal(["https://api.test/rest/images?action=initializeUpload", "https://upload.test/1", "https://api.test/rest/posts"], http.Seen.Select(s => s.Url));
        Assert.Equal(Person, JsonNode.Parse(http.Seen[0].Body)!["initializeUploadRequest"]!["owner"]!.GetValue<string>());
        Assert.Equal(Person, JsonNode.Parse(http.Seen[2].Body)!["author"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_401_on_a_pages_post_renews_the_page_token_once_and_sends_again()
    {
        var (client, http, store) = Client(PageTokens());
        http.Answers.Enqueue(Status(HttpStatusCode.Unauthorized));
        http.Answers.Enqueue(Status(HttpStatusCode.OK, """{"access_token":"page-access-2","expires_in":5184000}"""));
        http.Answers.Enqueue(Created("urn:li:share:9"));

        var result = await client.PostUpdateAsync(Update(Page, 0), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:share:9"), (result.Status, result.Urn));
        Assert.Equal("https://page-oauth.test/accessToken", http.Seen[1].Url);
        Assert.Contains("grant_type=refresh_token", http.Seen[1].Body, StringComparison.Ordinal);
        Assert.Contains("client_id=page-client", http.Seen[1].Body, StringComparison.Ordinal);
        Assert.Equal("Bearer page-access-2", http.Seen[2].Authorization);
        Assert.Equal(("page-access-2", "page-refresh"), (store.Read()!.AccessToken, store.Read()!.RefreshToken));
    }

    [Fact]
    public async Task An_expired_page_token_is_renewed_before_anything_is_sent()
    {
        var (client, http, _) = Client(PageTokens(accessExpires: Now.AddSeconds(-1)));
        http.Answers.Enqueue(Status(HttpStatusCode.OK, """{"access_token":"page-access-3","expires_in":5184000}"""));
        http.Answers.Enqueue(Created("urn:li:share:10"));

        await client.PostUpdateAsync(Update(Page, 0), CancellationToken.None);

        Assert.Equal("https://page-oauth.test/accessToken", http.Seen[0].Url);
        Assert.Equal("Bearer page-access-3", http.Seen[1].Authorization);
    }

    [Fact]
    public async Task A_401_that_cannot_be_renewed_is_a_refusal_that_says_to_connect_the_page_again()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Status(HttpStatusCode.Unauthorized));
        http.Answers.Enqueue(Status(HttpStatusCode.BadRequest));

        var result = await client.PostUpdateAsync(Update(Page, 0), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.False(result.Retryable);
        Assert.Contains("connects it again", result.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_page_is_configured_only_while_its_connection_can_give_a_token()
    {
        Assert.False(Client(page: null).Client.IsConfiguredFor(Page));
        Assert.True(Client(PageTokens()).Client.IsConfiguredFor(Page));
        Assert.True(Client(PageTokens(accessExpires: Now.AddDays(-1))).Client.IsConfiguredFor(Page));
        Assert.False(Client(PageTokens(accessExpires: Now.AddDays(-1), refreshExpires: Now.AddDays(-1))).Client.IsConfiguredFor(Page));
        Assert.False(Client(PageTokens(accessExpires: Now.AddDays(-1), refresh: null)).Client.IsConfiguredFor(Page));
        Assert.True(Client(PageTokens()).Client.IsConfiguredFor(Person));
        Assert.False(Client(PageTokens()).Client.IsConfiguredFor("urn:li:organizationBrand:1"));
    }

    [Fact]
    public async Task A_page_that_is_not_connected_is_refused_and_nothing_is_sent()
    {
        var (client, http, _) = Client(page: null);

        var result = await client.PostUpdateAsync(Update(Page, 1), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.Contains("page is not connected", result.Reason, StringComparison.Ordinal);
        Assert.Empty(http.Seen);
    }

    [Fact]
    public async Task A_first_comment_goes_to_the_post_as_the_page_in_plain_text_and_reads_back_its_urn()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Created("6643206422739898368",
            """{"id":"6643206422739898368","commentUrn":"urn:li:comment:(urn:li:activity:6641861161010679808,6643206422739898368)"}"""));

        var result = await client.CommentAsync(new LinkedInCommentRequest(Page, "urn:li:share:7", "Read it (all of it): https://canine.dev/assure/#start"), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:comment:(urn:li:activity:6641861161010679808,6643206422739898368)"), (result.Status, result.Urn));
        var (method, url, authorization, version, body) = Assert.Single(http.Seen);
        Assert.Equal((HttpMethod.Post, "https://pages.test/rest/socialActions/urn%3Ali%3Ashare%3A7/comments", "Bearer page-access", "202609"),
            (method, url, authorization, version));
        Assert.Equal(
            """{"actor":"urn:li:organization:141183954","object":"urn:li:share:7","message":{"text":"Read it (all of it): https://canine.dev/assure/#start"}}""",
            body);
    }

    [Fact]
    public async Task A_created_comment_without_a_body_is_known_by_the_id_LinkedIn_puts_in_the_header()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Created("6643206422739898368"));

        var result = await client.CommentAsync(new LinkedInCommentRequest(Page, "urn:li:ugcPost:5", "Link"), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "6643206422739898368"), (result.Status, result.Urn));
        Assert.Equal("https://pages.test/rest/socialActions/urn%3Ali%3AugcPost%3A5/comments", Assert.Single(http.Seen).Url);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, LinkedInSendStatus.Refused, true)]
    [InlineData(HttpStatusCode.Forbidden, LinkedInSendStatus.Refused, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, LinkedInSendStatus.Refused, false)]
    [InlineData(HttpStatusCode.InternalServerError, LinkedInSendStatus.Unknown, false)]
    [InlineData(HttpStatusCode.GatewayTimeout, LinkedInSendStatus.Unknown, false)]
    public async Task A_comments_answer_is_read_as_a_posts_is(HttpStatusCode code, LinkedInSendStatus status, bool retryable)
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Status(code, """{"message":"no"}"""));

        var result = await client.CommentAsync(new LinkedInCommentRequest(Page, "urn:li:share:7", "Link"), CancellationToken.None);

        Assert.Equal((status, retryable), (result.Status, result.Retryable));
    }

    [Fact]
    public async Task A_members_comment_goes_out_with_the_members_own_token()
    {
        var (client, http, _) = Client(PageTokens());
        http.Answers.Enqueue(Created("1", """{"commentUrn":"urn:li:comment:(urn:li:activity:1,1)"}"""));

        await client.CommentAsync(new LinkedInCommentRequest(Person, "urn:li:share:7", "Link"), CancellationToken.None);

        var seen = Assert.Single(http.Seen);
        Assert.Equal(("https://api.test/rest/socialActions/urn%3Ali%3Ashare%3A7/comments", "Bearer member-access", "202610"),
            (seen.Url, seen.Authorization, seen.Version));
        Assert.Equal(Person, JsonNode.Parse(seen.Body)!["actor"]!.GetValue<string>());
    }

    [Fact]
    public void The_page_app_asks_for_posting_and_commenting_as_the_page_and_the_member_app_is_unchanged()
    {
        Assert.Contains("scope=w_organization_social%20w_organization_social_feed", LinkedInOAuth.AuthorizeUrl(PageApp, "s"), StringComparison.Ordinal);
        Assert.Contains("scope=openid%20profile%20w_member_social&", LinkedInOAuth.AuthorizeUrl(MemberApp, "s") + "&", StringComparison.Ordinal);
    }
}
