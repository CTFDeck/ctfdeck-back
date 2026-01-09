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

        try
        {
            _cancellationTokenSource.Cancel();

            _httpListener.Stop();
            _isRunning = false;

            if (_listenerTask != null)
            {
                try
                {
                    await Task.WhenAny(_listenerTask, Task.Delay(2000));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error waiting for listener task: {ex.Message}");
                }
            }

            var closeTasks = new List<Task>();
            foreach (var client in _connectedClients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    closeTasks.Add(client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None));
                }
            }

            if (closeTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(closeTasks);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error closing client connections: {ex.Message}");
                }
            }

            _connectedClients.Clear();
            Console.WriteLine("WebSocket server stopped successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during server shutdown: {ex.Message}");
        }
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = Task.Run(() => HandleWebSocketConnectionAsync(context), cancellationToken);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"Error accepting connection: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Error in connection listener: {ex.Message}");
            }
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
            _clientExecutors.TryRemove(clientId, out _);

            if (webSocketContext?.WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocketContext.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing WebSocket for client {clientId}: {ex.Message}");
                }
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
        try
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
                Console.WriteLine($"Sent complete response to client {clientId} for command ID: {command.MessageId}");
                return;
            }

            // Use streaming for other commands
            var streamResult = await executor.ExecuteStreamingAsync(command.Command, async (data, isError) =>
            {
                if (webSocket.State != WebSocketState.Open) return;

                var chunkMessage = StreamChunkMessage.FromData(
                    isError ? MessageType.StreamError : MessageType.StreamOutput,
                    data,
                    command.MessageId);
                
                await SendBinaryAsync(webSocket, chunkMessage.Serialize());
            });

            // Send stream end message
            var endMessage = StreamEndMessage.FromResult(
                streamResult.ExitCode,
                executor.CurrentDirectory,
                command.MessageId);
            await SendBinaryAsync(webSocket, endMessage.Serialize());

            Console.WriteLine($"Completed streaming for client {clientId}, command ID: {command.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing binary message from client {clientId}: {ex.Message}");

            try
            {
                var errorResponse = WebSocketResponse.FromResult(
                    -1,
                    "",
                    $"Error processing command: {ex.Message}",
                    Environment.CurrentDirectory,
                    Guid.NewGuid());

                await SendBinaryAsync(webSocket, errorResponse.Serialize());
            }
            catch (Exception sendEx)
            {
                Console.WriteLine($"Failed to send error response to client {clientId}: {sendEx.Message}");
            }
        }
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