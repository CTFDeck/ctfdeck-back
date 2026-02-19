using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using Xunit;
using CtfDeck.Contracts.Transport;
using CtfDeck.ServerWs.WebSocket;


namespace CtfDeck.Tests.WebSocket;

public class OutputBatcherTests
{
    private class MockWebSocket : System.Net.WebSockets.WebSocket
    {
        public List<(byte[] Data, WebSocketMessageType MessageType, bool EndOfMessage)> SentMessages { get; } = new();
        private WebSocketState _state = WebSocketState.Open;
        public override WebSocketState State => _state;
        public override string? CloseStatusDescription => null;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? SubProtocol => null;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SentMessages.Add((buffer.ToArray(), messageType, endOfMessage));
            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() { }
    }

    private static WebSocketSender CreateSender(MockWebSocket socket)
        => new(socket, new SemaphoreSlim(1, 1));

    [Fact]
    public async Task EnqueueAsync_ShouldBatchOutputs()
    {
        // Arrange
        var socket = new MockWebSocket();
        var messageId = Guid.NewGuid();
        await using var batcher = new OutputBatcher(CreateSender(socket), messageId);

        // Act
        await batcher.EnqueueAsync("hello ", false);
        await batcher.EnqueueAsync("world", false);

        // Wait for batch delay
        await Task.Delay(50);

        await batcher.CompleteAsync(0, "/test");

        // Assert
        socket.SentMessages.Count.Should().BeGreaterThanOrEqualTo(2); // One for output, one for end

        // Verify output message
        var outputMsg = socket.SentMessages.First(m => (MessageType)m.Data[0] == MessageType.StreamOutput);
        var chunk = StreamChunkMessage.Deserialize(outputMsg.Data);
        chunk.Data.Should().Be("hello world");
        chunk.MessageId.Should().Be(messageId);

        // Verify end message
        var endMsg = socket.SentMessages.First(m => (MessageType)m.Data[0] == MessageType.StreamEnd);
        var end = StreamEndMessage.Deserialize(endMsg.Data);
        end.ExitCode.Should().Be(0);
        end.WorkingDirectory.Should().Be("/test");
        end.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldSeparateStdoutAndStderr()
    {
        // Arrange
        var socket = new MockWebSocket();
        var messageId = Guid.NewGuid();
        await using var batcher = new OutputBatcher(CreateSender(socket), messageId);

        // Act
        await batcher.EnqueueAsync("out", false);
        await batcher.EnqueueAsync("err", true);

        await Task.Delay(50);
        await batcher.CompleteAsync(0, "/test");

        // Assert
        var stdoutMsgs = socket.SentMessages.Where(m => (MessageType)m.Data[0] == MessageType.StreamOutput).ToList();
        var stderrMsgs = socket.SentMessages.Where(m => (MessageType)m.Data[0] == MessageType.StreamError).ToList();

        stdoutMsgs.Should().NotBeEmpty();
        stderrMsgs.Should().NotBeEmpty();

        StreamChunkMessage.Deserialize(stdoutMsgs[0].Data).Data.Should().Be("out");
        StreamChunkMessage.Deserialize(stderrMsgs[0].Data).Data.Should().Be("err");
    }

    [Fact]
    public async Task CompleteAsync_ShouldWaitUntilAllBatchesAreSent()
    {
        // Arrange
        var socket = new MockWebSocket();
        var messageId = Guid.NewGuid();
        var batcher = new OutputBatcher(CreateSender(socket), messageId);

        // Act
        await batcher.EnqueueAsync("final message", false);
        await batcher.CompleteAsync(0, "/test");

        // Assert
        var stdoutMsgs = socket.SentMessages.Where(m => (MessageType)m.Data[0] == MessageType.StreamOutput).ToList();
        stdoutMsgs.Should().ContainSingle();
        StreamChunkMessage.Deserialize(stdoutMsgs[0].Data).Data.Should().Be("final message");

        var endMsgs = socket.SentMessages.Where(m => (MessageType)m.Data[0] == MessageType.StreamEnd).ToList();
        endMsgs.Should().ContainSingle();
    }
}
