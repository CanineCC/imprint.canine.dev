namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// The clock a publishing schedule is read in. "Publish at 09:00" means nine in the morning
/// where the people writing are, not nine UTC — and the difference is an hour or two of a post
/// being live before anyone meant it to be.
///
/// <para>Instants are stored absolute (<see cref="DateTimeOffset"/>); this only decides how a
/// wall-clock time typed into the editor becomes one, and how one is written back out for a
/// reader. One zone for the whole install, deliberately: the estate is one newsroom. It becomes
/// a site setting the day a second one exists, and the conversion is centralized here so that
/// change is a change to this file.</para>
/// </summary>
public static class EditorialTime
{
    /// <summary>Europe/Copenhagen, or UTC on a system whose tz database does not carry it.</summary>
    public static TimeZoneInfo Zone { get; } = Resolve();

    /// <summary>A wall-clock time in the editorial zone, as an absolute instant.</summary>
    public static DateTimeOffset FromLocal(DateTime local) =>
        new(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone.GetUtcOffset(local));

    /// <summary>An absolute instant as wall-clock time in the editorial zone.</summary>
    public static DateTime ToLocal(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, Zone).DateTime;

    /// <summary>How a date is written on a published post: "17 August 2026", in the editorial zone.</summary>
    public static string ForReader(DateTimeOffset instant) =>
        ToLocal(instant).ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>How a scheduled time reads back to an author: "17 Aug 2026, 09:00".</summary>
    public static string ForAuthor(DateTimeOffset instant) =>
        ToLocal(instant).ToString("d MMM yyyy, HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // A container without tzdata must still publish. UTC shifts the wall clock by an
            // hour or two rather than failing the site, which is the right way round.
            return TimeZoneInfo.Utc;
        }
    }
}
