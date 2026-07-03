namespace Imprint.Media.Tests;

/// <summary>
/// A throwaway media root per test instance — traversal tests need a real directory
/// boundary to (fail to) escape, so no in-memory fake will do.
/// </summary>
internal sealed class TempMediaRoot : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "imprint-media-tests", Guid.NewGuid().ToString("N"));

    public TempMediaRoot() => Directory.CreateDirectory(Path);

    public MediaOptions Options => new() { RootPath = Path };

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // A test may legitimately have deleted everything already.
        }
    }
}
