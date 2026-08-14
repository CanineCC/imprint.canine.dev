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
    public string AppLogPath { get; private set; } = "";
    public string PublishDirectory => Path.Combine(_dataDirectory!, "publish");

    public async ValueTask InitializeAsync()
    {
        // Expect() keeps its OWN budget and ignores the context default set in NewPage(), so
        // without this every retrying assertion silently runs on 5s while every other wait has 60.
        Assertions.SetDefaultExpectTimeout(60_000);

        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"imprint-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        var editorProject = FindRepoPath("src/Imprint.Editor");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{editorProject}\" --no-build --ImprintData=\"{_dataDirectory}\" --urls={BaseUrl}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = FindRepoPath("."),
        };
        // Run the editor as Development: there is no launchSettings.json, so `dotnet run`
        // would otherwise default to Production — where the editor refuses to start without
        // Keycloak configured. The E2E suite exercises the app with auth off.
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        // Circuit-level detail: a dead circuit is invisible at Information level, and
        // "the click did nothing" bugs live exactly there.
        startInfo.Environment["Logging__LogLevel__Microsoft.AspNetCore.Components"] = "Debug";
        startInfo.Environment["Logging__LogLevel__Microsoft.AspNetCore.SignalR"] = "Debug";
        // Timestamps, because the question a red run asks is always "what happened during the
        // 60 seconds the test spent waiting" — and that is unanswerable without a clock.
        startInfo.Environment["Logging__Console__FormatterOptions__TimestampFormat"] = "HH:mm:ss.fff ";
        startInfo.Environment["Logging__Console__FormatterOptions__UseUtcTimestamp"] = "true";
        _app = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the editor process.");

        // The app's console is the first place to look when an E2E step goes quiet.
        AppLogPath = Path.Combine(_dataDirectory, "editor-console.log");
        var log = new StreamWriter(AppLogPath) { AutoFlush = true };
        // Stamped on arrival rather than by the app's logger: the question a red run asks is
        // always "what was the server doing during the 60s the test spent waiting", and that is
        // unanswerable without a clock. Doing it here also survives any logging configuration.
        void Write(string line)
        {
            lock (log)
            {
                log.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} {line}");
            }
        }

        _app.OutputDataReceived += (_, e) => { if (e.Data is not null) { Write(e.Data); } };
        _app.ErrorDataReceived += (_, e) => { if (e.Data is not null) { Write("ERR " + e.Data); } };
        _app.BeginOutputReadLine();
        _app.BeginErrorReadLine();

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
        // One generous default budget for every implicit wait, set in one place.
        //
        // This is NOT a way to paper over races — those are fixed at the source (a wait that a
        // PREVIOUS state can satisfy is a bug, and the driver's waits now name the node or count
        // the rows they expect). With those closed the suite finishes in ~20s and never comes near
        // this ceiling; it exists only so a CI box that is genuinely busy does not fail a correct
        // test. Playwright's 30s default is comfortable on an idle machine and marginal on a
        // saturated one, and "the machine was busy" is not a defect worth a red build.
        //
        // Rendering PERFORMANCE is gated separately and precisely, by the publish-time budget test
        // (page/stylesheet/JS byte ceilings), so nothing here is the thing that would catch a
        // slow-down.
        context.SetDefaultTimeout(60_000);
        var page = await context.NewPageAsync();

        // Browser-side failures otherwise vanish silently in headless runs.
        var jsLog = Path.Combine(DataDirectory, "js-console.log");
        page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
            {
                File.AppendAllText(jsLog, $"[console.{message.Type}] {message.Text}\n");
            }
        };
        page.PageError += (_, error) => File.AppendAllText(jsLog, $"[pageerror] {error}\n");
        return page;
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
        // Deliberately left on disk: the temp dir (incl. editor-console.log) is the
        // post-mortem for a red run, and temp cleanup is the OS's job anyway.
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
