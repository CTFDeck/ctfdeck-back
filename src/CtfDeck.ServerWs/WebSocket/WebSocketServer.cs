using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using CtfDeck.Abstractions.Ports.Scripts;
using CtfDeck.Abstractions.Ports.Sessions;
using WsClient = System.Net.WebSockets.WebSocket;

using CtfDeck.Contracts.Transport;

using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Data.Repositories.Scripts;

using CtfDeck.Terminal.Features.Sessions;
using CtfDeck.Terminal.Features.Scripts;
using CtfDeck.Terminal.Handlers;
using CtfDeck.Terminal.Terminal;

namespace CtfDeck.ServerWs.WebSocket;

public class WebSocketServer
{
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<string, WsClient> _connectedClients;
    private readonly ConcurrentDictionary<string, TerminalExecutor> _clientExecutors;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, TaskCompletionSource<string?>>> _clientSudoWaiters = new();
    private bool _isRunning;
    private Task? _listenerTask;

    // Parallel execution: track active commands per client for cancellation
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, CancellationTokenSource>> _clientActiveCommands = new();

    // Send locks per client: WebSocket.SendAsync is NOT thread-safe for concurrent calls
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _clientSendLocks = new();

    // Data
    private readonly CtfDeckDbContext _dbContext;

    // Session management
    private readonly SessionService _sessionService;
    private readonly ActiveSessionManager _activeSessionManager;
    private readonly SessionMessageHandler _sessionMessageHandler;

    // Custom script management
    private readonly CustomScriptService _customScriptService;
    private readonly CustomScriptMessageHandler _customScriptMessageHandler;

    public WebSocketServer(string host = "localhost", int port = 8080, bool useInMemoryDb = false)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://{host}:{port}/");
        _cancellationTokenSource = new CancellationTokenSource();
        _connectedClients = new ConcurrentDictionary<string, WsClient>();
        _clientExecutors = new ConcurrentDictionary<string, TerminalExecutor>();

        // Initialize DB (use in-memory for tests)
        var dbPath = useInMemoryDb ? ":memory:" : null;
        _dbContext = new CtfDeckDbContext(dbPath);

        // Repositories (as ports)
        ISessionRepository sessionRepository = new SessionRepository(_dbContext);
        ICustomScriptRepository customScriptRepository = new CustomScriptRepository(_dbContext);

        // Services / handlers
        _sessionService = new SessionService(sessionRepository);
        _activeSessionManager = new ActiveSessionManager(_sessionService);
        _sessionMessageHandler = new SessionMessageHandler(_sessionService, _activeSessionManager);

