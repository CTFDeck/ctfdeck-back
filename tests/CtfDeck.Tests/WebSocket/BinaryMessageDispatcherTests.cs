using System.Net.WebSockets;
using CtfDeck.Contracts.Transport;
using CtfDeck.WsServer.WebSocket;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class BinaryMessageDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_CommandExecuteCd_ShouldCallCdHandlerOnly()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-cd", socket);

        var cdCalls = 0;
        var streamingCalls = 0;

        var dispatcher = new BinaryMessageDispatcher(
            (c, cmd) =>
            {
                cdCalls++;
                cmd.Command.Should().Be("cd /tmp");
                return Task.CompletedTask;
            },
            (c, cmd) =>
            {
                streamingCalls++;
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            []);

        var payload = CreateCommandExecute("cd /tmp", Guid.NewGuid());

        await dispatcher.DispatchAsync(ctx, payload, CancellationToken.None);

        cdCalls.Should().Be(1);
        streamingCalls.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_CommandExecuteNonCd_ShouldCallStreamingHandler()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-stream", socket);

        var streamingCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, cmd) =>
            {
                cmd.Command.Should().Be("echo ok");
                streamingCalled.TrySetResult(true);
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            []);

        var payload = CreateCommandExecute("echo ok", Guid.NewGuid());

        await dispatcher.DispatchAsync(ctx, payload, CancellationToken.None);

        (await Task.WhenAny(streamingCalled.Task, Task.Delay(1000))).Should().Be(streamingCalled.Task);
    }

    [Fact]
    public async Task DispatchAsync_CommandKill_ShouldCallKillHandler()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-kill", socket);

        var expectedId = Guid.NewGuid();
        Guid? actualId = null;

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, id) =>
            {
                actualId = id;
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            []);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandKill);
        writer.WriteGuid(expectedId);

        await dispatcher.DispatchAsync(ctx, writer.ToArray(), CancellationToken.None);

        actualId.Should().Be(expectedId);
    }

    [Theory]
    [InlineData(CommandSignalKind.Interrupt)]
    [InlineData(CommandSignalKind.Eof)]
    public async Task DispatchAsync_CommandSignal_ShouldCallSignalHandler(CommandSignalKind kind)
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-signal", socket);

        var expectedId = Guid.NewGuid();
        Guid? actualId = null;
        CommandSignalKind? actualKind = null;

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, id, k) =>
            {
                actualId = id;
                actualKind = k;
                return Task.CompletedTask;
            },
            []);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandSignal);
        writer.WriteGuid(expectedId);
        writer.WriteByte((byte)kind);

        await dispatcher.DispatchAsync(ctx, writer.ToArray(), CancellationToken.None);

        actualId.Should().Be(expectedId);
        actualKind.Should().Be(kind);
    }

    [Fact]
    public async Task DispatchAsync_PasswordProvideWithoutWaiter_ShouldNotThrow()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-password-none", socket);
        var messageId = Guid.NewGuid();

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            []);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.PasswordProvide);
        writer.WriteGuid(messageId);
        writer.WriteInt32(0);

        await dispatcher.DispatchAsync(ctx, writer.ToArray(), CancellationToken.None);

        ctx.SudoWaiters.ContainsKey(messageId).Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_PasswordProvideWithWaiter_ShouldResolveWaiter()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-password", socket);
        var messageId = Guid.NewGuid();

        var waiter = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.SudoWaiters[messageId] = waiter;

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            []);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.PasswordProvide);
        writer.WriteGuid(messageId);
        writer.WriteInt32("secret"u8.Length);
        writer.WriteBytes("secret"u8);

        await dispatcher.DispatchAsync(ctx, writer.ToArray(), CancellationToken.None);

        (await waiter.Task).Should().Be("secret");
        ctx.SudoWaiters.ContainsKey(messageId).Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_DefaultWhenNotHandled_ShouldLogUnknownMessage()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-unknown", socket);
        var logs = new List<string>();

        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            [],
            logs.Add);

        await dispatcher.DispatchAsync(ctx, new byte[] { 255 }, CancellationToken.None);

        logs.Should().ContainSingle(m => m.Contains("Unknown message from client client-unknown"));
    }

    [Fact]
    public async Task DispatchAsync_DefaultWhenHandled_ShouldNotLogUnknownMessage()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-handled", socket);
        var logs = new List<string>();

        var handler = new StubHandler(true);
        var dispatcher = new BinaryMessageDispatcher(
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            [handler],
            logs.Add);

        await dispatcher.DispatchAsync(ctx, new byte[] { 250 }, CancellationToken.None);

        logs.Should().BeEmpty();
    }

    private static byte[] CreateCommandExecute(string command, Guid messageId)
    {
        var cmdBytes = System.Text.Encoding.UTF8.GetBytes(command);
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandExecute);
        writer.WriteInt32(cmdBytes.Length);
        writer.WriteBytes(cmdBytes);
        writer.WriteGuid(messageId);
        return writer.ToArray();
    }

    private sealed class StubHandler(bool shouldHandle) : MessageHandlerBase
    {
        protected override bool CanHandle(MessageType type) => shouldHandle;
        protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data) => [];
        protected override byte[] SerializeError(Guid messageId, string error) => [];
    }

    private sealed class MockWebSocket : System.Net.WebSockets.WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Abort() => _state = WebSocketState.Aborted;

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
        }
    }
}
