using System.Text;
using A2UI.Blazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Services;

/// <summary>
/// Tests for error handling in core services:
/// - Protected event invocations (OnStateChanged, OnSurfaceChanged)
/// - JSON parsing failures
/// - Malformed JSONL handling
/// </summary>
public class ErrorHandlingTests
{
    // ── Protected Event Invocations ─────────────────────────────

    [Fact]
    public void StreamClient_OnStateChanged_SubscriberException_DoesNotCrash()
    {
        var client = CreateStreamClient(_ => Task.FromResult(OkResponse("""{"type":"createSurface","surfaceId":"s1"}""" + "\n")));

        // Subscribe with a handler that throws
        client.OnStateChanged += _ => throw new InvalidOperationException("Subscriber crashed!");

        // Should not throw - the exception should be caught and logged
        var ex = Record.Exception(() =>
        {
            var task = client.ConnectAsync("/test");
            client.Disconnect(); // Stop immediately after start
            task.Wait();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void SurfaceManager_OnSurfaceChanged_SubscriberException_DoesNotCrash()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);

        // Subscribe with a handler that throws
        manager.OnSurfaceChanged += _ => throw new InvalidOperationException("Subscriber crashed!");

        // Should not throw - the exception should be caught and logged
        var ex = Record.Exception(() => manager.CreateSurface("s1", null, false));

        Assert.Null(ex);
    }

    // ── JSON Parsing Failures ───────────────────────────────────

    [Fact]
    public void SurfaceManager_UpdateDataModel_InvalidJson_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        manager.CreateSurface("s1", null, true);

        // Create a JsonElement with invalid nested structure that will fail to parse
        var invalidJson = System.Text.Json.JsonDocument.Parse("""{"nested":"value"}""").RootElement;

        // Simulate trying to parse malformed data - this should be caught
        var ex = Record.Exception(() => manager.UpdateDataModel("s1", "/", invalidJson));

        // Should not throw - error should be caught and logged
        Assert.Null(ex);
    }

    [Fact]
    public void SurfaceManager_UpdateDataModel_UnknownSurface_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var data = System.Text.Json.JsonDocument.Parse("""{"test":"value"}""").RootElement;

        // Trying to update non-existent surface should not throw
        var ex = Record.Exception(() => manager.UpdateDataModel("nonexistent", "/", data));

