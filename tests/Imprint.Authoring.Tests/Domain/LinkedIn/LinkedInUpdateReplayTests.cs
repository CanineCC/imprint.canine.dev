using System.Reflection;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

/// <summary>
/// Updates stored before page senders and first comments existed (the owner's own posts, scheduled from 28 October
/// 2026 on) must read and fold exactly as they did: the store is append-only and is never rewritten. The JSON below
/// is the shape the store holds for them, payload for payload.
/// </summary>
public sealed class LinkedInUpdateReplayTests
{
    private static readonly EventRegistry Registry = new([typeof(AuthoringJson).Assembly], AuthoringJson.Configure);
    private static readonly EventMetadata Meta = new("jimmy.borch@example.com", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    private static readonly LinkedInUpdateId Id = LinkedInUpdateId.From(Guid.Parse("d6e39535-5737-4d94-8b9e-e3a5e8b4e0d1"));
    private static readonly SiteId Site = SiteId.From(Guid.Parse("30d6c379-2712-4e26-9204-6abdc716f5eb"));
    private static readonly AssetId Image = AssetId.From(Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"));
    private static readonly DateTimeOffset PublishAt = new(2026, 11, 12, 7, 45, 0, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset At = new(2026, 11, 4, 9, 30, 0, TimeSpan.Zero);

    // The store writes letters outside ASCII as JSON escapes (æ for æ, å for å), as these do.
    private const string ScheduledJson =
        "{\"Id\":\"d6e39535-5737-4d94-8b9e-e3a5e8b4e0d1\",\"SiteId\":\"30d6c379-2712-4e26-9204-6abdc716f5eb\",\"Sender\":\"jimmy\"," +
        "\"Text\":\"En kontrol, som folk l\\u00E6rer at sl\\u00E5 fra, beskytter ingen.\"," +
        "\"Images\":[{\"AssetId\":\"0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d\",\"Alt\":\"Et diagram\"}]," +
        "\"PublishAt\":\"2026-11-12T07:45:00+01:00\",\"At\":\"2026-11-04T09:30:00+00:00\"}";

    private const string EditedJson =
        "{\"Sender\":\"jimmy\",\"Text\":\"En kontrol, som folk l\\u00E6rer at sl\\u00E5 fra, beskytter ingen.\"," +
        "\"Images\":[{\"AssetId\":\"0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d\",\"Alt\":\"Et diagram\"}]," +
        "\"PublishAt\":\"2026-11-12T07:45:00+01:00\",\"At\":\"2026-11-05T10:00:00+00:00\"}";

    private const string AttemptingJson = """{"TextSha256":"abc123","At":"2026-11-12T06:45:05+00:00"}""";

    private const string PostedJson = """{"Urn":"urn:li:share:7525810947253485568","At":"2026-11-12T06:45:07+00:00"}""";

    private static object Read<T>(string json) =>
        Registry.Deserialize(typeof(T).GetCustomAttribute<EventTypeAttribute>()!.StableId, json);

    private static IReadOnlyList<object> Stored() =>
    [
        Read<LinkedInUpdateScheduled>(ScheduledJson),
        Read<LinkedInUpdateEdited>(EditedJson),
        Read<LinkedInUpdateAttempting>(AttemptingJson),
        Read<LinkedInUpdatePosted>(PostedJson),
    ];

    [Fact]
    public void A_scheduled_update_stored_before_first_comments_reads_as_it_was_written()
    {
        var scheduled = Assert.IsType<LinkedInUpdateScheduled>(Read<LinkedInUpdateScheduled>(ScheduledJson));

        Assert.Equal(new LinkedInUpdateScheduled(
            Id, Site, "jimmy", "En kontrol, som folk lærer at slå fra, beskytter ingen.", [new LinkedInUpdateImage(Image, "Et diagram")],
            PublishAt, At), scheduled);
        Assert.Null(scheduled.FirstComment);
    }

    [Fact]
    public void An_edit_stored_before_first_comments_reads_as_it_was_written()
    {
        var edited = Assert.IsType<LinkedInUpdateEdited>(Read<LinkedInUpdateEdited>(EditedJson));

        Assert.Equal(new LinkedInUpdateEdited(
            "jimmy", "En kontrol, som folk lærer at slå fra, beskytter ingen.", [new LinkedInUpdateImage(Image, "Et diagram")],
            PublishAt, new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero)), edited);
        Assert.Null(edited.FirstComment);
    }

    [Fact]
    public void A_stored_update_folds_to_the_same_state_it_had()
    {
        var update = new LinkedInUpdate();
        update.LoadFrom(Stored());

        Assert.Equal(LinkedInUpdateState.Posted, update.State);
        Assert.Equal("urn:li:share:7525810947253485568", update.Urn);
        Assert.Equal(("jimmy", "En kontrol, som folk lærer at slå fra, beskytter ingen.", PublishAt), (update.Sender, update.Text, update.PublishAt));
        Assert.Equal([new LinkedInUpdateImage(Image, "Et diagram")], update.Images);
        Assert.False(update.MaySendAt(PublishAt.AddDays(1)));

        // No first comment: nothing more is ever sent for it.
        Assert.Null(update.FirstComment);
        Assert.Null(update.CommentState);
        Assert.False(update.MayCommentAt(PublishAt.AddMinutes(1)));
    }

    [Fact]
    public void A_stored_update_projects_to_the_same_row_it_had()
    {
        var list = new LinkedInUpdateList();
        long position = 0, version = 0;
        foreach (var @event in Stored())
        {
            list.Apply(new StoredEvent(++position, Id.Stream, ++version, Registry.StableIdOf(@event), @event, Meta));
        }

        var row = list.Get(Id)!;
        Assert.Equal(
            (Site, "jimmy", LinkedInUpdateState.Posted, "urn:li:share:7525810947253485568", PublishAt, 1, (string?)null),
            (row.SiteId, row.Sender, row.State, row.Urn, row.PublishAt, row.Images.Count, row.Reason));
        Assert.Equal("En kontrol, som folk lærer at slå fra, beskytter ingen.", row.Text);
        Assert.False(row.MaySendAt(PublishAt.AddDays(1)));
        Assert.Null(row.FirstComment);
        Assert.Null(row.CommentState);
        Assert.False(row.MayCommentAt(PublishAt.AddMinutes(1)));
    }
}
