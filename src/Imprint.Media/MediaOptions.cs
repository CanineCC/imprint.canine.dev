namespace Imprint.Media;

public sealed class MediaOptions
{
    /// <summary>Directory that receives all originals and derivatives. Created on demand.</summary>
    public required string RootPath { get; init; }

    // A bare command name resolves via PATH; an absolute path pins a specific build.
    public string FfmpegPath { get; init; } = "ffmpeg";

    // VP9 encoding is slow by design; ten minutes bounds a runaway encode without
    // failing legitimately long clips.
    public TimeSpan VideoTimeout { get; init; } = TimeSpan.FromMinutes(10);

    // 82 is the sweet spot where WebP artifacts stop being visible in photos while
    // still cutting size roughly in half versus quality 100.
    public int WebPQuality { get; init; } = 82;
}
