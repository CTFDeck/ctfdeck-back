using System.Net.WebSockets;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.ServerWs.WebSocket;
using CtfDeck.Terminal.Features.Sessions;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public sealed class CommandDispatcherTests : IDisposable
{
    private readonly CtfDeckDbContext _db = new(":memory:");
    private readonly SessionRepository _sessionRepository;
    private readonly SessionService _sessionService;
    private readonly ActiveSessionManager _activeSessions;

    public CommandDispatcherTests()
    {
        _sessionRepository = new SessionRepository(_db);
        _sessionService = new SessionService(_sessionRepository);
        _activeSessions = new ActiveSessionManager(_sessionService);
    }

    [Fact]
    public async Task RequestSudoPasswordAsync_WhenProvided_ShouldReturnPassword_AndCleanupWaiter()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-1", socket);
        var dispatcher = new CommandDispatcher(_activeSessions, CancellationToken.None);
        var messageId = Guid.NewGuid();

        var waitTask = dispatcher.RequestSudoPasswordAsync(ctx, messageId, CancellationToken.None);
        await WaitUntilAsync(() => ctx.SudoWaiters.ContainsKey(messageId));
        ctx.SudoWaiters[messageId].TrySetResult("secret");

        var result = await waitTask;

        result.Should().Be("secret");
        ctx.SudoWaiters.ContainsKey(messageId).Should().BeFalse();
        socket.SentMessages.Should().ContainSingle();
        socket.SentMessages[0].Data[0].Should().Be((byte)MessageType.PasswordRequest);
    }

    [Fact]
    public async Task RequestSudoPasswordAsync_WhenCancelled_ShouldReturnNull_AndCleanupWaiter()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-2", socket);
        var dispatcher = new CommandDispatcher(_activeSessions, CancellationToken.None);
        var messageId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var waitTask = dispatcher.RequestSudoPasswordAsync(ctx, messageId, cts.Token);
        await WaitUntilAsync(() => ctx.SudoWaiters.ContainsKey(messageId));
        cts.Cancel();

        var result = await waitTask;

        result.Should().BeNull();
        ctx.SudoWaiters.ContainsKey(messageId).Should().BeFalse();
    }

    [Fact]
    public async Task HandleKillAsync_WhenCommandExists_ShouldCancelAndSendSuccess()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-3", socket);
        var dispatcher = new CommandDispatcher(_activeSessions, CancellationToken.None);
        var commandId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        ctx.ActiveCommands[commandId] = cts;

        await dispatcher.HandleKillAsync(ctx, commandId);

        cts.IsCancellationRequested.Should().BeTrue();
        socket.SentMessages.Should().ContainSingle();
        socket.SentMessages[0].Data[0].Should().Be((byte)MessageType.CommandKillResult);
        socket.SentMessages[0].Data[^1].Should().Be(1);
    }

    [Fact]
    public async Task HandleKillAsync_WhenCommandMissing_ShouldSendFailure()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-4", socket);
        var dispatcher = new CommandDispatcher(_activeSessions, CancellationToken.None);

        await dispatcher.HandleKillAsync(ctx, Guid.NewGuid());

        socket.SentMessages.Should().ContainSingle();
        socket.SentMessages[0].Data[0].Should().Be((byte)MessageType.CommandKillResult);
        socket.SentMessages[0].Data[^1].Should().Be(0);
    }

    [Fact]
    public async Task ProcessStreamingAsync_SudoWithoutPassword_ShouldEmitKilledStreamEnd()
    {
        var socket = new MockWebSocket();
        using var ctx = new ClientContext("client-5", socket);
        var dispatcher = new CommandDispatcher(_activeSessions, CancellationToken.None);
        var messageId = Guid.NewGuid();
        var command = new WebSocketCommand
        {
            MessageId = messageId,
            Command = "sudo whoami"
        };

        var task = dispatcher.ProcessStreamingAsync(ctx, command);
        await WaitUntilAsync(() => ctx.ActiveCommands.ContainsKey(messageId));
        ctx.ActiveCommands[messageId].Cancel();
        await task;

        socket.SentMessages.Should().HaveCountGreaterThanOrEqualTo(2);
        socket.SentMessages[0].Data[0].Should().Be((byte)MessageType.PasswordRequest);

        var endPayload = socket.SentMessages.Last(m => m.Data[0] == (byte)MessageType.StreamEnd).Data;
        var end = StreamEndMessage.Deserialize(endPayload);
        end.MessageId.Should().Be(messageId);
        end.ExitCode.Should().Be(-1);
        ctx.ActiveCommands.ContainsKey(messageId).Should().BeFalse();
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 2000)
    {
        var start = Environment.TickCount64;
        while (!predicate())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Condition not reached in time.");
            await Task.Delay(10);
        }
    }

    public void Dispose() => _db.Dispose();

    private sealed class MockWebSocket : System.Net.WebSockets.WebSocket
    {
        public List<(byte[] Data, WebSocketMessageType MessageType, bool EndOfMessage)> SentMessages { get; } = [];
        private WebSocketState _state = WebSocketState.Open;

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SentMessages.Add((buffer.ToArray(), messageType, endOfMessage));
            return Task.CompletedTask;
        }

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
