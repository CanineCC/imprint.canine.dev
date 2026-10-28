using System.Net;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Editor.LinkedIn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// An article's LinkedIn post may go out as a person (a member sender, urn:li:person:) as well as a page: the member's
/// own connection carries it, so it needs no page app. A page sender still needs the page app and its connection.
/// </summary>
public sealed class LinkedInArticleAsMemberTests : IDisposable
{
    private const string Person = "urn:li:person:Ab3";
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly LinkedInOptions NoPageApp = new();
    private static readonly LinkedInOptions MemberApp = LinkedInOptions.ForMembers(new()
    {
        ClientId = "member-client", ClientSecret = "secret", Version = "202610", PublicOrigin = "https://imprint.example.com",
        ApiBase = "https://api.test", OAuthBase = "https://oauth.test",
    });

    private readonly string _data = Path.Combine(Path.GetTempPath(), $"imprint-linkedin-article-{Guid.NewGuid():N}");

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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Seen.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            var created = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") };
            created.Headers.Add("x-restli-id", "urn:li:share:9");
            return created;
        }
    }

    private (LinkedInClient Client, FakeHandler Http) Client(bool memberConnected)
    {
        var handler = new FakeHandler();
        var members = new LinkedInMemberTokenStore(_data);
        if (memberConnected)
        {
            members.Write(new LinkedInMember(Person, "Jimmy Borch",
                new LinkedInTokens("member-access", Now.AddDays(30), null, null, Now.AddDays(-1))));
        }

        return (new LinkedInClient(new HttpClient(handler), NoPageApp, new LinkedInTokenStore(_data),
            NullLogger<LinkedInClient>.Instance, new FixedClock(Now), MemberApp, members), handler);
    }

    [Fact]
    public async Task An_article_as_a_connected_member_goes_out_with_the_members_token_and_no_page_app()
    {
        var (client, http) = Client(memberConnected: true);

        Assert.True(client.IsConfiguredFor(Person));
        Assert.False(client.IsConfiguredFor("urn:li:organization:42"));
        var result = await client.PostAsync(
            new LinkedInPostRequest(Person, "Read it.", "https://rc.example.com/first/", "First", ""), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Posted, result.Status);
        Assert.Equal("urn:li:share:9", result.Urn);
        var (request, body) = Assert.Single(http.Seen);
        Assert.Equal("Bearer member-access", request.Headers.Authorization!.ToString());
        Assert.Contains("\"author\":\"urn:li:person:Ab3\"", body, StringComparison.Ordinal);
        Assert.Contains("\"source\":\"https://rc.example.com/first/\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_member_who_has_not_connected_is_not_configured_and_nothing_is_sent()
    {
        var (client, http) = Client(memberConnected: false);

        Assert.False(client.IsConfiguredFor(Person));
        var result = await client.PostAsync(
            new LinkedInPostRequest(Person, "Read it.", "https://rc.example.com/first/", "First", ""), CancellationToken.None);

        Assert.Equal(LinkedInSendStatus.Refused, result.Status);
        Assert.Empty(http.Seen);
    }
}

/// <summary>Connecting your LinkedIn from a site's settings makes you one of its senders, under your first name.</summary>
public sealed class LinkedInConnectAddsSenderTests
{
    private static readonly Imprint.Authoring.Domain.Sites.LinkedInSender Page = new("canine", "Canine Development", "urn:li:organization:1");

    [Fact]
    public void A_newly_connected_member_is_added_under_their_first_name()
    {
        var senders = LinkedInMemberOAuth.WithMember([Page], "urn:li:person:L1", "Lasse Overgaard");

        Assert.Equal([Page, new("lasse", "Lasse Overgaard", "urn:li:person:L1")], senders);
    }

    [Fact]
    public void A_member_already_a_sender_changes_nothing()
    {
        var jimmy = new Imprint.Authoring.Domain.Sites.LinkedInSender("jimmy", "Jimmy Borch", "urn:li:person:J1");

        Assert.Null(LinkedInMemberOAuth.WithMember([Page, jimmy], "urn:li:person:J1", "Jimmy Borch"));
    }

    [Fact]
    public void A_taken_key_or_a_name_with_no_plain_letters_still_gets_a_usable_key()
    {
        var jimmy = new Imprint.Authoring.Domain.Sites.LinkedInSender("jimmy", "Jimmy Borch", "urn:li:person:J1");

        Assert.Equal("jimmy-2", LinkedInMemberOAuth.WithMember([jimmy], "urn:li:person:J2", "Jimmy Hansen")![1].Key);
        Assert.Equal("soeren", LinkedInMemberOAuth.WithMember([], "urn:li:person:S1", "Søren Ålund")![0].Key);
        Assert.Equal("rene", LinkedInMemberOAuth.WithMember([], "urn:li:person:R1", "René Hansen")![0].Key);
        Assert.Equal("member", LinkedInMemberOAuth.WithMember([], "urn:li:person:X1", "李")![0].Key);
    }
}
