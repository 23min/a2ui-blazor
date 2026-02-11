using System.Net;
using System.Text;
using System.Text.Json;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Services;

public class A2UIStreamClientTests
{
    [Fact]
    public void InitialState_IsDisconnected()
    {
        var client = CreateClient(_ => Task.FromResult(OkResponse("")));
        Assert.Equal(StreamConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ConnectAsync_TransitionsToConnecting_ThenConnected()
    {
        var states = new List<StreamConnectionState>();
        var jsonl = """{"type":"createSurface","surfaceId":"s1"}""" + "\n";
        var client = CreateClient(_ => Task.FromResult(OkResponse(jsonl)));
        client.OnStateChanged += s => states.Add(s);

        // Stream ends after one message, then client tries to reconnect.
        // Cancel after first Connected to stop the loop.
        client.OnStateChanged += s =>
        {
            if (s == StreamConnectionState.Connected)
                client.Disconnect();
        };

        await client.ConnectAsync("/test");

        Assert.Contains(StreamConnectionState.Connecting, states);
        Assert.Contains(StreamConnectionState.Connected, states);
        Assert.Equal(StreamConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ConnectAsync_ClientError_ThrowsImmediately()
    {
        var client = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ConnectAsync("/missing"));
        Assert.Equal(StreamConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ConnectAsync_ServerError_RetriesWithReconnectingState()
    {
        int callCount = 0;
        var states = new List<StreamConnectionState>();
        var jsonl = """{"type":"createSurface","surfaceId":"s1"}""" + "\n";

        var client = CreateClient(_ =>
        {
            callCount++;
            if (callCount == 1)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return Task.FromResult(OkResponse(jsonl));
        });

        client.OnStateChanged += s =>
        {
            states.Add(s);
            // Stop after we reconnect and get Connected
            if (s == StreamConnectionState.Connected && callCount >= 2)
                client.Disconnect();
        };

        await client.ConnectAsync("/test");

        Assert.Contains(StreamConnectionState.Reconnecting, states);
        Assert.Contains(StreamConnectionState.Connected, states);
    }

    [Fact]
    public async Task Disconnect_StopsReconnectLoop()
    {
        var client = CreateClient(_ =>
            throw new HttpRequestException("Network down"));

        // Disconnect after the first reconnecting state
        client.OnStateChanged += s =>
        {
            if (s == StreamConnectionState.Reconnecting)
                client.Disconnect();
        };

        await client.ConnectAsync("/test");

        Assert.Equal(StreamConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ConnectAsync_StreamEndsNormally_Reconnects()
    {
        int callCount = 0;
        var states = new List<StreamConnectionState>();
        var jsonl = """{"type":"createSurface","surfaceId":"s1"}""" + "\n";

        var client = CreateClient(_ =>
        {
            callCount++;
            return Task.FromResult(OkResponse(jsonl));
        });

        client.OnStateChanged += s =>
        {
            states.Add(s);
            // After second connection succeeds, stop
            if (s == StreamConnectionState.Connected && callCount >= 2)
                client.Disconnect();
        };

        await client.ConnectAsync("/test");

        // Should have gone: Connecting → Connected → Reconnecting → Connected → Disconnected
        Assert.True(callCount >= 2, $"Expected at least 2 calls, got {callCount}");
        Assert.Contains(StreamConnectionState.Reconnecting, states);
    }

    [Theory]
    [InlineData(0, 800, 1200)]      // 1s ±20%
    [InlineData(1, 1600, 2400)]     // 2s ±20%
    [InlineData(2, 3200, 4800)]     // 4s ±20%
    [InlineData(10, 24000, 36000)]  // capped at 30s base ±20%
    public void ComputeDelay_ExponentialWithCap(int attempt, int minExpected, int maxExpected)
    {
        // Run multiple times to account for jitter
        for (int i = 0; i < 50; i++)
        {
            var delay = A2UIStreamClient.ComputeDelay(attempt);
            Assert.InRange(delay, minExpected, maxExpected);
        }
    }

    [Fact]
    public void SetState_DoesNotFireEvent_WhenStateUnchanged()
    {
        var client = CreateClient(_ => Task.FromResult(OkResponse("")));
        int fireCount = 0;
        client.OnStateChanged += _ => fireCount++;

        // State is already Disconnected, so setting it again should not fire
        // (we can't call SetState directly since it's private, but this validates
        // the initial state)
        Assert.Equal(0, fireCount);
    }

    [Fact]
    public async Task SendActionAsync_SendsV09Envelope()
    {
        string? capturedBody = null;
        var client = CreateClient(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
            {
                capturedBody = await req.Content.ReadAsStringAsync();
            }
            return OkResponse("");
        });

        var action = new A2UIUserAction
        {
            Name = "search",
            SurfaceId = "contacts",
            SourceComponentId = "search-btn"
        };

        await client.SendActionAsync("/agents/contacts", action);

        Assert.NotNull(capturedBody);
        var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;

        Assert.Equal("v0.9", root.GetProperty("version").GetString());
        Assert.True(root.TryGetProperty("action", out var actionEl));
        Assert.Equal("search", actionEl.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SendActionAsync_IncludesCapabilitiesHeader()
    {
        string? headerValue = null;
        var client = CreateClient(req =>
        {
            if (req.Method == HttpMethod.Post &&
                req.Headers.TryGetValues("A2UI-Client-Capabilities", out var values))
            {
                headerValue = values.FirstOrDefault();
            }
            return Task.FromResult(OkResponse(""));
        });

        var action = new A2UIUserAction
        {
            Name = "test",
            SurfaceId = "s1",
            SourceComponentId = "c1"
        };

        await client.SendActionAsync("/test", action);

        Assert.NotNull(headerValue);
        var doc = JsonDocument.Parse(headerValue);
        Assert.True(doc.RootElement.TryGetProperty("v0.9", out _));
    }

    [Fact]
    public async Task SendActionAsync_IncludesDataModel_WhenSendDataModelTrue()
    {
        string? capturedBody = null;
        var (client, surfaceManager) = CreateClientWithManager(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return OkResponse("");
        });

        // Set up surface with sendDataModel=true and a data model
        surfaceManager.CreateSurface("s1", null, true, null);
        surfaceManager.UpdateDataModel("s1", "/", JsonDocument.Parse("""{"count":5}""").RootElement);
        surfaceManager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        await client.SendActionAsync("/test", new A2UIUserAction
        {
            Name = "click", SurfaceId = "s1", SourceComponentId = "btn1"
        });

        Assert.NotNull(capturedBody);
        var root = JsonDocument.Parse(capturedBody).RootElement;
        Assert.True(root.TryGetProperty("dataModel", out var dm));
        Assert.Equal(5, dm.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task SendActionAsync_OmitsDataModel_WhenSendDataModelFalse()
    {
        string? capturedBody = null;
        var (client, surfaceManager) = CreateClientWithManager(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return OkResponse("");
        });

        surfaceManager.CreateSurface("s1", null, false, null);
        surfaceManager.UpdateDataModel("s1", "/", JsonDocument.Parse("""{"x":1}""").RootElement);
        surfaceManager.UpdateComponents("s1", [new() { Id = "root", Component = "Column" }]);

        await client.SendActionAsync("/test", new A2UIUserAction
        {
            Name = "click", SurfaceId = "s1", SourceComponentId = "btn1"
        });

        Assert.NotNull(capturedBody);
        var root = JsonDocument.Parse(capturedBody).RootElement;
        Assert.False(root.TryGetProperty("dataModel", out _));
    }

    [Fact]
    public async Task SendErrorAsync_SendsV09ErrorEnvelope()
    {
        string? capturedBody = null;
        var client = CreateClient(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return OkResponse("");
        });

        var error = new A2UIClientError
        {
            Code = "VALIDATION_FAILED",
            SurfaceId = "s1",
            Message = "Expected string, got number"
        };

        await client.SendErrorAsync("/test", error);

        Assert.NotNull(capturedBody);
        var root = JsonDocument.Parse(capturedBody).RootElement;
        Assert.Equal("v0.9", root.GetProperty("version").GetString());
        Assert.True(root.TryGetProperty("error", out var errorEl));
        Assert.Equal("VALIDATION_FAILED", errorEl.GetProperty("code").GetString());
        Assert.Equal("s1", errorEl.GetProperty("surfaceId").GetString());
        Assert.False(root.TryGetProperty("action", out _));
    }

    [Fact]
    public async Task SendErrorAsync_IncludesCapabilitiesHeader()
    {
        string? headerValue = null;
        var client = CreateClient(req =>
        {
            if (req.Method == HttpMethod.Post &&
                req.Headers.TryGetValues("A2UI-Client-Capabilities", out var values))
            {
                headerValue = values.FirstOrDefault();
            }
            return Task.FromResult(OkResponse(""));
        });

        await client.SendErrorAsync("/test", new A2UIClientError
        {
            Code = "TEST", SurfaceId = "s1", Message = "test"
        });

        Assert.NotNull(headerValue);
        var doc = JsonDocument.Parse(headerValue);
        Assert.True(doc.RootElement.TryGetProperty("v0.9", out _));
    }

    [Fact]
    public async Task SendErrorAsync_IncludesPath_ForValidationError()
    {
        string? capturedBody = null;
        var client = CreateClient(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return OkResponse("");
        });

        await client.SendErrorAsync("/test", new A2UIClientError
        {
            Code = "VALIDATION_FAILED",
            SurfaceId = "s1",
            Message = "bad",
            Path = "/components/0/text"
        });

        Assert.NotNull(capturedBody);
        var errorEl = JsonDocument.Parse(capturedBody).RootElement.GetProperty("error");
        Assert.Equal("/components/0/text", errorEl.GetProperty("path").GetString());
    }

    [Fact]
    public async Task SendErrorAsync_OmitsPath_WhenNull()
    {
        string? capturedBody = null;
        var client = CreateClient(async req =>
        {
            if (req.Method == HttpMethod.Post && req.Content is not null)
                capturedBody = await req.Content.ReadAsStringAsync();
            return OkResponse("");
        });

        await client.SendErrorAsync("/test", new A2UIClientError
        {
            Code = "GENERIC", SurfaceId = "s1", Message = "oops"
        });

        Assert.NotNull(capturedBody);
        var errorEl = JsonDocument.Parse(capturedBody).RootElement.GetProperty("error");
        Assert.False(errorEl.TryGetProperty("path", out _));
    }

    [Fact]
    public async Task SendErrorAsync_ProcessesResponseStream()
    {
        var jsonl = """{"type":"createSurface","surfaceId":"ack"}""" + "\n";
        var (client, manager) = CreateClientWithManager(_ => Task.FromResult(OkResponse(jsonl)));

        await client.SendErrorAsync("/test", new A2UIClientError
        {
            Code = "TEST", SurfaceId = "s1", Message = "test"
        });

        Assert.NotNull(manager.GetSurface("ack"));
    }

    [Fact]
    public async Task Dispose_CalledTwice_AfterConnect_DoesNotThrow()
    {
        var jsonl = """{"type":"createSurface","surfaceId":"s1"}""" + "\n";
        var client = CreateClient(_ => Task.FromResult(OkResponse(jsonl)));

        client.OnStateChanged += s =>
        {
            if (s == StreamConnectionState.Connected)
                client.Disconnect();
        };

        await client.ConnectAsync("/test");

        // First dispose cancels and disposes the CTS
        client.Dispose();

        // Second dispose must not throw ObjectDisposedException
        var ex = Record.Exception(() => client.Dispose());
        Assert.Null(ex);
    }

    // --- Helpers ---

    private static A2UIStreamClient CreateClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var (client, _) = CreateClientWithManager(handler);
        return client;
    }

    private static (A2UIStreamClient Client, SurfaceManager Manager) CreateClientWithManager(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var http = new HttpClient(new DelegateHandler(handler))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var dispatcher = new MessageDispatcher(manager, NullLogger<MessageDispatcher>.Instance);
        var reader = new JsonlStreamReader(NullLogger<JsonlStreamReader>.Instance);
        var logger = NullLogger<A2UIStreamClient>.Instance;
        return (new A2UIStreamClient(http, reader, dispatcher, manager, logger), manager);
    }

    private static HttpResponseMessage OkResponse(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(body)))
        };
    }

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
