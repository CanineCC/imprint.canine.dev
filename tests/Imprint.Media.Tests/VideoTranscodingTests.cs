using System.Diagnostics;
using System.Text;
using Imprint.Authoring.Domain;

namespace Imprint.Media.Tests;

public sealed class VideoTranscodingTests : IDisposable
{
    private readonly TempMediaRoot _root = new();
    private readonly DiskMediaStore _store;
    private readonly AssetId _id = AssetId.New();

    public VideoTranscodingTests() => _store = new DiskMediaStore(_root.Options);

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task TranscodeToWebM_with_bogus_ffmpeg_path_returns_null_and_explains_why()
    {
        var options = new MediaOptions
        {
            RootPath = _root.Path,
            FfmpegPath = "/definitely/not/a/real/ffmpeg-binary",
        };
        var processor = new SkiaMediaProcessor(_store, options);
        var key = await _store.SaveOriginal(_id, "clip.mp4", new MemoryStream(Encoding.UTF8.GetBytes("fake")));

        var result = await processor.TranscodeToWebM(_id, key);

        Assert.Null(result);
        Assert.NotNull(processor.VideoUnavailableReason);
        Assert.Contains("was not found at '/definitely/not/a/real/ffmpeg-binary'", processor.VideoUnavailableReason);
        Assert.Contains("FfmpegPath", processor.VideoUnavailableReason);
    }

    [Fact]
    public void VideoUnavailableReason_is_null_when_the_probe_succeeds()
    {
        var ffmpeg = Environment.GetEnvironmentVariable("IMPRINT_TEST_FFMPEG");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(ffmpeg),
            "Set IMPRINT_TEST_FFMPEG to an ffmpeg binary to run video transcoding tests.");

        var processor = new SkiaMediaProcessor(_store, new MediaOptions { RootPath = _root.Path, FfmpegPath = ffmpeg });

        Assert.Null(processor.VideoUnavailableReason);
    }

    [Fact]
    public async Task TranscodeToWebM_produces_webm_with_ebml_magic_bytes()
    {
        var ffmpeg = Environment.GetEnvironmentVariable("IMPRINT_TEST_FFMPEG");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(ffmpeg),
            "Set IMPRINT_TEST_FFMPEG to an ffmpeg binary to run video transcoding tests.");

        var originalKey = await _store.SaveOriginal(_id, "clip.mp4", new MemoryStream(GenerateClip(ffmpeg)));
        var processor = new SkiaMediaProcessor(_store, new MediaOptions { RootPath = _root.Path, FfmpegPath = ffmpeg });

        var result = await processor.TranscodeToWebM(_id, originalKey);

        Assert.NotNull(result);
        Assert.Equal($"derived/{_id.Compact}/video.webm", result.Value.StorageKey);
        await using var stream = await _store.Open(result.Value.StorageKey);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        Assert.Equal(result.Value.ByteSize, bytes.LongLength);
        // EBML header: every WebM (and Matroska) file starts 1A 45 DF A3.
        Assert.Equal([0x1A, 0x45, 0xDF, 0xA3], bytes.Take(4));
    }

    [Fact]
    public async Task TranscodeToWebM_garbage_input_fails_with_the_stderr_tail_in_the_message()
    {
        var ffmpeg = Environment.GetEnvironmentVariable("IMPRINT_TEST_FFMPEG");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(ffmpeg),
            "Set IMPRINT_TEST_FFMPEG to an ffmpeg binary to run video transcoding tests.");

        var key = await _store.SaveOriginal(_id, "garbage.mp4", new MemoryStream(Encoding.UTF8.GetBytes("not a video")));
        var processor = new SkiaMediaProcessor(_store, new MediaOptions { RootPath = _root.Path, FfmpegPath = ffmpeg });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => processor.TranscodeToWebM(_id, key));

        Assert.Contains("ffmpeg exited with code", exception.Message);
        Assert.Contains("Output tail:", exception.Message);
    }

    // The clip is generated with the same ffmpeg under test — no binary fixture, and
    // the test cannot drift from what that ffmpeg build can actually read.
    private static byte[] GenerateClip(string ffmpegPath)
    {
        var clipPath = Path.Combine(Path.GetTempPath(), $"imprint-test-clip-{Guid.NewGuid():N}.mp4");
        try
        {
            var startInfo = new ProcessStartInfo(ffmpegPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            string[] arguments =
            [
                "-f", "lavfi", "-i", "color=c=red:size=64x64:rate=10:duration=1",
                "-pix_fmt", "yuv420p", "-y", clipPath,
            ];
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)!;
            var stderr = process.StandardError.ReadToEnd();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"test clip generation failed: {stderr}");
            return File.ReadAllBytes(clipPath);
        }
        finally
        {
            File.Delete(clipPath);
        }
    }
}
