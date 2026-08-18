using System.Security.Claims;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.Authoring.Projections;
using Imprint.Editor.Auth;
using Imprint.EventSourcing;
using Microsoft.AspNetCore.Components.Authorization;

namespace Imprint.Editor.Tests;

/// <summary>
/// The reviewer's pass. A site's reviewer is mailed a link to ONE post and is deliberately not a
/// collaborator, so a site-wide access check answered "no" for the only person the mail was
/// addressed to. These pin the scope of the pass that replaced it: it opens posts that have been
/// handed to the reviewer, it never opens the site's other drafts, and it never widens to editing.
/// </summary>
public sealed class ReviewerPostPassTests
{
    private const string Owner = "author@canine.dev";
    private const string Reviewer = "lasse@canine.dev";
    private const string Stranger = "nobody@example.com";

    private static readonly SiteId Site = SiteId.New();

    private sealed class FakeAuthState(string? email) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = email is null
                ? new ClaimsIdentity()
                : new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    /// <summary>A projection holding one site: created by <see cref="Owner"/>, reviewed by whoever.</summary>
    private static SiteOverview Sites(string? reviewer = Reviewer, string? collaborator = null)
    {
        var overview = new SiteOverview();
        var position = 0L;
        void Apply(object @event) => overview.Apply(new StoredEvent(
            ++position, $"site-{Site.Value:N}", position, "site.created.v1", @event,
            new EventMetadata(Owner, DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty)));

        Apply(new SiteCreated(Site, "Canine Blog", new Locale("en"), SiteKind.Blog));
        if (reviewer is not null)
        {
            Apply(new SiteReviewerSet("Lasse", reviewer));
        }

        if (collaborator is not null)
        {
            Apply(new SiteCollaboratorAdded(collaborator));
        }

        return overview;
    }

    private static SiteAccess AccessFor(string? signedInAs, SiteOverview sites, bool authEnabled = true) =>
        new(new KeycloakOptions { Authority = authEnabled ? "https://keycloak.example/realms/imprint" : null },
            new EditorActor(new FakeAuthState(signedInAs)),
            sites);

    [Fact]
    public async Task With_auth_off_everyone_still_edits()
    {
        var access = AccessFor(null, Sites(), authEnabled: false);
        Assert.Equal(PostPass.Edit, await access.PassForAsync(Site, PostReview.None));
    }

    [Fact]
    public async Task The_owner_edits()
    {
        var access = AccessFor(Owner, Sites());
        Assert.Equal(PostPass.Edit, await access.PassForAsync(Site, PostReview.Pending));
    }

    [Fact]
    public async Task A_collaborator_edits()
    {
        var access = AccessFor(Stranger, Sites(collaborator: Stranger));
        Assert.Equal(PostPass.Edit, await access.PassForAsync(Site, PostReview.Pending));
    }

    // The bug: this was PostPass.None, so the review link redirected Lasse to the dashboard.
    [Theory]
    [InlineData(PostReview.Pending)]
    [InlineData(PostReview.Approved)]
    [InlineData(PostReview.ChangesRequested)]
    public async Task The_reviewer_may_open_a_post_that_has_been_handed_to_them(PostReview review)
    {
        var access = AccessFor(Reviewer, Sites());
        Assert.Equal(PostPass.Review, await access.PassForAsync(Site, review));
    }

    [Fact]
    public async Task The_reviewer_may_not_open_a_draft_that_was_never_submitted()
    {
        var access = AccessFor(Reviewer, Sites());
        Assert.Equal(PostPass.None, await access.PassForAsync(Site, PostReview.None));
    }

    // Approval lapses to None when the author edits, which hands the post back to them.
    [Fact]
    public async Task A_lapsed_approval_closes_the_reviewers_pass_again()
    {
        var access = AccessFor(Reviewer, Sites());
        Assert.Equal(PostPass.Review, await access.PassForAsync(Site, PostReview.Approved));
        Assert.Equal(PostPass.None, await access.PassForAsync(Site, PostReview.None));
    }

    [Fact]
    public async Task The_reviewers_pass_never_widens_to_editing()
    {
        var access = AccessFor(Reviewer, Sites());
        Assert.NotEqual(PostPass.Edit, await access.PassForAsync(Site, PostReview.Pending));
    }

    [Fact]
    public async Task A_reviewer_who_is_also_a_collaborator_keeps_the_wider_pass()
    {
        var access = AccessFor(Reviewer, Sites(collaborator: Reviewer));
        Assert.Equal(PostPass.Edit, await access.PassForAsync(Site, PostReview.Pending));
    }

    [Fact]
    public async Task The_reviewers_address_is_matched_without_regard_to_case()
    {
        var access = AccessFor("LASSE@Canine.DEV", Sites());
        Assert.Equal(PostPass.Review, await access.PassForAsync(Site, PostReview.Pending));
    }

    [Fact]
    public async Task A_stranger_gets_nothing_even_while_the_post_is_in_review()
    {
        var access = AccessFor(Stranger, Sites());
        Assert.Equal(PostPass.None, await access.PassForAsync(Site, PostReview.Pending));
    }

    [Fact]
    public async Task A_site_with_no_reviewer_hands_out_no_pass()
    {
        var access = AccessFor(Stranger, Sites(reviewer: null));
        Assert.Equal(PostPass.None, await access.PassForAsync(Site, PostReview.Pending));
    }
}
