using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using A2UI.Blazor.Diagnostics;
using A2UI.Blazor.Protocol;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<A2UIStreamClient> _logger;
    private CancellationTokenSource? _cts;

    private const int MaxDelayMs = 30_000;
    private const int BaseDelayMs = 1_000;

    public A2UIStreamClient(HttpClient http, JsonlStreamReader reader, MessageDispatcher dispatcher, ILogger<A2UIStreamClient> logger)
    {
        _http = http;
        _reader = reader;
        _dispatcher = dispatcher;
        _logger = logger;
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

            if (attempt == 0)
            {
                _logger.LogInformation(LogEvents.Connecting, "Connecting to {AgentPath}", agentPath);
            }
            else
            {
                _logger.LogInformation(LogEvents.Reconnecting, "Reconnecting to {AgentPath} (attempt {Attempt})", agentPath, attempt);
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, agentPath);
                EnableBrowserStreaming(request);
                var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                if (!response.IsSuccessStatusCode && IsClientError(response.StatusCode))
                {
                    _logger.LogError(LogEvents.ClientError, "Client error {StatusCode} connecting to {AgentPath}",
                        (int)response.StatusCode, agentPath);
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
                        _logger.LogInformation(LogEvents.Connected, "Connected to {AgentPath}", agentPath);
                        SetState(StreamConnectionState.Connected);
                    }
                    _dispatcher.Dispatch(message);
                }

                // Stream ended normally — reconnect
                _logger.LogInformation(LogEvents.StreamEnded, "Stream ended for {AgentPath}, will reconnect", agentPath);
                attempt++;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue && IsClientError(ex.StatusCode.Value))
            {
                _logger.LogError(LogEvents.ClientError, ex, "Client error {StatusCode} connecting to {AgentPath}",
                    (int)ex.StatusCode.Value, agentPath);
                SetState(StreamConnectionState.Disconnected);
                throw;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                // Network errors, browser fetch aborts, stream drops — retry
                _logger.LogWarning(LogEvents.StreamError, ex, "Stream error for {AgentPath}, will retry", agentPath);
                attempt++;
            }

            if (token.IsCancellationRequested) break;

            var delay = ComputeDelay(attempt);
            _logger.LogDebug("Delaying {DelayMs}ms before reconnect attempt {Attempt}", delay, attempt + 1);
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

        _logger.LogInformation(LogEvents.Disconnected, "Disconnected from {AgentPath}", agentPath);
        SetState(StreamConnectionState.Disconnected);
    }

    private static readonly string s_capabilitiesJson =
        JsonSerializer.Serialize(new A2UIClientCapabilities());

    public async Task SendActionAsync(string agentPath, A2UIUserAction action)
    {
        try
        {
            _logger.LogDebug(LogEvents.SendingAction, "Sending action {ActionName} to {AgentPath}", action.Name, agentPath);

            var envelope = new A2UIClientMessage { Action = action };
            var request = new HttpRequestMessage(HttpMethod.Post, agentPath);
            request.Content = JsonContent.Create(envelope);
            request.Headers.Add("A2UI-Client-Capabilities", s_capabilitiesJson);
            EnableBrowserStreaming(request);

            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            await foreach (var message in _reader.ReadMessagesAsync(stream, CancellationToken.None))
            {
                _dispatcher.Dispatch(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.ActionFailed, ex, "Failed to send action {ActionName} to {AgentPath}", action.Name, agentPath);
            throw;
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

        try
        {
            OnStateChanged?.Invoke(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscriber error in OnStateChanged for state {State}", state);
        }
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
