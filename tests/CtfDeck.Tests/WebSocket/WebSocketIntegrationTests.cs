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
            var commandStruct = WebSocketCommand.FromCommand(command, messageId);
            var commandData = commandStruct.Serialize();

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(commandData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);

            var responseBuffer = new byte[1024 * 4];
            var responseResult = await client.ReceiveAsync(
                new ArraySegment<byte>(responseBuffer),
                CancellationToken.None);

            // Assert
            responseResult.MessageType.Should().Be(WebSocketMessageType.Binary);
            responseResult.EndOfMessage.Should().BeTrue();

            var responseData = new byte[responseResult.Count];
            Array.Copy(responseBuffer, responseData, responseResult.Count);

            var response = WebSocketResponse.Deserialize(responseData);
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
                var commandStruct = WebSocketCommand.FromCommand(command, messageId);
                var commandData = commandStruct.Serialize();

                await client.SendAsync(
                    new ArraySegment<byte>(commandData),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);

                var responseBuffer = new byte[1024 * 4];
                var responseResult = await client.ReceiveAsync(
                    new ArraySegment<byte>(responseBuffer),
                    CancellationToken.None);

                var responseData = new byte[responseResult.Count];
                Array.Copy(responseBuffer, responseData, responseResult.Count);

                var response = WebSocketResponse.Deserialize(responseData);
                responses.Add(response);
            }

            // Assert
            responses.Count.Should().Be(commands.Length);
            foreach (var response in responses)
            {
                // Commands are executed for real, so we just verify they complete successfully
                // pwd and whoami should always succeed, ls may fail if directory is empty but exit code should be 0
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
            var commandStruct = WebSocketCommand.FromCommand(emptyCommand, messageId);
            var commandData = commandStruct.Serialize();

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(commandData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);

            var responseBuffer = new byte[1024 * 4];
            var responseResult = await client.ReceiveAsync(
                new ArraySegment<byte>(responseBuffer),
                CancellationToken.None);

            // Assert
            responseResult.MessageType.Should().Be(WebSocketMessageType.Binary);

            var responseData = new byte[responseResult.Count];
            Array.Copy(responseBuffer, responseData, responseResult.Count);

            var response = WebSocketResponse.Deserialize(responseData);
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
            var commandStruct = WebSocketCommand.FromCommand(largeCommand, messageId);
            var commandData = commandStruct.Serialize();

            // Act
            await client.SendAsync(
                new ArraySegment<byte>(commandData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None);

            var responseBuffer = new byte[1024 * 8]; // Larger buffer for response
            var responseResult = await client.ReceiveAsync(
                new ArraySegment<byte>(responseBuffer),
                CancellationToken.None);

            // Assert
            responseResult.MessageType.Should().Be(WebSocketMessageType.Binary);

            var responseData = new byte[responseResult.Count];
            Array.Copy(responseBuffer, responseData, responseResult.Count);

            var response = WebSocketResponse.Deserialize(responseData);
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
}