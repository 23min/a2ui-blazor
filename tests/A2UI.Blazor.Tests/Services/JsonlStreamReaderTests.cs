using System.Text;
using A2UI.Blazor.Services;

namespace A2UI.Blazor.Tests.Services;

public class JsonlStreamReaderTests
{
    private readonly JsonlStreamReader _reader = new();

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private async Task<List<A2UI.Blazor.Protocol.A2UIMessage>> ReadAll(string content, CancellationToken ct = default)
    {
        var messages = new List<A2UI.Blazor.Protocol.A2UIMessage>();
        await foreach (var msg in _reader.ReadMessagesAsync(ToStream(content), ct))
        {
            messages.Add(msg);
        }
        return messages;
    }

    [Fact]
    public async Task ReadMessages_PlainJsonl_ParsesMessage()
    {
        var messages = await ReadAll("""{"type":"createSurface","surfaceId":"s1"}""");
        Assert.Single(messages);
        Assert.Equal("createSurface", messages[0].Type);
        Assert.Equal("s1", messages[0].SurfaceId);
    }

    [Fact]
    public async Task ReadMessages_SseFormatWithSpace_ParsesMessage()
    {
        var messages = await ReadAll("""
data: {"type":"createSurface","surfaceId":"s1"}

""");
        Assert.Single(messages);
        Assert.Equal("createSurface", messages[0].Type);
    }

    [Fact]
    public async Task ReadMessages_SseFormatNoSpace_ParsesMessage()
    {
        var messages = await ReadAll("""
data:{"type":"createSurface","surfaceId":"s1"}

""");
        Assert.Single(messages);
        Assert.Equal("createSurface", messages[0].Type);
    }

    [Fact]
    public async Task ReadMessages_EmptyLines_AreSkipped()
    {
        var messages = await ReadAll("""

{"type":"createSurface","surfaceId":"s1"}

""");
        Assert.Single(messages);
    }

    [Fact]
    public async Task ReadMessages_SseComments_AreSkipped()
    {
        var messages = await ReadAll("""
: keepalive
{"type":"createSurface","surfaceId":"s1"}
: another comment
""");
        Assert.Single(messages);
    }

    [Fact]
    public async Task ReadMessages_DoneMarker_IsSkipped()
    {
        var messages = await ReadAll("""
{"type":"createSurface","surfaceId":"s1"}
[DONE]
""");
        Assert.Single(messages);
    }

    [Fact]
    public async Task ReadMessages_MalformedJson_IsSkipped()
    {
        var messages = await ReadAll("""
{"type":"createSurface","surfaceId":"s1"}
not valid json {{{
{"type":"deleteSurface","surfaceId":"s2"}
""");
        Assert.Equal(2, messages.Count);
        Assert.Equal("s1", messages[0].SurfaceId);
        Assert.Equal("s2", messages[1].SurfaceId);
    }

    [Fact]
    public async Task ReadMessages_MultipleMessages_ParsesAll()
    {
        var messages = await ReadAll("""
data: {"type":"createSurface","surfaceId":"s1"}

data: {"type":"updateDataModel","surfaceId":"s1","path":"/"}

data: {"type":"updateComponents","surfaceId":"s1","components":[]}

""");
        Assert.Equal(3, messages.Count);
        Assert.Equal("createSurface", messages[0].Type);
        Assert.Equal("updateDataModel", messages[1].Type);
        Assert.Equal("updateComponents", messages[2].Type);
    }

    [Fact]
    public async Task ReadMessages_EmptyStream_ReturnsNothing()
    {
        var messages = await ReadAll("");
        Assert.Empty(messages);
    }

    [Fact]
    public async Task ReadMessages_CancellationToken_StopsReading()
    {
        var cts = new CancellationTokenSource();
        // Long stream that keeps producing data
        var content = string.Join("\n", Enumerable.Range(0, 1000)
            .Select(i => $$"""{"type":"updateDataModel","surfaceId":"s{{i}}"}"""));

        var messages = new List<A2UI.Blazor.Protocol.A2UIMessage>();
        await foreach (var msg in _reader.ReadMessagesAsync(ToStream(content), cts.Token))
        {
            messages.Add(msg);
            if (messages.Count >= 3)
                cts.Cancel();
        }
        // Should have stopped early due to cancellation
        Assert.True(messages.Count < 1000);
    }
}
