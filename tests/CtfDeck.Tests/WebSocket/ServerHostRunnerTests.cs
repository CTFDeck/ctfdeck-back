using CtfDeck.WsServer;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class ServerHostRunnerTests
{
    [Fact]
    public async Task RunAsync_WhenStartupSucceeds_ShouldStartAndStopServer()
    {
        var host = new FakeServerHost();
        var logs = new List<string>();
        using var runner = new ServerHostRunner(host, loopDelay: TimeSpan.FromMilliseconds(200), log: logs.Add);

        var runTask = runner.RunAsync();
        await Task.Delay(30);
        var firstSignalHandled = runner.RequestShutdown();
        await runTask;

        firstSignalHandled.Should().BeTrue();
        host.StartCalls.Should().Be(1);
        host.StopCalls.Should().Be(1);
        logs.Should().Contain(m => m.Contains("Server shutdown initiated"));
        logs.Should().Contain(m => m.Contains("Server stopped successfully"));
    }

    [Fact]
    public async Task RunAsync_WhenStartupFails_ShouldLogError_AndStillCallStop()
    {
        var host = new FakeServerHost { StartException = new InvalidOperationException("boom") };
        var logs = new List<string>();
        using var runner = new ServerHostRunner(host, log: logs.Add);

        await runner.RunAsync();

        host.StartCalls.Should().Be(1);
        host.StopCalls.Should().Be(1);
        logs.Should().Contain(m => m.Contains("Server error: boom"));
        logs.Should().Contain(m => m.Contains("Server stopped successfully"));
    }

    [Fact]
    public async Task RunAsync_WhenStopFails_ShouldLogStopError()
    {
        var host = new FakeServerHost { StopException = new InvalidOperationException("stop failed") };
        var logs = new List<string>();
        using var runner = new ServerHostRunner(host, log: logs.Add);

        runner.RequestShutdown();
        await runner.RunAsync();

        host.StartCalls.Should().Be(1);
        host.StopCalls.Should().Be(1);
        logs.Should().Contain(m => m.Contains("Error stopping server: stop failed"));
    }

    [Fact]
    public void RequestShutdown_ShouldHandleFirstAndSecondSignalDifferently()
    {
        var host = new FakeServerHost();
        using var runner = new ServerHostRunner(host, log: _ => { });

        var first = runner.RequestShutdown();
        var second = runner.RequestShutdown();

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    private sealed class FakeServerHost : IServerHost
    {
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public Exception? StartException { get; init; }
        public Exception? StopException { get; init; }

        public Task StartAsync()
        {
            StartCalls++;
            if (StartException is not null)
            {
                throw StartException;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCalls++;
            if (StopException is not null)
            {
                throw StopException;
            }

            return Task.CompletedTask;
        }
    }
}
