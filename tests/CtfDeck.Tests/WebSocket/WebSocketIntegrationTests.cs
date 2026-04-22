using System.Net.WebSockets;
using System.Text;
using CtfDeck.WsServer.WebSocket;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class WebSocketIntegrationTests : IDisposable
{
    private readonly WebSocketServer _server;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public WebSocketIntegrationTests()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _server = new WebSocketServer("localhost", 8095, useInMemoryDb: true); // Use different port for tests
    }

    public void Dispose()
    {
        try
        {
            var stopTask = _server.StopAsync();
            _ = Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(5))).GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }

    private async Task<(byte[] Data, WebSocketMessageType Type)> ReceiveFullMessageBytesAsync(ClientWebSocket client, CancellationToken ct)
    {
        var buffer = new byte[1024 * 32];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

        if (result.MessageType == WebSocketMessageType.Close || result.EndOfMessage)
        {
            var data = new byte[result.Count];
            Array.Copy(buffer, data, result.Count);
            return (data, result.MessageType);
        }

        using var ms = new MemoryStream();
        ms.Write(buffer, 0, result.Count);

        while (!result.EndOfMessage)
        {
            result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            ms.Write(buffer, 0, result.Count);
        }

        return (ms.ToArray(), result.MessageType);
    }

    private async Task<WebSocketResponse> ReceiveCompleteResponseAsync(ClientWebSocket client, CancellationToken ct, int timeoutMs = 10000)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        var token = cts.Token;

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var messageId = Guid.Empty;

        try
        {
            while (true)
            {
                var (data, msgType) = await ReceiveFullMessageBytesAsync(client, token);
                if (msgType == WebSocketMessageType.Close) break;

                var type = (MessageType)data[0];

                if (type == MessageType.CompleteResponse)
                {
                    return WebSocketResponse.Deserialize(data);
                }
                else if (type == MessageType.StreamOutput)
                {
                    var chunk = StreamChunkMessage.Deserialize(data);
                    stdout.Append(chunk.Data);
                    messageId = chunk.MessageId;
                }
                else if (type == MessageType.StreamError)
                {
                    var chunk = StreamChunkMessage.Deserialize(data);
                    stderr.Append(chunk.Data);
                    messageId = chunk.MessageId;
                }
                else if (type == MessageType.StreamEnd)
                {
                    var endMsg = StreamEndMessage.Deserialize(data);
                    return WebSocketResponse.FromResult(
                        endMsg.ExitCode,
                        stdout.ToString(),
                        stderr.ToString(),
                        endMsg.WorkingDirectory,
                        endMsg.MessageId
                    );
                }
                else if (type == MessageType.CommandKillResult && data.Length >= 18)
                {
                    var killedMessageId = new Guid(data.AsSpan(1, 16));
                    var success = data[17] == 1;

                    return new WebSocketResponse
                    {
                        MessageId = killedMessageId,
                        ExitCode = success ? -1 : -3
                    };
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation
        }

        return new WebSocketResponse { MessageId = messageId, ExitCode = -2 }; // -2 indicates timeout/aborted in test
    }

    private static async Task SafeCloseClientAsync(ClientWebSocket client, string reason = "Test complete")
    {
        if (client.State != WebSocketState.Open && client.State != WebSocketState.CloseReceived)
            return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ignore close handshake timeout in tests.
        }
        catch (WebSocketException)
        {
            // Ignore transport-level closure failures in tests.
        }
        catch (ObjectDisposedException)
        {
            // Ignore if the client has already been disposed.
        }
    }

    [Fact]
    public async Task ClientConnection_ShouldConnectAndDisconnectSuccessfully()
    {
        // Arrange
        await _server.StartAsync();

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            // Act
            await client.ConnectAsync(serverUri, CancellationToken.None);

            // Assert
            client.State.Should().Be(WebSocketState.Open);
            _server.IsRunning.Should().BeTrue();
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }
        }
    }

    /*     [Fact]
        public async Task ClientSendingBinaryMessage_ShouldReceiveBinaryResponse()
        {
            // Arrange
            await _server.StartAsync();

            using var client = new ClientWebSocket();
            var serverUri = new Uri("ws://localhost:8095/");

            try
            {
                await client.ConnectAsync(serverUri, CancellationToken.None);

                // Create test command
                var command = "echo hello world";
                var messageId = Guid.NewGuid();
                var cmdBytes = Encoding.UTF8.GetBytes(command);
                var commandStruct = new WebSocketCommand
                {
                    Command = command,
                    CommandLength = cmdBytes.Length,
                    CommandBytes = cmdBytes,
                    MessageId = messageId
                };
                var commandData = commandStruct.Serialize();

                // Act
                await client.SendAsync(
                    new ArraySegment<byte>(commandData),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);

                var response = await ReceiveCompleteResponseAsync(client, CancellationToken.None);

                // Assert
                response.MessageId.Should().Be(messageId);
                response.ExitCode.Should().Be(0);
                response.Output.Should().Contain("hello world");
                response.Error.Should().BeEmpty();
            }
            finally
            {
                if (client.State == WebSocketState.Open)
                {
                    await SafeCloseClientAsync(client);
                }
            }
        } */

    [Fact]
    public async Task MultipleClients_ShouldConnectSimultaneously()
    {
        // Arrange
        await _server.StartAsync();

        var clients = new List<ClientWebSocket>();
        var connectionTasks = new List<Task>();

        try
        {
            // Act - Connect multiple clients
            for (int i = 0; i < 3; i++)
            {
                var client = new ClientWebSocket();
                clients.Add(client);

                var task = client.ConnectAsync(new Uri("ws://localhost:8095/"), CancellationToken.None);
                connectionTasks.Add(task);
            }

            await Task.WhenAll(connectionTasks);

            // Assert
            foreach (var client in clients)
            {
                client.State.Should().Be(WebSocketState.Open);
            }

            _server.IsRunning.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            var closeTasks = new List<Task>();
            foreach (var client in clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    closeTasks.Add(SafeCloseClientAsync(client));
                }
            }
            await Task.WhenAll(closeTasks);
        }
    }

    [Fact]
    public async Task ClientSendingMultipleCommands_ShouldReceiveResponsesForAll()
    {
        // Arrange
        await _server.StartAsync();

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            await client.ConnectAsync(serverUri, CancellationToken.None);

            var commands = new[] { "ls", "pwd", "whoami" };
            var responses = new List<WebSocketResponse>();

            // Act
            foreach (var command in commands)
            {
                var messageId = Guid.NewGuid();
                var cmdBytes = Encoding.UTF8.GetBytes(command);
                var commandStruct = new WebSocketCommand
                {
                    Command = command,
                    CommandLength = cmdBytes.Length,
                    CommandBytes = cmdBytes,
                    MessageId = messageId
                };
                var commandData = commandStruct.Serialize();

                await client.SendAsync(
                    new ArraySegment<byte>(commandData),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);

                var response = await ReceiveCompleteResponseAsync(client, CancellationToken.None);
                responses.Add(response);
            }

            // Assert
            responses.Count.Should().Be(commands.Length);
            foreach (var response in responses)
            {
                // Commands are executed for real, so we just verify they complete successfully
                response.ExitCode.Should().BeGreaterThanOrEqualTo(0);
            }
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }
        }
    }

    [Fact]
    public async Task ClientSendingTextMessage_ShouldBeIgnoredOrError()
    {
        // Arrange
        await _server.StartAsync();

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            await client.ConnectAsync(serverUri, CancellationToken.None);

            var textMessage = Encoding.UTF8.GetBytes("Hello world");

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(textMessage),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            // Wait a bit to ensure server processes (or ignores) the message
            await Task.Delay(100);

            // Assert - Server should still be running and connection should be open
            client.State.Should().Be(WebSocketState.Open);
            _server.IsRunning.Should().BeTrue();
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }
        }
    }

    [Fact]
    public async Task ClientClosingConnection_ShouldUpdateServerClientCount()
    {
        // Arrange
        await _server.StartAsync();

        // Initial state
        _server.ConnectedClientCount.Should().Be(0);

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            // Act - Connect
            await client.ConnectAsync(serverUri, CancellationToken.None);

            // Give server time to register the connection
            await Task.Delay(100);

            // Assert after connection
            _server.ConnectedClientCount.Should().BeGreaterThan(0);
        }
        finally
        {
            // Act - Disconnect
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }

            // Give server time to process disconnection
            await Task.Delay(100);

            // Assert after disconnection
            _server.ConnectedClientCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task EmptyCommand_ShouldReturnValidResponse()
    {
        // Arrange
        await _server.StartAsync();

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            await client.ConnectAsync(serverUri, CancellationToken.None);

            // Create empty command
            var emptyCommand = "";
            var messageId = Guid.NewGuid();
            var cmdBytes = Encoding.UTF8.GetBytes(emptyCommand);
            var commandStruct = new WebSocketCommand
            {
                Command = emptyCommand,
                CommandLength = cmdBytes.Length,
                CommandBytes = cmdBytes,
                MessageId = messageId
            };
            var commandData = commandStruct.Serialize();

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(commandData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);

            var response = await ReceiveCompleteResponseAsync(client, CancellationToken.None);

            // Assert
            response.MessageId.Should().Be(messageId);
            response.ExitCode.Should().Be(0);
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }
        }
    }

    [Fact]
    public async Task LargeCommand_ShouldProcessCorrectly()
    {
        // Arrange
        await _server.StartAsync();

        using var client = new ClientWebSocket();
        var serverUri = new Uri("ws://localhost:8095/");

        try
        {
            await client.ConnectAsync(serverUri, CancellationToken.None);

            // Create a valid command with large output
            var largeCommand = "echo " + new string('a', 500);
            var messageId = Guid.NewGuid();
            var cmdBytes = Encoding.UTF8.GetBytes(largeCommand);
            var commandStruct = new WebSocketCommand
            {
                Command = largeCommand,
                CommandLength = cmdBytes.Length,
                CommandBytes = cmdBytes,
                MessageId = messageId
            };
            var commandData = commandStruct.Serialize();

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(commandData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);

            var response = await ReceiveCompleteResponseAsync(client, CancellationToken.None);

            // Assert
            response.MessageId.Should().Be(messageId);
            response.ExitCode.Should().Be(0);
            response.Output.Should().Contain(new string('a', 500));
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await SafeCloseClientAsync(client);
            }
        }
    }

    [Fact]
    public async Task StopAsync_WithActiveClients_ShouldCloseClientsCorrectly()
    {
        // Arrange
        await _server.StartAsync();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://localhost:8095/"), CancellationToken.None);

        // Act
        await _server.StopAsync();

        // Drain any buffered server messages (e.g. initial ToolCatalogSnapshot) until the
        // client observes the close/abort — a single receive is fragile because the first
        // frame may be the catalog, not the close frame.
        using var drainCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!drainCts.IsCancellationRequested &&
               client.State != WebSocketState.CloseReceived &&
               client.State != WebSocketState.CloseSent &&
               client.State != WebSocketState.Closed &&
               client.State != WebSocketState.Aborted)
        {
            try
            {
                await ReceiveFullMessageBytesAsync(client, drainCts.Token);
            }
            catch (WebSocketException) { break; }
            catch (OperationCanceledException) { break; }
        }

        // Assert
        client.State.Should().Match(s => s == WebSocketState.CloseReceived || s == WebSocketState.CloseSent || s == WebSocketState.Closed || s == WebSocketState.Aborted);
        _server.ConnectedClientCount.Should().Be(0);
    }

    [Fact]
    public async Task CdCommand_ShouldChangeWorkingDirectory()
    {
        // Arrange
        await _server.StartAsync();
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://localhost:8095/"), CancellationToken.None);

        var tempDir = Path.GetTempPath();

        // Act
        var messageId = Guid.NewGuid();
        var commandStr = $"cd {tempDir}";
        var cmdBytes = Encoding.UTF8.GetBytes(commandStr);
        var commandStruct = new WebSocketCommand
        {
            Command = commandStr,
            CommandLength = cmdBytes.Length,
            CommandBytes = cmdBytes,
            MessageId = messageId
        };

        await client.SendAsync(new ArraySegment<byte>(commandStruct.Serialize()), WebSocketMessageType.Binary, true, CancellationToken.None);

        var response = await ReceiveCompleteResponseAsync(client, CancellationToken.None);

        // Assert
        response.ExitCode.Should().Be(0);
        response.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar).Should().Be(tempDir.TrimEnd(Path.DirectorySeparatorChar));

        await SafeCloseClientAsync(client);
    }

    [Fact]
    public async Task NonWebSocketRequest_ShouldBeClosed()
    {
        // Arrange
        await _server.StartAsync();
        using var httpClient = new HttpClient();

        // Act
        // Send a regular HTTP GET request instead of a WebSocket upgrade
        var response = await httpClient.GetAsync("http://localhost:8095/");

        // Assert
        // Since we call context.Response.Close() without setting status code, it might default to 200 or just terminate the connection.
        // The important thing is that it finishes and doesn't hang.
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

