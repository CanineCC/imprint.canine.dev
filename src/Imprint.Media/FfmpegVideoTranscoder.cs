using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Imprint.Media;

/// <summary>
/// Runs the external ffmpeg binary to produce VP9/Opus WebM. ffmpeg is optional
/// infrastructure: when it is missing the transcoder reports why instead of throwing,
/// so the asset pipeline can degrade to publishing the original.
/// </summary>
internal sealed class FfmpegVideoTranscoder(MediaOptions options)
{
    // ffmpeg is interleaved progress chatter; only the tail names the actual error.
    private const int StderrTailChars = 2048;

    // Probed once per process: availability does not flicker, and probing per call
    // would put a process spawn on every upload.
    private readonly Lazy<string?> _unavailableReason =
        new(() => Probe(options.FfmpegPath), LazyThreadSafetyMode.ExecutionAndPublication);

    public string? UnavailableReason => _unavailableReason.Value;

    /// <summary>Returns the WebM bytes, or null when ffmpeg is unavailable.</summary>
    public async Task<byte[]?> Transcode(string inputPath, CancellationToken ct = default)
    {
        if (UnavailableReason is not null)
        {
            return null;
        }

        // ffmpeg writes to a real file (it needs to seek to finalize the container);
        // the bytes are then handed to the media store, which owns key layout.
        var outputPath = Path.Combine(Path.GetTempPath(), $"imprint-transcode-{Guid.NewGuid():N}.webm");
        try
        {
            var startInfo = new ProcessStartInfo(options.FfmpegPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            string[] arguments =
            [
                "-i", inputPath,
                "-c:v", "libvpx-vp9", "-crf", "33", "-b:v", "0", "-row-mt", "1",
                "-c:a", "libopus",
                "-y", outputPath,
            ];
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"ffmpeg failed to start from '{options.FfmpegPath}'.");

            // Both pipes must be drained while waiting or ffmpeg blocks on a full pipe.
            var stderrTail = ReadTail(process.StandardError, StderrTailChars);
            var stdoutDrain = process.StandardOutput.ReadToEndAsync(CancellationToken.None);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(options.VideoTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                await process.WaitForExitAsync(CancellationToken.None);
                ct.ThrowIfCancellationRequested();
                throw new InvalidOperationException(
                    $"ffmpeg did not finish within {options.VideoTimeout.TotalMinutes:0.#} minutes and was terminated. " +
                    $"Output tail: {await stderrTail}");
            }

            await stdoutDrain;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ffmpeg exited with code {process.ExitCode}. Output tail: {await stderrTail}");
            }

            return await File.ReadAllBytesAsync(outputPath, ct);
        }
        finally
        {
            try
            {
                File.Delete(outputPath);
            }
            catch (IOException)
            {
                // A leaked temp file is preferable to masking the real outcome.
            }
        }
    }

    private static string? Probe(string ffmpegPath)
    {
        var guidance =
            $"ffmpeg was not found at '{ffmpegPath}'. Install ffmpeg (or set MediaOptions.FfmpegPath " +
            "to its location) to enable WebM video transcoding; until then videos publish as uploaded.";
        try
        {
            using var process = Process.Start(new ProcessStartInfo(ffmpegPath, "-version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
            {
                return guidance;
            }

            // '-version' answers instantly; a long wait means something is deeply
            // wrong with the binary, which counts as unavailable.
            if (!process.WaitForExit(10_000))
            {
                TryKill(process);
                return $"'{ffmpegPath} -version' did not respond and was terminated; " +
                       "video transcoding is disabled.";
            }

            return process.ExitCode == 0
                ? null
                : $"'{ffmpegPath} -version' exited with code {process.ExitCode}; video transcoding is disabled.";
        }
        catch (Exception exception) when (exception is Win32Exception or PlatformNotSupportedException or InvalidOperationException)
        {
            return guidance;
        }
    }

    private static async Task<string> ReadTail(StreamReader reader, int maxChars)
    {
        var buffer = new char[1024];
        var tail = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            tail.Append(buffer, 0, read);
            if (tail.Length > maxChars)
            {
                tail.Remove(0, tail.Length - maxChars);
            }
        }

        return tail.ToString();
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited between the timeout and the kill — the race is benign.
        }
    }
}
