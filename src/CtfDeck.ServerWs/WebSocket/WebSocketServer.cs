using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

using CtfDeck.Abstractions.Ports.Scripts;
using CtfDeck.Abstractions.Ports.Sessions;
using CtfDeck.Abstractions.Ports.WriteUps;
using CtfDeck.Abstractions.Ports.Media;
using CtfDeck.Abstractions.Ports.Projects;
using CtfDeck.Abstractions.Ports.Aliases;

using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Models.Aliases;

using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Data.Repositories.Scripts;
using CtfDeck.Data.Repositories.WriteUps;
using CtfDeck.Data.Repositories.Media;
using CtfDeck.Data.Repositories.Projects;
using CtfDeck.Data.Repositories.Aliases;

using CtfDeck.Terminal.Features.Sessions;
using CtfDeck.Terminal.Features.Scripts;
using CtfDeck.Terminal.Features.WriteUps;
using CtfDeck.Terminal.Features.Media;
using CtfDeck.Terminal.Features.Projects;
using CtfDeck.Terminal.Features.Aliases;
using CtfDeck.Terminal.Handlers;
using CtfDeck.Terminal.Terminal.Shell;

using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Terminal.Features.Tools;

using CtfDeck.Contracts.Protocols.Tools;

namespace CtfDeck.ServerWs.WebSocket;

public class WebSocketServer
{
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<string, ClientContext> _clients = new();
    private bool _isRunning;
    private Task? _listenerTask;

    private readonly CtfDeckDbContext _dbContext;

    private readonly ActiveSessionManager _activeSessionManager;

    private readonly CommandAliasService _commandAliasService;
    private readonly Func<string, string> _aliasResolver;

    private readonly CommandDispatcher _commandDispatcher;

    private readonly List<MessageHandlerBase> _messageHandlers;

    private readonly ToolCatalogSnapshotService _toolCatalogSnapshotService;

    public WebSocketServer(string host = "localhost", int port = 8080, bool useInMemoryDb = false)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://{host}:{port}/");
        _cancellationTokenSource = new CancellationTokenSource();

        var dbPath = useInMemoryDb ? ":memory:" : null;
        _dbContext = new CtfDeckDbContext(dbPath);

        ISessionRepository sessionRepository = new SessionRepository(_dbContext);
        ICustomScriptRepository customScriptRepository = new CustomScriptRepository(_dbContext);
        IWriteUpRepository writeUpRepository = new WriteUpRepository(_dbContext);
        IMediaRepository mediaRepository = new MediaRepository(_dbContext);
        IProjectRepository projectRepository = new ProjectRepository(_dbContext);

        IToolCatalogProvider toolCatalogProvider = new ToolCatalogService();
        IToolPathResolver toolPathResolver = new ToolPathResolver();
        IToolDetector toolDetector = new ToolDetectionService(toolCatalogProvider, toolPathResolver);
        var archiveExtractor = new ArchiveExtractor();
        IToolInstaller toolInstaller = new ToolInstallationService(toolPathResolver, archiveExtractor);
        IToolInstallationCoordinator toolInstallationCoordinator =
            new ToolInstallationCoordinator(toolCatalogProvider, toolDetector, toolInstaller);
        _toolCatalogSnapshotService = new ToolCatalogSnapshotService(toolCatalogProvider, toolDetector);

        ICommandAliasRepository aliasRepository = new CommandAliasRepository(_dbContext);
        _commandAliasService = new CommandAliasService(aliasRepository);

        var os = DetectOsKind();
        var shell = DetectShellKind();
        _aliasResolver = cmd => _commandAliasService.Apply(cmd, os, shell);

        var sessionService = new SessionService(sessionRepository);
        _activeSessionManager = new ActiveSessionManager(sessionService);

        _commandDispatcher = new CommandDispatcher(_activeSessionManager, _cancellationTokenSource.Token, _aliasResolver);

        var customScriptService = new CustomScriptService(customScriptRepository);
        var writeUpService = new WriteUpService(writeUpRepository);
        var mediaService = new MediaService(mediaRepository);
        var projectService = new ProjectService(projectRepository, sessionRepository, writeUpRepository);

