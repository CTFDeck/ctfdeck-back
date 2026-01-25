using System.Net;
using System.Net.WebSockets;
using CtfDeck.Terminal.WebSocket;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class WebSocketServerTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var server = new WebSocketServer();

        // Assert
        server.IsRunning.Should().BeFalse();
        server.ConnectedClientCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithCustomHostAndPort_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var server = new WebSocketServer("127.0.0.1", 9999);

        // Assert
        server.IsRunning.Should().BeFalse();
        server.ConnectedClientCount.Should().Be(0);
    }

    [Fact]
    public void IsRunning_ShouldReflectServerState()
    {
        // Arrange
        var server = new WebSocketServer();

        // Assert - Initial state
        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void ConnectedClientCount_ShouldReturnZeroInitially()
    {
        // Arrange
        var server = new WebSocketServer();

        // Act & Assert
        server.ConnectedClientCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8081); // Use different port to avoid conflicts

        try
        {
            // Act
            await server.StartAsync();

            // Assert
            server.IsRunning.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            try
            {
                await server.StopAsync();
            }
            catch
            {
                // Ignore cleanup errors for this test
            }
        }
    }

    [Fact]
    public async Task StopAsync_ShouldStopCleanly()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8082);
        await server.StartAsync();

        // Act
        await server.StopAsync();

        // Assert
        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldReturn()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8083);

        // Act & Assert - Should not throw
        await server.StopAsync();
        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ShouldReturn()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8084);
        await server.StartAsync();

        try
        {
            // Act
            await server.StartAsync();

            // Assert
            server.IsRunning.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task StartStopCycle_ShouldMaintainCorrectState()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8085);

        // Act & Assert
        server.IsRunning.Should().BeFalse();

        await server.StartAsync();
        server.IsRunning.Should().BeTrue();

        await server.StopAsync();
        server.IsRunning.Should().BeFalse();

        await server.StartAsync();
        server.IsRunning.Should().BeTrue();

        await server.StopAsync();
        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleStops_WhenNotRunning_ShouldNotThrow()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8086);

        // Act & Assert
        await server.StopAsync();
        await server.StopAsync();
        await server.StopAsync();

        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartStopWithDifferentPorts_ShouldWorkCorrectly()
    {
        // Arrange
        var ports = new[] { 8087, 8088, 8089 };

        foreach (var port in ports)
        {
            var server = new WebSocketServer("localhost", port);

            try
            {
                // Act
                await server.StartAsync();

                // Assert
                server.IsRunning.Should().BeTrue();

                // Cleanup
                await server.StopAsync();
                server.IsRunning.Should().BeFalse();
            }
            catch (Exception ex) when (ex.Message.Contains("Only one usage of each socket address"))
            {
                // Skip if port is in use (rare in test environment)
                Console.WriteLine($"Port {port} is in use, skipping test");
            }
        }
    }

    [Fact]
    public async Task ServerProperties_AfterStartStop_ShouldMaintainConsistency()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8090);

        try
        {
            // Initial state
            server.IsRunning.Should().BeFalse();
            server.ConnectedClientCount.Should().Be(0);

            // After start
            await server.StartAsync();
            server.IsRunning.Should().BeTrue();
            server.ConnectedClientCount.Should().Be(0); // No clients connected yet

            // After stop
            await server.StopAsync();
            server.IsRunning.Should().BeFalse();
            server.ConnectedClientCount.Should().Be(0);
        }
        finally
        {
            await server.StopAsync(); // Ensure cleanup
        }
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("0.0.0.0")]
    public void Constructor_WithDifferentHosts_ShouldInitializeCorrectly(string host)
    {
        // Arrange & Act
        var server = new WebSocketServer(host, 8091);

        // Assert
        server.IsRunning.Should().BeFalse();
        server.ConnectedClientCount.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void Constructor_WithValidPorts_ShouldInitializeCorrectly(int port)
    {
        // Arrange & Act
        var server = new WebSocketServer("localhost", port);

        // Assert
        server.IsRunning.Should().BeFalse();
        server.ConnectedClientCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WithInvalidPort_ShouldThrowException()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 99999); // Invalid port

        // Act & Assert
        await Assert.ThrowsAsync<HttpListenerException>(() => server.StartAsync());
        server.IsRunning.Should().BeFalse();
    }

    /* [Fact]
    public async Task StartStopStressTest_ShouldMaintainStability()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8092);
        const int iterations = 5;

        try
        {
            for (int i = 0; i < iterations; i++)
            {
                // Act
                await server.StartAsync();
                server.IsRunning.Should().BeTrue();

                await server.StopAsync();
                server.IsRunning.Should().BeFalse();
            }
        }
        finally
        {
            await server.StopAsync(); // Ensure cleanup
        }
    } */

    [Fact]
    public async Task ConnectedClientCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var server = new WebSocketServer("localhost", 8093);
        await server.StartAsync();

        try
        {
            // Act & Assert
            // Initially no clients
            server.ConnectedClientCount.Should().Be(0);
        }
        finally
        {
            await server.StopAsync();
        }
    }
}