using System.Text.Json;

namespace Imprint.Editor.LinkedIn;

/// <summary>The member's tokens, as LinkedIn issued them. Never written to the event stream or a log.</summary>
public sealed record LinkedInTokens(
    string AccessToken, DateTimeOffset AccessExpiresAt, string? RefreshToken, DateTimeOffset? RefreshExpiresAt, DateTimeOffset ConnectedAt);

/// <summary>
/// The tokens in one file under the data directory's <c>secrets</c> folder, readable by the editor's user only
/// (mode 600 on Unix). A secret file rather than an event: the event stream is copied into backups and read by
/// the authoring API, and a refresh token there would outlive every rotation.
/// </summary>
public sealed class LinkedInTokenStore(string dataDirectory)
{
    private readonly string _path = Path.Combine(dataDirectory, "secrets", "linkedin-token.json");
    private readonly Lock _gate = new();

    public LinkedInTokens? Read()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<LinkedInTokens>(File.ReadAllText(_path));
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public void Write(LinkedInTokens tokens)
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            var temporary = _path + ".tmp";
            // Created with owner-only permissions before any secret is written into it.
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var stream = new FileStream(temporary, options))
            {
                JsonSerializer.Serialize(stream, tokens);
            }

            File.Move(temporary, _path, overwrite: true);
        }
    }

    public void Delete()
    {
        lock (_gate)
        {
            File.Delete(_path);
        }
    }
}
