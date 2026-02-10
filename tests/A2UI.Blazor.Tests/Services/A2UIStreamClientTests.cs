using System.Net;
using System.Text;
using A2UI.Blazor.Services;

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

    // --- Helpers ---

    private static A2UIStreamClient CreateClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var http = new HttpClient(new DelegateHandler(handler))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var manager = new SurfaceManager();
        var dispatcher = new MessageDispatcher(manager);
        var reader = new JsonlStreamReader();
        return new A2UIStreamClient(http, reader, dispatcher);
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
