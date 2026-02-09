using System.Diagnostics;
using System.Net.Http;

namespace A2UI.Blazor.Playwright;

/// <summary>
/// Assembly-level setup fixture that starts the Python server and Blazor WASM SPA
/// on dedicated test ports before any Playwright tests run, and shuts them down
/// gracefully (SIGTERM) after all tests complete.
///
/// Test ports (15050/15200) are separate from the default dev ports (5050/5200)
/// so tests never interfere with manually-started servers.
/// </summary>
[SetUpFixture]
public class ServerFixture
{
    private static Process? _pythonServer;
    private static Process? _blazorApp;

    private const int TestPythonPort = 15050;
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

        // Always start our own servers on dedicated test ports
        _pythonServer = StartProcess(
            "uv", $"run uvicorn server:app --host 0.0.0.0 --port {TestPythonPort}",
            Path.Combine(repoRoot, "samples", "python-server"),
            environmentVariables: null);

        _blazorApp = StartProcess(
            "dotnet", $"run --no-launch-profile --urls http://0.0.0.0:{TestSpaPort}",
            Path.Combine(repoRoot, "samples", "blazor-wasm-spa"),
            environmentVariables: new Dictionary<string, string>
            {
                // "Testing" environment makes the WASM app load appsettings.Testing.json,
                // which points A2UIServerUrl to the test Python server port.
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            });

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
