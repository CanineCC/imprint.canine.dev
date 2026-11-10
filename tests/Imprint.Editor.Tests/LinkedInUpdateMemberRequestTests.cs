using System.Net;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;
using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// A member's update, request by request and byte by byte: the addresses, the headers, the token and the bodies a
/// person sender's update sends today. These are pinned so that adding page senders and first comments cannot change
/// what goes out for a member (the owner's own posts are planned weeks ahead and must not notice either change).
/// </summary>
public sealed class LinkedInUpdateMemberRequestTests : IDisposable
{
    private const string Person = "urn:li:person:Ab3";
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly LinkedInOptions MemberApp = LinkedInOptions.ForMembers(new()
    {
        ClientId = "client", ClientSecret = "secret", Version = "202610", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://api.test", OAuthBase = "https://oauth.test",
    });

    // Danish letters, a line break, brackets and hashtags: what the owner's posts carry.
    private const string Text = "Sikkerhedsviden har aldrig været lettere at finde (2026).\n\nLæs mere: canine.dev #AI #ledelse";

    private readonly string _data = Path.Combine(Path.GetTempPath(), $"imprint-linkedin-pin-{Guid.NewGuid():N}");

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

    private sealed record Seen(string Method, string Url, string? Authorization, string? Version, string? Restli, string? ContentType, string Body);

    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<Seen> Requests { get; } = [];
        public Queue<Func<HttpResponseMessage>> Answers { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? ""
                : request.Content is StringContent ? await request.Content.ReadAsStringAsync(ct)
                : Convert.ToHexString(await request.Content.ReadAsByteArrayAsync(ct));
            Requests.Add(new Seen(
                request.Method.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(),
                Header(request, "Linkedin-Version"), Header(request, "X-Restli-Protocol-Version"),
                request.Content?.Headers.ContentType?.ToString(), body));
            return Answers.Dequeue()();
        }

        private static string? Header(HttpRequestMessage request, string name) =>
            request.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
    }

    private (LinkedInUpdateClient Client, FakeHandler Http) Client()
    {
        var handler = new FakeHandler();
        var members = new LinkedInMemberTokenStore(_data);
        members.Write(new LinkedInMember(Person, "Jimmy Borch", new LinkedInTokens("member-access", Now.AddDays(30), null, null, Now.AddDays(-1))));
        return (new LinkedInUpdateClient(new HttpClient(handler), MemberApp, members, NullLogger<LinkedInUpdateClient>.Instance, new FixedClock(Now)), handler);
    }

    private static Func<HttpResponseMessage> Init(int n) => () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"value\":{\"uploadUrl\":\"https://upload.test/" + n + "\",\"image\":\"urn:li:image:" + n + "\"}}"),
    };

    private static Func<HttpResponseMessage> Uploaded => () => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("") };

    private static Func<HttpResponseMessage> Created(string urn) => () =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("") };
        response.Headers.Add("x-restli-id", urn);
        return response;
    };

    private static LinkedInUpdateRequest Request(int images) => new(
        Person, Text, [.. Enumerable.Range(1, images).Select(i => new LinkedInUpdateImageFile([1, 2, (byte)i], "image/png", $"Graf {i} – æøå"))]);

    private static Seen Init(string owner) => new(
        "POST", "https://api.test/rest/images?action=initializeUpload", "Bearer member-access", "202610", "2.0.0",
        "application/json; charset=utf-8", "{\"initializeUploadRequest\":{\"owner\":\"" + owner + "\"}}");

    private static Seen Put(int n) => new("PUT", $"https://upload.test/{n}", "Bearer member-access", null, null, "image/png", $"01020{n}");

    private static Seen Post(string body) => new(
        "POST", "https://api.test/rest/posts", "Bearer member-access", "202610", "2.0.0", "application/json; charset=utf-8", body);

    private const string Commentary =
        "\"commentary\":\"Sikkerhedsviden har aldrig v\\u00E6ret lettere at finde \\\\(2026\\\\).\\n\\nL\\u00E6s mere: canine.dev {hashtag|\\\\#|AI} {hashtag|\\\\#|ledelse}\"";

    private const string Envelope =
        "\"visibility\":\"PUBLIC\",\"distribution\":{\"feedDistribution\":\"MAIN_FEED\",\"targetEntities\":[],\"thirdPartyDistributionChannels\":[]}," +
        "\"lifecycleState\":\"PUBLISHED\",\"isReshareDisabledByAuthor\":false";

    [Fact]
    public async Task One_image_goes_up_as_the_member_then_one_post_with_the_member_token_and_the_same_body()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Uploaded);
        http.Answers.Enqueue(Created("urn:li:share:1"));

        var result = await client.PostUpdateAsync(Request(1), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:share:1"), (result.Status, result.Urn));
        Assert.Equal(
            [
                Init(Person),
                Put(1),
                Post("{\"author\":\"urn:li:person:Ab3\"," + Commentary + "," + Envelope +
                     ",\"content\":{\"media\":{\"id\":\"urn:li:image:1\",\"altText\":\"Graf 1 \\u2013 \\u00E6\\u00F8\\u00E5\"}}}"),
            ],
            http.Requests);
    }

    [Fact]
    public async Task Several_images_go_up_in_order_and_one_post_carries_them_all()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Uploaded);
        http.Answers.Enqueue(Init(2));
        http.Answers.Enqueue(Uploaded);
        http.Answers.Enqueue(Created("urn:li:ugcPost:2"));

        var result = await client.PostUpdateAsync(Request(2), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:ugcPost:2"), (result.Status, result.Urn));
        Assert.Equal(
            [
                Init(Person),
                Put(1),
                Init(Person),
                Put(2),
                Post("{\"author\":\"urn:li:person:Ab3\"," + Commentary + "," + Envelope +
                     ",\"content\":{\"multiImage\":{\"images\":[" +
                     "{\"id\":\"urn:li:image:1\",\"altText\":\"Graf 1 \\u2013 \\u00E6\\u00F8\\u00E5\"}," +
                     "{\"id\":\"urn:li:image:2\",\"altText\":\"Graf 2 \\u2013 \\u00E6\\u00F8\\u00E5\"}]}}}"),
            ],
            http.Requests);
    }

    [Fact]
    public async Task Without_images_one_post_goes_out_and_nothing_else()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Created("urn:li:share:3"));

        await client.PostUpdateAsync(Request(0), CancellationToken.None);

        Assert.Equal([Post("{\"author\":\"urn:li:person:Ab3\"," + Commentary + "," + Envelope + "}")], http.Requests);
    }

    [Fact]
    public async Task A_member_whose_connection_has_expired_is_refused_without_a_call()
    {
        var handler = new FakeHandler();
        var members = new LinkedInMemberTokenStore(_data);
        members.Write(new LinkedInMember(Person, "Jimmy Borch", new LinkedInTokens("member-access", Now.AddSeconds(30), null, null, Now.AddDays(-60))));
        var client = new LinkedInUpdateClient(new HttpClient(handler), MemberApp, members, NullLogger<LinkedInUpdateClient>.Instance, new FixedClock(Now));

        var result = await client.PostUpdateAsync(Request(1), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.False(result.Retryable);
        Assert.Contains("connect again", result.Reason, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }
}
