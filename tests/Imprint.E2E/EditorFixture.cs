using System.Diagnostics;
using System.Net;
using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// Boots the real editor (dotnet run against a throwaway data directory) and one real
/// Chromium. Shared across the E2E collection: the suite drives one editor instance
/// the way one human would.
/// </summary>
public sealed class EditorFixture : IAsyncLifetime
{
    private Process? _app;
    private IPlaywright? _playwright;
    private string? _dataDirectory;

    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = null!;
    public string DataDirectory => _dataDirectory!;
    public string PublishDirectory => Path.Combine(_dataDirectory!, "publish");

    public async ValueTask InitializeAsync()
    {
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        var editorProject = FindRepoPath("src/Imprint.Editor");
        _app = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{editorProject}\" --no-build --ImprintData=\"{_dataDirectory}\" --urls={BaseUrl}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = FindRepoPath("."),
        }) ?? throw new InvalidOperationException("Failed to start the editor process.");

        await WaitForHttp(new Uri(BaseUrl + "/"), TimeSpan.FromSeconds(60));

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("IMPRINT_E2E_HEADED") != "1",
        });
    }

    public async Task<IPage> NewPage()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            BaseURL = BaseUrl,
        });
        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_app is { HasExited: false })
        {
            _app.Kill(entireProcessTree: true);
            await _app.WaitForExitAsync();
        }

        _app?.Dispose();
        if (_dataDirectory is not null && Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    private static int FreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task WaitForHttp(Uri url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(250);
        }

        throw new TimeoutException($"The editor did not become reachable at {url} within {timeout}.");
    }

    /// <summary>Walks up from the test binary to the repo root (identified by Imprint.slnx).</summary>
    public static string FindRepoPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Imprint.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repo root from the test binary.");
        }

        return Path.GetFullPath(Path.Combine(directory.FullName, relative));
    }
}

[CollectionDefinition("editor")]
public sealed class EditorCollection : ICollectionFixture<EditorFixture>;
