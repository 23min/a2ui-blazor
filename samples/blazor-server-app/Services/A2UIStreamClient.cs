using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using System.Net.Http.Json;

namespace blazor_server_app.Services;

public sealed class A2UIStreamClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonlStreamReader _reader;
    private readonly MessageDispatcher _dispatcher;
    private CancellationTokenSource? _cts;

    public A2UIStreamClient(HttpClient http, JsonlStreamReader reader, MessageDispatcher dispatcher)
    {
        _http = http;
        _reader = reader;
        _dispatcher = dispatcher;
    }

    public async Task ConnectAsync(string agentPath)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var request = new HttpRequestMessage(HttpMethod.Get, agentPath);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(_cts.Token);

        await foreach (var message in _reader.ReadMessagesAsync(stream, _cts.Token))
        {
            _dispatcher.Dispatch(message);
        }
    }

    public async Task SendActionAsync(string agentPath, A2UIUserAction action)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, agentPath);
        request.Content = JsonContent.Create(action);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        await foreach (var message in _reader.ReadMessagesAsync(stream, CancellationToken.None))
        {
            _dispatcher.Dispatch(message);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