        _messageHandlers =
        [
            new SessionMessageHandler(sessionService, _activeSessionManager),
            new CustomScriptMessageHandler(customScriptService),
            new WriteUpMessageHandler(writeUpService),
            new MediaMessageHandler(mediaService),
            new ProjectMessageHandler(projectService),
            new ToolMessageHandler(toolInstallationCoordinator)
        ];
    }

    private static OsKind DetectOsKind()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OsKind.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OsKind.MacOs;
        return OsKind.Linux;
    }

    private static ShellKind DetectShellKind()
    {
        var resolved = ShellDetector.ResolveShellType(ShellType.Auto);

        return resolved switch
        {
            ShellType.Bash => ShellKind.Bash,
            ShellType.PowerShell => ShellKind.Pwsh,
            _ => ShellKind.Any
        };
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
            await Task.WhenAny(_listenerTask, Task.Delay(2000));
        }

        foreach (var ctx in _clients.Values)
        {
            try
            {
                if (ctx.WebSocket.State == WebSocketState.Open)
                    await ctx.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
            }
            catch (WebSocketException) { /* ignore */ }
            finally
            {
                try
                {
                    if (ctx.WebSocket.State != WebSocketState.Closed && ctx.WebSocket.State != WebSocketState.Aborted)
                    {
                        ctx.WebSocket.Abort();
                    }
                }
                catch { /* ignore */ }
            }
        }

        foreach (var ctx in _clients.Values)
            ctx.Dispose();

        _clients.Clear();
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

    private async Task SendInitialToolCatalogAsync(ClientContext ctx)
    {
        try
        {
            var snapshot = await _toolCatalogSnapshotService.GetSnapshotAsync(_cancellationTokenSource.Token);

            var payload = ToolProtocolSerializer.SerializeCatalogSnapshot(snapshot);
            Console.WriteLine($"Sending initial tool catalog snapshot to client {ctx.ClientId}, size={payload.Length} bytes");

            await ctx.Sender.SendAsync(payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send initial tool catalog to client {ctx.ClientId}: {ex.Message}");
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
                var ctx = new ClientContext(clientId, webSocket);
                _clients.TryAdd(clientId, ctx);

                Console.WriteLine($"Client {clientId} connected from {context.Request.RemoteEndPoint}");
                await SendInitialToolCatalogAsync(ctx);

                await HandleClientMessagesAsync(ctx);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling WebSocket connection for client {clientId}: {ex.Message}");
        }
        finally
        {
            _activeSessionManager.ClearClient(clientId);

            if (_clients.TryRemove(clientId, out var removed))
            {
                removed.Dispose();
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

    private async Task HandleClientMessagesAsync(ClientContext ctx)
    {
        var buffer = new byte[1024 * 64];
        var webSocket = ctx.WebSocket;
        var clientId = ctx.ClientId;

        try
        {
            while (webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var (message, messageType) = await ReceiveFullMessageAsync(webSocket, buffer, _cancellationTokenSource.Token);

                if (messageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (messageType == WebSocketMessageType.Binary)
                {
                    var protocolType = (MessageType)message.Span[0];

                    switch (protocolType)
                    {
                        case MessageType.CommandExecute:
                            {
                                var messageDataCopy = message.ToArray();
                                var command = WebSocketCommand.Deserialize(messageDataCopy);
                                var trimmedCommand = command.Command.Trim();

                                if (trimmedCommand.StartsWith("cd ") || trimmedCommand == "cd")
                                {
                                    await _commandDispatcher.ProcessCdAsync(ctx, command);
                                }
                                else
                                {
                                    _ = Task.Run(() => _commandDispatcher.ProcessStreamingAsync(ctx, command));
                                }
                                break;
                            }

                        case MessageType.CommandKill:
                            {
                                var killCommandId = new CommandKillReader(message.Span).CommandId;
                                await _commandDispatcher.HandleKillAsync(ctx, killCommandId);
                                break;
                            }

                        case MessageType.PasswordProvide:
                            {
                                var reader = new PasswordProvideReader(message.Span);
                                var pwd = reader.PasswordLength == 0 ? null : reader.GetPassword();

                                if (ctx.SudoWaiters.TryRemove(reader.MessageId, out var tcs))
                                {
                                    tcs.TrySetResult(pwd);
                                }
                                break;
                            }

                        default:
                            {
                                var handled = false;

                                foreach (var handler in _messageHandlers)
                                {
                                    if (await handler.TryHandleAsync(
                                            clientId,
                                            message,
                                            payload => ctx.Sender.SendAsync(payload, _cancellationTokenSource.Token),
                                            _cancellationTokenSource.Token))
                                    {
                                        handled = true;
                                        break;
                                    }
                                }

                                if (!handled)
                                    Console.WriteLine($"Unknown message from client {clientId}: type={message.Span[0]}, size={message.Length}");

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
    /// Reads a complete WebSocket message, accumulating frames if fragmented.
    /// For single-frame messages (vast majority), returns a slice of the shared buffer — zero allocation.
    /// For multi-frame messages (large media uploads), allocates a new array to hold the full payload.
    /// </summary>
    private static async Task<(ReadOnlyMemory<byte> Message, WebSocketMessageType Type)> ReceiveFullMessageAsync(
        System.Net.WebSockets.WebSocket webSocket, byte[] buffer, CancellationToken ct)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

        if (result.MessageType == WebSocketMessageType.Close)
            return (ReadOnlyMemory<byte>.Empty, WebSocketMessageType.Close);

        // Fast path: single frame fits in buffer (>99% of messages)
        if (result.EndOfMessage)
            return (new ReadOnlyMemory<byte>(buffer, 0, result.Count), result.MessageType);

        // Slow path: fragmented message — accumulate into a MemoryStream
        var ms = new MemoryStream(result.Count * 2);
        ms.Write(buffer, 0, result.Count);

        while (!result.EndOfMessage)
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            ms.Write(buffer, 0, result.Count);
        }

        return (ms.GetBuffer().AsMemory(0, (int)ms.Length), result.MessageType);
    }

    public bool IsRunning => _isRunning;

    public int ConnectedClientCount => _clients.Count(kvp => kvp.Value.WebSocket.State == WebSocketState.Open);
}
