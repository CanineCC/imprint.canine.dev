using System.Net;
using System.Text.Json.Nodes;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;
using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// A member's update against a fake HTTP handler: each image goes up as the member first, then one post carries
/// them with their alt text, hashtags stay hashtags, and nothing is posted when an image could not go up.
/// </summary>
public sealed class LinkedInUpdateClientTests : IDisposable
{
    private const string Person = "urn:li:person:Ab3";
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly LinkedInOptions App = LinkedInOptions.ForMembers(new()
    {
        ClientId = "client", ClientSecret = "secret", Version = "202610", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://api.test", OAuthBase = "https://oauth.test",
    });

    private readonly string _data = Path.Combine(Path.GetTempPath(), $"imprint-linkedin-member-{Guid.NewGuid():N}");

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
            Seen.Add((request, request.Content is null ? "" : request.Content is ByteArrayContent and not StringContent
                ? $"{(await request.Content.ReadAsByteArrayAsync(ct)).Length} bytes"
                : await request.Content.ReadAsStringAsync(ct)));
            return Answers.Dequeue()();
        }
    }

    private (LinkedInUpdateClient Client, FakeHandler Http) Client(bool connected = true, DateTimeOffset? expires = null)
    {
        var handler = new FakeHandler();
        var store = new LinkedInMemberTokenStore(_data);
        if (connected)
        {
            store.Write(new LinkedInMember(Person, "Jimmy Borch",
                new LinkedInTokens("access", expires ?? Now.AddDays(30), null, null, Now.AddDays(-1))));
        }

        return (new LinkedInUpdateClient(new HttpClient(handler), App, store, NullLogger<LinkedInUpdateClient>.Instance, new FixedClock(Now)), handler);
    }

    private static Func<HttpResponseMessage> Init(int n) => () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"value\":{\"uploadUrl\":\"https://upload.test/" + n + "\",\"image\":\"urn:li:image:" + n + "\"}}"),
    };

    private static Func<HttpResponseMessage> Status(HttpStatusCode code) => () => new HttpResponseMessage(code) { Content = new StringContent("{}") };

    private static Func<HttpResponseMessage> Created(string urn) => () =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("") };
        response.Headers.Add("x-restli-id", urn);
        return response;
    };

    private static LinkedInUpdateRequest Request(int images) => new(
        Person, "Review (still) matters. #AI #ledelse",
        [.. Enumerable.Range(1, images).Select(i => new LinkedInUpdateImageFile([1, 2, 3], "image/png", $"Chart {i}"))]);

    [Fact]
    public async Task Two_images_go_up_as_the_member_and_one_post_carries_both_with_their_alt_text()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Status(HttpStatusCode.Created));
        http.Answers.Enqueue(Init(2));
        http.Answers.Enqueue(Status(HttpStatusCode.Created));
        http.Answers.Enqueue(Created("urn:li:share:77"));

        var result = await client.PostUpdateAsync(Request(2), CancellationToken.None);

        Assert.Equal((LinkedInSendStatus.Posted, "urn:li:share:77"), (result.Status, result.Urn));
        Assert.Equal(5, http.Seen.Count);
        var init = JsonNode.Parse(http.Seen[0].Body)!;
        Assert.Equal(Person, init["initializeUploadRequest"]!["owner"]!.GetValue<string>());
        Assert.Equal((HttpMethod.Put, "https://upload.test/1"), (http.Seen[1].Request.Method, http.Seen[1].Request.RequestUri!.ToString()));
        var post = JsonNode.Parse(http.Seen[4].Body)!;
        Assert.Equal(Person, post["author"]!.GetValue<string>());
        var images = post["content"]!["multiImage"]!["images"]!.AsArray();
        Assert.Equal(["urn:li:image:1", "urn:li:image:2"], images.Select(i => i!["id"]!.GetValue<string>()));
        Assert.Equal(["Chart 1", "Chart 2"], images.Select(i => i!["altText"]!.GetValue<string>()));
        Assert.Equal("202610", http.Seen[4].Request.Headers.GetValues("Linkedin-Version").Single());
    }

    [Fact]
    public async Task One_image_is_sent_as_media_and_hashtags_stay_hashtags()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Init(1));
        http.Answers.Enqueue(Status(HttpStatusCode.Created));
        http.Answers.Enqueue(Created("urn:li:share:1"));

        await client.PostUpdateAsync(Request(1), CancellationToken.None);

        var post = JsonNode.Parse(http.Seen[2].Body)!;
        Assert.Equal("urn:li:image:1", post["content"]!["media"]!["id"]!.GetValue<string>());
        Assert.Equal("Chart 1", post["content"]!["media"]!["altText"]!.GetValue<string>());
        Assert.Equal(@"Review \(still\) matters. {hashtag|\#|AI} {hashtag|\#|ledelse}", post["commentary"]!.GetValue<string>());
    }

    [Fact]
    public async Task Without_images_the_post_has_no_content_block()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Created("urn:li:share:1"));

        await client.PostUpdateAsync(Request(0), CancellationToken.None);

        Assert.Null(JsonNode.Parse(Assert.Single(http.Seen).Body)!["content"]);
    }

    [Fact]
    public async Task An_image_that_cannot_go_up_means_nothing_is_posted_and_it_says_so()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Status(HttpStatusCode.Forbidden));

        var result = await client.PostUpdateAsync(Request(1), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.Contains("image", result.Reason);
        Assert.Single(http.Seen);
    }

    [Fact]
    public async Task A_rate_limit_on_the_post_is_a_retryable_refusal_and_a_server_error_is_unknown()
    {
        var (client, http) = Client();
        http.Answers.Enqueue(Status(HttpStatusCode.TooManyRequests));
        Assert.True((await client.PostUpdateAsync(Request(0), CancellationToken.None)).Retryable);

        http.Answers.Enqueue(Status(HttpStatusCode.InternalServerError));
        Assert.Equal(LinkedInSendStatus.Unknown, (await client.PostUpdateAsync(Request(0), CancellationToken.None)).Status);
    }

    [Fact]
    public void A_member_is_configured_only_while_connected_and_unexpired()
    {
        // Not connected first: the instances below share this test's data directory.
        Assert.False(Client(connected: false).Client.IsConfiguredFor(Person));
        Assert.True(Client().Client.IsConfiguredFor(Person));
        Assert.False(Client().Client.IsConfiguredFor("urn:li:person:someone-else"));
        Assert.False(Client(expires: Now.AddSeconds(30)).Client.IsConfiguredFor(Person));
        Assert.False(Client().Client.IsConfiguredFor("urn:li:organization:42"));
    }

    [Fact]
    public void The_member_token_file_is_readable_by_its_owner_only()
    {
        Client();
        var file = Assert.Single(Directory.GetFiles(Path.Combine(_data, "secrets", "linkedin-members")));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(file));
        }

        Assert.Equal("Jimmy Borch", new LinkedInMemberTokenStore(_data).Read(Person)!.Name);
    }

    [Fact]
    public async Task The_member_is_known_by_the_id_and_name_openid_connect_gives()
    {
        var handler = new FakeHandler();
        handler.Answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"sub\":\"Ab3\",\"name\":\"Jimmy Borch\"}") });
        handler.Answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"sub\":\"../x\"}") });

        Assert.Equal(("urn:li:person:Ab3", "Jimmy Borch"), await LinkedInMemberOAuth.Who(new HttpClient(handler), App, "t", CancellationToken.None));
        Assert.Null(await LinkedInMemberOAuth.Who(new HttpClient(handler), App, "t", CancellationToken.None));
        Assert.Equal("https://api.test/v2/userinfo", handler.Seen[0].Request.RequestUri!.ToString());
    }

    [Fact]
    public void The_member_app_asks_for_openid_and_member_sharing_and_returns_to_its_own_callback()
    {
        var url = LinkedInOAuth.AuthorizeUrl(App, "s");
        Assert.Contains("scope=openid%20profile%20w_member_social", url);
        Assert.Contains(Uri.EscapeDataString("https://imprint.example.com/linkedin/member-callback"), url);
    }
}
