using A2UI.Blazor.Protocol;
using System.Net;
using System.Net.Http.Json;

namespace A2UI.Blazor.Services;

/// <summary>
/// Connects to an A2UI agent endpoint, reads the JSONL/SSE stream,
/// and dispatches messages to the SurfaceManager. Automatically
/// reconnects with exponential backoff when the stream drops.
/// </summary>
public sealed class A2UIStreamClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonlStreamReader _reader;
    private readonly MessageDispatcher _dispatcher;
    private CancellationTokenSource? _cts;

    private const int MaxDelayMs = 30_000;
    private const int BaseDelayMs = 1_000;

    public A2UIStreamClient(HttpClient http, JsonlStreamReader reader, MessageDispatcher dispatcher)
    {
        _http = http;
        _reader = reader;
        _dispatcher = dispatcher;
    }

    public StreamConnectionState State { get; private set; }

    public event Action<StreamConnectionState>? OnStateChanged;

    public async Task ConnectAsync(string agentPath)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        int attempt = 0;

        while (!token.IsCancellationRequested)
        {
            SetState(attempt == 0 ? StreamConnectionState.Connecting : StreamConnectionState.Reconnecting);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, agentPath);
                EnableBrowserStreaming(request);
                var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                if (!response.IsSuccessStatusCode && IsClientError(response.StatusCode))
                {
                    SetState(StreamConnectionState.Disconnected);
                    response.EnsureSuccessStatusCode(); // throws
                }

                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(token);

                await foreach (var message in _reader.ReadMessagesAsync(stream, token))
                {
                    if (State != StreamConnectionState.Connected)
                    {
                        attempt = 0;
                        SetState(StreamConnectionState.Connected);
                    }
                    _dispatcher.Dispatch(message);
                }

                // Stream ended normally — reconnect
                attempt++;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue && IsClientError(ex.StatusCode.Value))
            {
                SetState(StreamConnectionState.Disconnected);
                throw;
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                // Network errors, browser fetch aborts, stream drops — retry
                attempt++;
            }

            if (token.IsCancellationRequested) break;

            var delay = ComputeDelay(attempt);
            SetState(StreamConnectionState.Reconnecting);

            try
            {
                await Task.Delay(delay, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        SetState(StreamConnectionState.Disconnected);
    }

    public async Task SendActionAsync(string agentPath, A2UIUserAction action)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, agentPath);
        request.Content = JsonContent.Create(action);
        EnableBrowserStreaming(request);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        await foreach (var message in _reader.ReadMessagesAsync(stream, CancellationToken.None))
        {
            _dispatcher.Dispatch(message);
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void SetState(StreamConnectionState state)
    {
        if (State == state) return;
        State = state;
        OnStateChanged?.Invoke(state);
    }

    internal static int ComputeDelay(int attempt)
    {
        var baseDelay = Math.Min(BaseDelayMs * (1 << Math.Min(attempt, 15)), MaxDelayMs);
        // ±20% jitter
        var jitter = (int)(baseDelay * 0.2);
        return baseDelay + Random.Shared.Next(-jitter, jitter + 1);
    }

    private static bool IsClientError(HttpStatusCode statusCode)
        => (int)statusCode >= 400 && (int)statusCode < 500;

    private static void EnableBrowserStreaming(HttpRequestMessage request)
    {
        if (OperatingSystem.IsBrowser())
            request.Options.Set(new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse"), true);
    }
}