        Assert.Null(ex);
    }

    [Fact]
    public void SurfaceManager_UpdateComponents_UnknownSurface_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);

        var ex = Record.Exception(() =>
            manager.UpdateComponents("nonexistent", [new() { Id = "root", Component = "Column" }]));

        Assert.Null(ex);
    }

    [Fact]
    public void SurfaceManager_DeleteSurface_UnknownSurface_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);

        var ex = Record.Exception(() => manager.DeleteSurface("nonexistent"));

        Assert.Null(ex);
    }

    // ── Malformed JSONL Handling ────────────────────────────────

    [Fact]
    public async Task JsonlStreamReader_MalformedJson_SkipsLineAndContinues()
    {
        var reader = new JsonlStreamReader(NullLogger<JsonlStreamReader>.Instance);

        var jsonl = """
            {"type":"createSurface","surfaceId":"s1"}
            this is not valid json {{{
            {"type":"deleteSurface","surfaceId":"s2"}
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonl));
        var messages = new List<Protocol.A2UIMessage>();

        await foreach (var msg in reader.ReadMessagesAsync(stream, CancellationToken.None))
        {
            messages.Add(msg);
        }

        // Should have received 2 valid messages, skipping the malformed line
        Assert.Equal(2, messages.Count);
        Assert.Equal("createSurface", messages[0].Type);
        Assert.Equal("deleteSurface", messages[1].Type);
    }

    [Fact]
    public async Task JsonlStreamReader_PartiallyValidJson_SkipsMalformed()
    {
        var reader = new JsonlStreamReader(NullLogger<JsonlStreamReader>.Instance);

        var jsonl = """
            {"type":"createSurface","surfaceId":"s1"}
            {"type":"updateComponents","surfaceId":"s1"
            {"type":"deleteSurface","surfaceId":"s1"}
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonl));
        var messages = new List<Protocol.A2UIMessage>();

        await foreach (var msg in reader.ReadMessagesAsync(stream, CancellationToken.None))
        {
            messages.Add(msg);
        }

        // Should have received 2 valid messages, skipping the incomplete line
        Assert.Equal(2, messages.Count);
        Assert.Equal("createSurface", messages[0].Type);
        Assert.Equal("deleteSurface", messages[1].Type);
    }

    [Fact]
    public async Task JsonlStreamReader_EmptyAndMalformed_HandlesGracefully()
    {
        var reader = new JsonlStreamReader(NullLogger<JsonlStreamReader>.Instance);

        var jsonl = """

            not json
            {"type":"createSurface","surfaceId":"s1"}

            [[[[invalid
            {"type":"deleteSurface","surfaceId":"s1"}

            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonl));
        var messages = new List<Protocol.A2UIMessage>();

        await foreach (var msg in reader.ReadMessagesAsync(stream, CancellationToken.None))
        {
            messages.Add(msg);
        }

        Assert.Equal(2, messages.Count);
    }

    // ── SendActionAsync Error Handling ──────────────────────────

    [Fact]
    public async Task StreamClient_SendActionAsync_NetworkError_Throws()
    {
        var client = CreateStreamClient(_ => throw new HttpRequestException("Network down"));

        var action = new Protocol.A2UIUserAction { Name = "test-action" };

        // Should throw and log the error
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendActionAsync("/test", action));
    }

    [Fact]
    public async Task StreamClient_SendActionAsync_ClientError_Throws()
    {
        var client = CreateStreamClient(_ =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)));

        var action = new Protocol.A2UIUserAction { Name = "test-action" };

        // Should throw and log the error
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendActionAsync("/test", action));
    }

    [Fact]
    public async Task StreamClient_SendActionAsync_ServerError_Throws()
    {
        var client = CreateStreamClient(_ =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)));

        var action = new Protocol.A2UIUserAction { Name = "test-action" };

        // Should throw and log the error
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendActionAsync("/test", action));
    }

    // ── MessageDispatcher Error Handling ────────────────────────

    [Fact]
    public void MessageDispatcher_UnknownMessageType_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var dispatcher = new MessageDispatcher(manager, NullLogger<MessageDispatcher>.Instance);

        var ex = Record.Exception(() =>
            dispatcher.Dispatch(new Protocol.A2UIMessage { Type = "unknownType", SurfaceId = "s1" }));

        Assert.Null(ex);
    }

    [Fact]
    public void MessageDispatcher_NullSurfaceId_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var dispatcher = new MessageDispatcher(manager, NullLogger<MessageDispatcher>.Instance);

        var ex = Record.Exception(() =>
            dispatcher.Dispatch(new Protocol.A2UIMessage { Type = "createSurface", SurfaceId = null }));

        Assert.Null(ex);
    }

    [Fact]
    public void MessageDispatcher_NullComponents_DoesNotThrow()
    {
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var dispatcher = new MessageDispatcher(manager, NullLogger<MessageDispatcher>.Instance);

        dispatcher.Dispatch(new Protocol.A2UIMessage { Type = "createSurface", SurfaceId = "s1" });

        var ex = Record.Exception(() =>
            dispatcher.Dispatch(new Protocol.A2UIMessage { Type = "updateComponents", SurfaceId = "s1", Components = null }));

        Assert.Null(ex);
    }

    // ── ComponentRegistry Error Handling ────────────────────────

    [Fact]
    public void ComponentRegistry_ResolveUnknown_ReturnsNull()
    {
        var registry = new ComponentRegistry(NullLogger<ComponentRegistry>.Instance);
        registry.RegisterStandardComponents();

        var result = registry.Resolve("NonExistentComponent");

        Assert.Null(result);
    }

    [Fact]
    public void ComponentRegistry_ResolveEmpty_ReturnsNull()
    {
        var registry = new ComponentRegistry(NullLogger<ComponentRegistry>.Instance);
        registry.RegisterStandardComponents();

        var result = registry.Resolve("");

        Assert.Null(result);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static A2UIStreamClient CreateStreamClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var http = new HttpClient(new DelegateHandler(handler))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var manager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        var dispatcher = new MessageDispatcher(manager, NullLogger<MessageDispatcher>.Instance);
        var reader = new JsonlStreamReader(NullLogger<JsonlStreamReader>.Instance);
        var logger = NullLogger<A2UIStreamClient>.Instance;
        return new A2UIStreamClient(http, reader, dispatcher, manager, logger);
    }

    private static HttpResponseMessage OkResponse(string body)
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
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
