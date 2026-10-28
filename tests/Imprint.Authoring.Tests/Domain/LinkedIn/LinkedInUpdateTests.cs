using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

/// <summary>A LinkedIn update is a post of its own, sent at a chosen time. It can change until it is sent, it goes
/// out at most once, and one whose time passed while nothing was running is held back instead of sent late.</summary>
public sealed class LinkedInUpdateTests
{
    private static readonly LinkedInUpdateId Id = LinkedInUpdateId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly AssetId Image = AssetId.New();
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Nine = new(2026, 11, 3, 7, 15, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<LinkedInUpdateImage> OneImage = [new(Image, "A chart")];

    private static LinkedInUpdateScheduled Scheduled(DateTimeOffset? publishAt = null) =>
        new(Id, Site, "jimmy", "Review cannot keep up.", OneImage, publishAt ?? Nine, Now);

    private static LinkedInUpdateAttempting Attempting => new("sha", Nine);

    [Fact]
    public void Scheduling_records_the_sender_text_images_and_time()
    {
        var update = LinkedInUpdate.Schedule(Id, Site, " Jimmy ", " Review cannot keep up. ", OneImage, Nine, Now);

        Assert.Equal(Scheduled(), Assert.Single(update.UncommittedEvents));
        Assert.Equal(LinkedInUpdateState.Scheduled, update.State);
        Assert.Equal($"linkedin-update-{Id.Value:N}", update.StreamId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_update_without_text_is_refused(string text) =>
        Assert.Contains("text", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", text, [], Nine, Now)).Message);

    [Fact]
    public void Text_longer_than_linkedin_allows_is_refused() =>
        Assert.Contains("3,000", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", new string('a', 3001), [], Nine, Now)).Message);

    [Fact]
    public void More_than_nine_images_are_refused() =>
        Assert.Contains("9", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", "Hi", [.. Enumerable.Range(0, 10).Select(_ => new LinkedInUpdateImage(AssetId.New(), "x"))], Nine, Now)).Message);

    [Fact]
    public void An_image_without_alt_text_is_refused() =>
        Assert.Contains("alt text", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", "Hi", [new LinkedInUpdateImage(Image, " ")], Nine, Now)).Message);

    [Fact]
    public void The_same_image_twice_is_refused() =>
        Assert.Contains("twice", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", "Hi", [new(Image, "a"), new(Image, "b")], Nine, Now)).Message);

    [Fact]
    public void A_time_in_the_past_is_refused() =>
        Assert.Contains("past", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "jimmy", "Hi", [], Now.AddMinutes(-1), Now)).Message);

    [Fact]
    public void A_scheduled_update_can_be_edited() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.Edit("jimmy", "New text", [], Nine.AddDays(1), Now))
            .ThenRaised(new LinkedInUpdateEdited("jimmy", "New text", [], Nine.AddDays(1), Now));

    [Fact]
    public void An_update_that_is_being_sent_cannot_be_edited() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting)
            .When(u => u.Edit("jimmy", "New text", [], Nine.AddDays(1), Now))
            .ThenFails("cannot be changed");

    [Fact]
    public void A_posted_update_cannot_be_edited_or_cancelled()
    {
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, new LinkedInUpdatePosted("urn:li:share:1", Nine))
            .When(u => u.Edit("jimmy", "x", [], Nine.AddDays(1), Now))
            .ThenFails("cannot be changed");
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, new LinkedInUpdatePosted("urn:li:share:1", Nine))
            .When(u => u.Cancel(Now))
            .ThenFails("cannot be cancelled");
    }

    [Fact]
    public void A_scheduled_update_can_be_cancelled_and_is_then_never_sent()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled(), new LinkedInUpdateCancelled(Now)]);

        Assert.Equal(LinkedInUpdateState.Cancelled, update.State);
        Assert.False(update.MaySendAt(Nine.AddHours(1)));
    }

    [Fact]
    public void It_may_be_sent_once_its_time_has_come_and_not_before()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled()]);

        Assert.False(update.MaySendAt(Nine.AddMinutes(-1)));
        Assert.True(update.MaySendAt(Nine));
    }

    [Fact]
    public void Sending_before_its_time_is_refused() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.Attempt("sha", Nine.AddMinutes(-1)))
            .ThenFails("not due");

    [Fact]
    public void A_posted_update_is_never_sent_again() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, new LinkedInUpdatePosted("urn:li:share:1", Nine))
            .When(u => u.Attempt("sha", Nine.AddDays(1)))
            .ThenFails("already gone out");

    [Fact]
    public void An_unknown_result_is_never_retried_on_its_own() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, new LinkedInUpdateUnknown("timed out", Nine))
            .When(u => u.Attempt("sha", Nine.AddDays(1)))
            .ThenFails("unknown");

    [Fact]
    public void A_retryable_refusal_may_be_sent_again_when_its_time_has_come()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled(), Attempting, new LinkedInUpdateFailed("rate limited", true, Nine, Nine.AddMinutes(15))]);

        Assert.False(update.MaySendAt(Nine.AddMinutes(14)));
        Assert.True(update.MaySendAt(Nine.AddMinutes(15)));
    }

    [Fact]
    public void A_final_refusal_is_not_sent_again_but_may_be_edited_and_rescheduled() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, new LinkedInUpdateFailed("no permission", false, Nine))
            .When(u => u.Edit("jimmy", "Fixed", [], Nine.AddDays(1), Nine))
            .ThenRaised(new LinkedInUpdateEdited("jimmy", "Fixed", [], Nine.AddDays(1), Nine));

    [Fact]
    public void An_update_whose_time_passed_long_ago_is_held_back_as_missed() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.Miss("Its time passed while nothing was running.", Nine.AddHours(3)))
            .ThenRaised(new LinkedInUpdateMissed("Its time passed while nothing was running.", Nine.AddHours(3)));

    [Fact]
    public void A_missed_update_is_not_sent_until_it_is_given_a_new_time()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled(), new LinkedInUpdateMissed("late", Nine.AddHours(3))]);
        Assert.False(update.MaySendAt(Nine.AddHours(4)));

        update.Edit("jimmy", "Review cannot keep up.", OneImage, Nine.AddDays(2), Nine.AddHours(4));
        Assert.Equal(LinkedInUpdateState.Scheduled, update.State);
    }

    [Fact]
    public void A_person_resolves_an_unknown_result_as_posted_with_its_urn()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled(), Attempting, new LinkedInUpdateUnknown("timed out", Nine)]);
        update.Resolve("urn:li:share:7", Nine.AddHours(1));

        Assert.Equal((LinkedInUpdateState.Posted, "urn:li:share:7"), (update.State, update.Urn));
    }

    [Fact]
    public void A_person_resolves_an_unknown_result_as_not_posted_and_it_waits_for_a_new_time()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom([Scheduled(), Attempting, new LinkedInUpdateUnknown("timed out", Nine)]);
        update.Resolve(null, Nine.AddHours(1));

        Assert.Equal(LinkedInUpdateState.Missed, update.State);
        Assert.False(update.MaySendAt(Nine.AddHours(2)));
    }

    [Fact]
    public void Only_an_unknown_result_can_be_resolved() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.Resolve(null, Nine))
            .ThenFails("not unknown");
}
