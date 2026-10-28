using System.Text.Json;

namespace Imprint.Editor.LinkedIn;

/// <summary>A member who connected LinkedIn to post under their own name: their person URN, the name LinkedIn
/// gave, and their tokens.</summary>
public sealed record LinkedInMember(string PersonUrn, string Name, LinkedInTokens Tokens);

/// <summary>
/// Each connected member in its own file under <c>secrets/linkedin-members/</c>, readable by the editor's user only.
/// Files, not events, for the reason <see cref="LinkedInTokenStore"/> gives: the event stream is copied into backups.
/// </summary>
public sealed class LinkedInMemberTokenStore(string dataDirectory)
{
    private readonly string _directory = Path.Combine(dataDirectory, "secrets", "linkedin-members");
    private readonly Lock _gate = new();

    public LinkedInMember? Read(string personUrn)
    {
        lock (_gate)
        {
            return ReadFile(PathOf(personUrn));
        }
    }

    public IReadOnlyList<LinkedInMember> All()
    {
        lock (_gate)
        {
            return Directory.Exists(_directory)
                ? [.. Directory.GetFiles(_directory, "*.json").Select(ReadFile).OfType<LinkedInMember>().OrderBy(m => m.Name)]
                : [];
        }
    }

    public void Write(LinkedInMember member)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            var path = PathOf(member.PersonUrn);
            var temporary = path + ".tmp";
            // Created with owner-only permissions before any secret is written into it.
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var stream = new FileStream(temporary, options))
            {
                JsonSerializer.Serialize(stream, member);
            }

            File.Move(temporary, path, overwrite: true);
        }
    }

    public void Delete(string personUrn)
    {
        lock (_gate)
        {
            File.Delete(PathOf(personUrn));
        }
    }

    private static LinkedInMember? ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LinkedInMember>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // The person id is LinkedIn's (letters, digits, '-' and '_'); anything else never names a file.
    private string PathOf(string personUrn)
    {
        var id = personUrn.StartsWith("urn:li:person:", StringComparison.Ordinal) ? personUrn["urn:li:person:".Length..] : "";
        if (id.Length == 0 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new ArgumentException($"'{personUrn}' is not a LinkedIn member.", nameof(personUrn));
        }

        return Path.Combine(_directory, id + ".json");
    }
}
