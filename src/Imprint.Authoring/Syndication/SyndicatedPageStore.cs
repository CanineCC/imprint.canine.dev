using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Microsoft.Data.Sqlite;

namespace Imprint.Authoring.Syndication;

/// <summary>One page authored elsewhere and served as part of this site.</summary>
/// <param name="Path">Where it is served, relative to the site root — may be nested.</param>
/// <param name="Node">The page's content as this system's own node tree, so it renders like any other page.</param>
public sealed record SyndicatedPage(
    SiteId SiteId,
    string Path,
    LocalizedText Title,
    LocalizedText MetaTitle,
    LocalizedText MetaDescription,
    Node Node,
    string ContentHash,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Pages this site serves but does not author: they are produced by another system, pushed here, and
/// rendered by the same views, chrome and stylesheet as everything else.
/// </summary>
/// <remarks>
/// Deliberately NOT event-sourced, unlike every authored page. The producing system is the source of
/// truth; this is a mirror. Giving a few thousand generated survey pages their own event streams
/// would put them in the editor's page list, the drafts projection and the undo history — as if
/// someone had typed them — and none of that is true or useful. Losing this table costs nothing but
/// a re-push.
/// <para>
/// It stores a NODE TREE rather than markup, and that is the load-bearing decision. If the producer
/// sent finished HTML it would have to know this site's chrome, stylesheet, theme tokens and heading
/// anchors — a second renderer, drifting from the first, which is the exact failure the publish
/// manifest's renderer version exists to prevent. Sending content and letting this side supply form
/// means a syndicated page gets every rendering fix for free, forever.
/// </para>
/// </remarks>
public sealed class SyndicatedPageStore
{
    /// <summary>
    /// Raised after a push or a withdrawal actually changed something, so the publisher can pick it up.
    /// </summary>
    /// <remarks>
    /// Authored pages wake the publisher through the projection engine's catch-up. These pages are not event-sourced
    /// (see the type remarks), so they raise nothing — and without this a pushed page would sit in the table until
    /// some UNRELATED authoring event happened to trigger a pass. On a site nobody is editing, that is indefinitely:
    /// the producer would be told its push succeeded, and the page would never appear.
    /// </remarks>
    public event Action? Changed;

    private readonly string _connectionString;

    public SyndicatedPageStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SyndicatedPages (
                SiteId          TEXT NOT NULL,
                Path            TEXT NOT NULL,
                TitleJson       TEXT NOT NULL,
                MetaTitleJson   TEXT NOT NULL,
                MetaDescJson    TEXT NOT NULL,
                NodeJson        TEXT NOT NULL,
                ContentHash     TEXT NOT NULL,
                UpdatedAt       TEXT NOT NULL,
                PRIMARY KEY (SiteId, Path)
            );
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Insert or replace one page. Returns true when the content actually changed.</summary>
    /// <remarks>
    /// The producer re-pushes everything it owns on every run, so most calls carry content identical
    /// to what is already stored. Comparing the hash before writing keeps <c>UpdatedAt</c> meaning
    /// "when this page last CHANGED" rather than "when it was last mentioned" — which is what makes
    /// it usable as a staleness signal instead of a heartbeat.
    /// </remarks>
    public bool Upsert(SyndicatedPage page)
    {
        using var connection = Open();
        using var existing = connection.CreateCommand();
        existing.CommandText = "SELECT ContentHash FROM SyndicatedPages WHERE SiteId = $site AND Path = $path;";
        existing.Parameters.AddWithValue("$site", page.SiteId.Compact);
        existing.Parameters.AddWithValue("$path", page.Path);
        if (existing.ExecuteScalar() as string == page.ContentHash)
        {
            return false;
        }

        using var write = connection.CreateCommand();
        write.CommandText = """
            INSERT INTO SyndicatedPages (SiteId, Path, TitleJson, MetaTitleJson, MetaDescJson, NodeJson, ContentHash, UpdatedAt)
            VALUES ($site, $path, $title, $metaTitle, $metaDesc, $node, $hash, $updated)
            ON CONFLICT (SiteId, Path) DO UPDATE SET
                TitleJson = excluded.TitleJson, MetaTitleJson = excluded.MetaTitleJson,
                MetaDescJson = excluded.MetaDescJson, NodeJson = excluded.NodeJson,
                ContentHash = excluded.ContentHash, UpdatedAt = excluded.UpdatedAt;
            """;
        write.Parameters.AddWithValue("$site", page.SiteId.Compact);
        write.Parameters.AddWithValue("$path", page.Path);
        write.Parameters.AddWithValue("$title", SyndicatedJson.Localized(page.Title));
        write.Parameters.AddWithValue("$metaTitle", SyndicatedJson.Localized(page.MetaTitle));
        write.Parameters.AddWithValue("$metaDesc", SyndicatedJson.Localized(page.MetaDescription));
        write.Parameters.AddWithValue("$node", SyndicatedJson.Node(page.Node));
        write.Parameters.AddWithValue("$hash", page.ContentHash);
        write.Parameters.AddWithValue("$updated", page.UpdatedAt.ToString("O"));
        write.ExecuteNonQuery();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Remove one page. Returns true when something was there to remove.</summary>
    public bool Remove(SiteId siteId, string path)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SyndicatedPages WHERE SiteId = $site AND Path = $path;";
        command.Parameters.AddWithValue("$site", siteId.Compact);
        command.Parameters.AddWithValue("$path", path);
        if (command.ExecuteNonQuery() == 0)
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Every syndicated page of one site, in path order so a publish pass is deterministic.</summary>
    public IReadOnlyList<SyndicatedPage> AllForSite(SiteId siteId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Path, TitleJson, MetaTitleJson, MetaDescJson, NodeJson, ContentHash, UpdatedAt
            FROM SyndicatedPages WHERE SiteId = $site ORDER BY Path;
            """;
        command.Parameters.AddWithValue("$site", siteId.Compact);

        var pages = new List<SyndicatedPage>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(new SyndicatedPage(
                siteId,
                reader.GetString(0),
                SyndicatedJson.ReadLocalized(reader.GetString(1)),
                SyndicatedJson.ReadLocalized(reader.GetString(2)),
                SyndicatedJson.ReadLocalized(reader.GetString(3)),
                SyndicatedJson.ReadNode(reader.GetString(4)),
                reader.GetString(5),
                DateTimeOffset.Parse(reader.GetString(6), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return pages;
    }

    /// <summary>A content hash over everything that can change what the page renders.</summary>
    public static string HashOf(LocalizedText title, LocalizedText metaTitle, LocalizedText metaDescription, Node node)
    {
        var payload = string.Join(
            '',
            SyndicatedJson.Localized(title),
            SyndicatedJson.Localized(metaTitle),
            SyndicatedJson.Localized(metaDescription),
            SyndicatedJson.Node(node));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..16].ToLowerInvariant();
    }
}