        _customScriptService = new CustomScriptService(customScriptRepository);
        _customScriptMessageHandler = new CustomScriptMessageHandler(_customScriptService);
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            Console.WriteLine("WebSocket server is already running.");
            return;
        }

        try
        {
            _httpListener.Start();
            _isRunning = true;
            Console.WriteLine($"WebSocket server started on {_httpListener.Prefixes.First()}");

            _listenerTask = ListenForConnectionsAsync(_cancellationTokenSource.Token);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start WebSocket server: {ex.Message}");
            _isRunning = false;
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _cancellationTokenSource.Cancel();
        _httpListener.Stop();
        _isRunning = false;

        if (_listenerTask != null)
        {
            // Wait for listener to complete gracefully
            await Task.WhenAny(_listenerTask, Task.Delay(2000));
        }

        var closeTasks = _connectedClients.Values
            .Where(client => client.State == WebSocketState.Open)
            .Select(client => client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None))
            .ToList();

        if (closeTasks.Count > 0)
        {
            await Task.WhenAll(closeTasks);
        }

        _connectedClients.Clear();
        _dbContext.Dispose();
        Console.WriteLine("WebSocket server stopped successfully.");
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            var context = await AcceptContextAsync(cancellationToken);
            if (context == null) break;

            if (context.Request.IsWebSocketRequest)
            {
                _ = Task.Run(() => HandleWebSocketConnectionAsync(context), cancellationToken);
            }
            else
            {
                context.Response.Close();
            }
        }
    }

    private async Task<HttpListenerContext?> AcceptContextAsync(CancellationToken ct)
    {
        try
        {
            return await _httpListener.GetContextAsync();
        }
        catch (HttpListenerException) when (ct.IsCancellationRequested || !_isRunning)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        WebSocketContext? webSocketContext = null;
        var clientId = Guid.NewGuid().ToString();

        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = webSocketContext.WebSocket;

            if (webSocket.State == WebSocketState.Open)
            {
                _connectedClients.TryAdd(clientId, webSocket);
                _clientExecutors.TryAdd(clientId, new TerminalExecutor());
                _clientActiveCommands.TryAdd(clientId, new ConcurrentDictionary<Guid, CancellationTokenSource>());
                _clientSendLocks.TryAdd(clientId, new SemaphoreSlim(1, 1));
                _clientSudoWaiters.TryAdd(clientId, new ConcurrentDictionary<Guid, TaskCompletionSource<string?>>());

                Console.WriteLine($"Client {clientId} connected from {context.Request.RemoteEndPoint}");

                await HandleClientMessagesAsync(webSocket, clientId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling WebSocket connection for client {clientId}: {ex.Message}");
        }
        finally
        {
            // Cancel all active commands for this client
            if (_clientActiveCommands.TryRemove(clientId, out var activeCommands))
            {
                foreach (var cts in activeCommands.Values)
                {
                    try { cts.Cancel(); cts.Dispose(); }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }

            _connectedClients.TryRemove(clientId, out _);
            _activeSessionManager.ClearClient(clientId);
            _clientSudoWaiters.TryRemove(clientId, out _);

            // Dispose the executor to clean up the persistent shell process
            if (_clientExecutors.TryRemove(clientId, out var executor))
            {
                executor.Dispose();
            }

            if (_clientSendLocks.TryRemove(clientId, out var sendLock))
            {
                sendLock.Dispose();
            }

            if (webSocketContext?.WebSocket.State == WebSocketState.Open)
            {
                await webSocketContext.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }

            Console.WriteLine($"Client {clientId} disconnected");
        }
    }

    private async Task HandleClientMessagesAsync(WsClient webSocket, string clientId)
    {
        var buffer = new byte[1024 * 64]; // 64KB buffer for session data
        var sendLock = _clientSendLocks[clientId];

        try
        {
            while (webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var messageType = (MessageType)buffer[0];

                    switch (messageType)
                    {
                        case MessageType.CommandExecute:
                            {
                                // Copy buffer before dispatching (buffer is reused by receive loop)
                                var messageDataCopy = buffer.AsSpan(0, result.Count).ToArray();
                                var command = WebSocketCommand.Deserialize(messageDataCopy);
                                var trimmedCommand = command.Command.Trim();

                                if (trimmedCommand.StartsWith("cd ") || trimmedCommand == "cd")
                                {
                                    // cd commands must be sequential — they modify shared cwd state
                                    await ProcessCdCommand(webSocket, command, clientId, sendLock);
                                }
                                else
                                {
                                    // Fire-and-forget for streaming commands — enables parallel execution
                                    _ = Task.Run(() => ProcessStreamingCommandAsync(webSocket, command, clientId, sendLock));
                                }
                                break;
                            }

                        case MessageType.CommandKill:
                            {
                                var killCommandId = new CommandKillReader(buffer.AsSpan(0, result.Count)).CommandId;
                                await HandleCommandKill(webSocket, killCommandId, clientId, sendLock);
                                break;
                            }

                        case MessageType.PasswordProvide:
                            {
                                var reader = new PasswordProvideReader(buffer.AsSpan(0, result.Count));
                                var pwd = reader.PasswordLength == 0 ? null : reader.GetPassword();

                                if (_clientSudoWaiters.TryGetValue(clientId, out var waiters) &&
                                    waiters.TryRemove(reader.MessageId, out var tcs))
                                {
                                    tcs.TrySetResult(pwd);
                                }
                                break;
                            }

                        default:
                            {
                                // Session, custom script, and other typed messages
                                var messageData = buffer.AsMemory(0, result.Count);

                                if (await _sessionMessageHandler.TryHandleAsync(clientId, messageData, webSocket, _cancellationTokenSource.Token, sendLock))
                                    break;

                                if (await _customScriptMessageHandler.TryHandleAsync(clientId, messageData, webSocket, _cancellationTokenSource.Token, sendLock))
                                    break;

                                Console.WriteLine($"Unknown message from client {clientId}: type={buffer[0]}, size={result.Count}");
                                break;
                            }
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket error for client {clientId}: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling messages for client {clientId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Process cd command sequentially (modifies shared cwd state)
    /// </summary>
    private async Task ProcessCdCommand(WsClient webSocket, WebSocketCommand command, string clientId, SemaphoreSlim sendLock)
    {
        Console.WriteLine($"Client {clientId} sent command: '{command.Command}' (ID: {command.MessageId})");

        if (!_clientExecutors.TryGetValue(clientId, out var executor))
        {
            var errorResponse = WebSocketResponse.FromResult(
                -1, "", "No terminal executor found for this client",
                Environment.CurrentDirectory, command.MessageId);

            await SendBinaryAsync(webSocket, errorResponse.Serialize(), sendLock);
            return;
        }

        var result = await executor.ExecuteAsync(command.Command);

        var response = WebSocketResponse.FromResult(
            result.ExitCode, result.Output, result.Error,
            result.WorkingDirectory, command.MessageId);

        await SendBinaryAsync(webSocket, response.Serialize(), sendLock);

        _activeSessionManager.RecordCommand(
            clientId, command.Command, result.Output + result.Error,
            result.ExitCode, result.WorkingDirectory);
    }

    /// <summary>
    /// Process streaming command in background (enables parallel execution)
    /// </summary>
    private async Task ProcessStreamingCommandAsync(WsClient webSocket, WebSocketCommand command, string clientId, SemaphoreSlim sendLock)
    {
        Console.WriteLine($"Client {clientId} sent command: '{command.Command}' (ID: {command.MessageId})");

        if (!_clientExecutors.TryGetValue(clientId, out var executor))
        {
            var errorResponse = WebSocketResponse.FromResult(
                -1, "", "No terminal executor found for this client",
                Environment.CurrentDirectory, command.MessageId);

            await SendBinaryAsync(webSocket, errorResponse.Serialize(), sendLock);
            return;
        }

        // Create a linked CancellationTokenSource for this command
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);

        // Register in active commands for kill support
        var activeCommands = _clientActiveCommands.GetOrAdd(clientId, _ => new ConcurrentDictionary<Guid, CancellationTokenSource>());
        activeCommands.TryAdd(command.MessageId, cts);

        try
        {
            var trimmed = command.Command.TrimStart();

            // actually keep the password and pass it to executor
            string? sudoPassword = null;

            // Request sudo password if needed
            if (trimmed.StartsWith("sudo ", StringComparison.Ordinal) || trimmed == "sudo")
            {
                sudoPassword = await RequestSudoPasswordAsync(webSocket, clientId, command.MessageId, sendLock, cts.Token);
                if (string.IsNullOrEmpty(sudoPassword))
                {
                    // User cancelled or timeout - treat as cancelled operation
                    throw new OperationCanceledException("Sudo password not provided");
                }
            }

            await using var batcher = new OutputBatcher(webSocket, command.MessageId, sendLock);

            // Pass sudoPassword here (TerminalExecutor injects -S and stdin)
            var streamResult = await executor.ExecuteStreamingAsync(
                command.Command,
                (data, isError) => batcher.EnqueueAsync(data, isError).AsTask(),
                cts.Token,
                sudoPassword);

            var accumulatedOutput = await batcher.CompleteAsync(streamResult.ExitCode, executor.CurrentDirectory);

            _activeSessionManager.RecordCommand(
                clientId, command.Command, accumulatedOutput,
                streamResult.ExitCode, executor.CurrentDirectory);
        }
        catch (OperationCanceledException)
        {
            // Command was killed or sudo password cancelled — send StreamEnd with exitCode -1
            try
            {
                var endData = BinaryProtocolSerializer.SerializeStreamEnd(command.MessageId, -1, executor.CurrentDirectory);
                await SendBinaryAsync(webSocket, endData, sendLock);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending kill stream end for client {clientId}: {ex.Message}");
            }

            _activeSessionManager.RecordCommand(
                clientId, command.Command, "[killed]", -1, executor.CurrentDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command for client {clientId}: {ex.Message}");
        }
        finally
        {
            activeCommands.TryRemove(command.MessageId, out _);
        }
    }

    /// <summary>
    /// Handle CommandKill message — cancel a running command by its messageId
    /// </summary>
    private async Task HandleCommandKill(WsClient webSocket, Guid commandId, string clientId, SemaphoreSlim sendLock)
    {
        var success = false;

        if (_clientActiveCommands.TryGetValue(clientId, out var activeCommands)
            && activeCommands.TryGetValue(commandId, out var cts))
        {
            try
            {
                cts.Cancel();
                success = true;
            }
            catch (ObjectDisposedException)
            {
                // Command already finished and disposed its CTS
            }
        }

        Console.WriteLine($"[KILL] {commandId} → {(success ? "cancelled" : "not found")}");

        var result = BinaryProtocolSerializer.SerializeCommandKillResult(commandId, success);
        await SendBinaryAsync(webSocket, result, sendLock);
    }

    private async Task SendBinaryAsync(WsClient webSocket, byte[] data, SemaphoreSlim sendLock)
    {
        if (webSocket.State != WebSocketState.Open) return;

        await sendLock.WaitAsync(CancellationToken.None);
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);
            }
        }
        finally
        {
            sendLock.Release();
        }
    }

    /// <summary>
    /// Request sudo password from client and wait for response
    /// </summary>
    private async Task<string?> RequestSudoPasswordAsync(
        WsClient webSocket,
        string clientId,
        Guid messageId,
        SemaphoreSlim sendLock,
        CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var waiters = _clientSudoWaiters.GetOrAdd(clientId, _ => new ConcurrentDictionary<Guid, TaskCompletionSource<string?>>());
        waiters[messageId] = tcs;

        var req = BinaryProtocolSerializer.SerializePasswordRequest(messageId, "Sudo password required");
        await SendBinaryAsync(webSocket, req, sendLock);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, linked.Token));
        }
        catch
        {
            // ignored
        }

        waiters.TryRemove(messageId, out _);
        return tcs.Task.IsCompleted ? tcs.Task.Result : null;
    }

    public bool IsRunning => _isRunning;

    public int ConnectedClientCount => _connectedClients.Count(kvp => kvp.Value.State == WebSocketState.Open);
}
