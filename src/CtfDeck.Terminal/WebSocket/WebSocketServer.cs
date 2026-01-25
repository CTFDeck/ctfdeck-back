using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using CtfDeck.Terminal.Terminal;
using WsClient = System.Net.WebSockets.WebSocket;

namespace CtfDeck.Terminal.WebSocket;

public class WebSocketServer
{
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<string, WsClient> _connectedClients;
    private readonly ConcurrentDictionary<string, TerminalExecutor> _clientExecutors;
    private bool _isRunning;
    private Task? _listenerTask;

    public WebSocketServer(string host = "localhost", int port = 8080)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://{host}:{port}/");
        _cancellationTokenSource = new CancellationTokenSource();
        _connectedClients = new ConcurrentDictionary<string, WsClient>();
        _clientExecutors = new ConcurrentDictionary<string, TerminalExecutor>();
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
        string clientId = Guid.NewGuid().ToString();

        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            var webSocket = webSocketContext.WebSocket;

            if (webSocket.State == WebSocketState.Open)
            {
                _connectedClients.TryAdd(clientId, webSocket);
                _clientExecutors.TryAdd(clientId, new TerminalExecutor());
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
            _connectedClients.TryRemove(clientId, out _);

            // Dispose the executor to clean up the persistent shell process
            if (_clientExecutors.TryRemove(clientId, out var executor))
            {
                executor.Dispose();
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
        var buffer = new byte[1024 * 4];

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
                    await ProcessBinaryMessageStreaming(webSocket, buffer, result.Count, clientId);
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
    /// Process command with streaming output
    /// </summary>
    private async Task ProcessBinaryMessageStreaming(WsClient webSocket, byte[] buffer, int messageLength, string clientId)
    {
        var messageData = new byte[messageLength];
        Array.Copy(buffer, messageData, messageLength);

        var command = WebSocketCommand.Deserialize(messageData);

        Console.WriteLine($"Client {clientId} sent command: '{command.Command}' (ID: {command.MessageId})");

        if (!_clientExecutors.TryGetValue(clientId, out var executor))
        {
            var errorResponse = WebSocketResponse.FromResult(
                -1,
                "",
                "No terminal executor found for this client",
                Environment.CurrentDirectory,
                command.MessageId);
            await SendBinaryAsync(webSocket, errorResponse.Serialize());
            return;
        }

        // Check if this is a simple command that doesn't need streaming (cd, pwd, etc.)
        var trimmedCommand = command.Command.Trim();
        if (trimmedCommand.StartsWith("cd ") || trimmedCommand == "cd")
        {
            // Use non-streaming for cd
            var result = await executor.ExecuteAsync(command.Command);
            var response = WebSocketResponse.FromResult(
                result.ExitCode,
                result.Output,
                result.Error,
                result.WorkingDirectory,
                command.MessageId);
            await SendBinaryAsync(webSocket, response.Serialize());
            return;
        }

        // Use batched streaming for other commands (high-performance)
        await using var batcher = new OutputBatcher(webSocket, command.MessageId);

        var streamResult = await executor.ExecuteStreamingAsync(
            command.Command,
            (data, isError) => batcher.EnqueueAsync(data, isError).AsTask());

        // Complete batching and send stream end
        await batcher.CompleteAsync(streamResult.ExitCode, executor.CurrentDirectory);
    }

    private async Task SendBinaryAsync(WsClient webSocket, byte[] data)
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

    public bool IsRunning => _isRunning;

    public int ConnectedClientCount => _connectedClients.Count(kvp => kvp.Value.State == WebSocketState.Open);
}