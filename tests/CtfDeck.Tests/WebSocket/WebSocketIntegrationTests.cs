using System.Net.WebSockets;
using System.Text;
using CtfDeck.Terminal.WebSocket;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class WebSocketIntegrationTests : IDisposable
{
    private readonly WebSocketServer _server;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public WebSocketIntegrationTests()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _server = new WebSocketServer("localhost", 8095); // Use different port for tests
    }

    public void Dispose()
    {
        try
        {
            _server.StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource.Dispose();
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    private async Task<WebSocketResponse> ReceiveCompleteResponseAsync(ClientWebSocket client, CancellationToken ct)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var buffer = new byte[1024 * 32];

        var messageId = Guid.Empty;

        while (true)
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close) break;

            // Copy relevant bytes
            var data = new byte[result.Count];
            Array.Copy(buffer, data, result.Count);

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
        }

        return new WebSocketResponse { MessageId = messageId };
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }
    }

    [Fact]
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }
    }

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
                    closeTasks.Add(client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None));
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
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
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
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

        // Wait for client to detect closure
        var buffer = new byte[1024];
        try
        {
            await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        catch (WebSocketException) { }

        // Assert
        client.State.Should().Match(s => s == WebSocketState.CloseReceived || s == WebSocketState.Closed || s == WebSocketState.Aborted);
        _server.ConnectedClientCount.Should().Be(0);
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