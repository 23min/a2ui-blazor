using System.Diagnostics;
using System.Net.Http;

namespace A2UI.Blazor.Playwright;

/// <summary>
/// Assembly-level setup fixture that starts the Python server and Blazor WASM SPA
/// before any Playwright tests run, and shuts them down gracefully (SIGTERM) after
/// all tests complete.
///
/// The Python server runs on port 5050 (the default in appsettings.json) because
/// .NET 10 Blazor WASM does not load environment-specific appsettings files.
/// The SPA runs on a dedicated test port (15200) to avoid interfering with
/// manually-started dev servers on 5200.
/// </summary>
[SetUpFixture]
public class ServerFixture
{
    private static Process? _pythonServer;
    private static Process? _blazorApp;

    private const int TestPythonPort = 5050;
    private const int TestSpaPort = 15200;

    public static string SpaBaseUrl =>
        Environment.GetEnvironmentVariable("A2UI_SPA_URL") ?? $"http://localhost:{TestSpaPort}";

    public static string ServerUrl =>
        Environment.GetEnvironmentVariable("A2UI_SERVER_URL") ?? $"http://localhost:{TestPythonPort}";

    [OneTimeSetUp]
    public async Task StartServers()
    {
        // VSCode sets BROWSER to its own helper script, which confuses Playwright.
        Environment.SetEnvironmentVariable("BROWSER", null);

        var repoRoot = FindRepoRoot();

        // Kill any existing listener on the Python server port so we can bind to it.
        KillListenerOnPort(TestPythonPort);

        _pythonServer = StartProcess(
            "uv", $"run uvicorn server:app --host 0.0.0.0 --port {TestPythonPort}",
            Path.Combine(repoRoot, "samples", "python-server"),
            environmentVariables: null);

        _blazorApp = StartProcess(
            "dotnet", $"run --no-launch-profile --urls http://0.0.0.0:{TestSpaPort}",
            Path.Combine(repoRoot, "samples", "blazor-wasm-spa"),
            environmentVariables: null);

        // Wait for both servers to be healthy.
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        await WaitForServer(http, ServerUrl + "/agents/gallery", "Python server", TimeSpan.FromSeconds(30));
        await WaitForServer(http, SpaBaseUrl, "Blazor WASM SPA", TimeSpan.FromSeconds(60));
    }

    [OneTimeTearDown]
    public void StopServers()
    {
        GracefulStop(_blazorApp);
        GracefulStop(_pythonServer);
    }

    private static Process StartProcess(
        string fileName, string arguments, string workingDirectory,
        Dictionary<string, string>? environmentVariables)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.EnvironmentVariables[key] = value;
        }

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {fileName} {arguments}");

        // Drain stdout/stderr asynchronously to prevent the child process from
        // blocking when the OS pipe buffer (4 KB) fills up.
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static async Task WaitForServer(HttpClient http, string url, string name, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // Use ResponseHeadersRead so SSE/streaming endpoints return immediately
                // after sending response headers instead of blocking until stream ends.
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ServerFixture] {name} is ready at {url}");
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"[ServerFixture] {name} did not become ready at {url} within {timeout.TotalSeconds}s");
    }

    /// <summary>
    /// Sends SIGTERM for graceful shutdown. Falls back to SIGKILL only if the process
    /// doesn't exit within the timeout.
    /// </summary>
    private static void GracefulStop(Process? process)
    {
        if (process is null || process.HasExited) return;

        try
        {
            // Send SIGTERM via the kill command (default signal)
            using var kill = Process.Start("kill", process.Id.ToString());
            kill?.WaitForExit(1000);

            // Wait up to 5s for graceful shutdown
            if (!process.WaitForExit(5000))
            {
                // Last resort: SIGKILL
                process.Kill();
                process.WaitForExit(2000);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
        finally
        {
            process.Dispose();
        }
    }

    /// <summary>
    /// Kills any process listening on the given port so the fixture can bind to it.
    /// Uses lsof -sTCP:LISTEN to target only the listener, not client connections.
    /// </summary>
    private static void KillListenerOnPort(int port)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"lsof -i :{port} -sTCP:LISTEN -t | xargs kill 2>/dev/null\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Best-effort — if it fails, StartProcess will fail with "address in use"
        }
    }

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: assume we're in tests/A2UI.Blazor.Playwright/bin/...
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));
    }
}
