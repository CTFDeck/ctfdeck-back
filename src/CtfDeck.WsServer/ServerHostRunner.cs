namespace CtfDeck.WsServer;

public interface IServerHost
{
    Task StartAsync();
    Task StopAsync();
}

public sealed class ServerHostRunner : IDisposable
{
    private static readonly TimeSpan DefaultLoopDelay = TimeSpan.FromSeconds(1);

    private readonly IServerHost _server;
    private readonly TimeSpan _loopDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _shutdownTokenSource = new();

    private int _shutdownRequested;

    public ServerHostRunner(
        IServerHost server,
        TimeSpan? loopDelay = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Action<string>? log = null)
    {
        _server = server;
        _loopDelay = loopDelay ?? DefaultLoopDelay;
        _delayAsync = delayAsync ?? ((delay, ct) => Task.Delay(delay, ct));
        _log = log ?? Console.WriteLine;
    }

    public bool RequestShutdown()
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
        {
            return false;
        }

        _log("\nShutdown signal received. Stopping server...");
        _shutdownTokenSource.Cancel();
        return true;
    }

    public async Task RunAsync()
    {
        try
        {
            await _server.StartAsync();
            _log("Press Ctrl+C to stop the server...");

            try
            {
                while (!_shutdownTokenSource.Token.IsCancellationRequested)
                {
                    await _delayAsync(_loopDelay, _shutdownTokenSource.Token);
                }
            }
            catch (TaskCanceledException) when (_shutdownTokenSource.Token.IsCancellationRequested)
            {
                // Expected when shutdown is requested while waiting in delay.
            }

            if (_shutdownTokenSource.Token.IsCancellationRequested)
            {
                _log("Server shutdown initiated...");
            }
        }
        catch (Exception ex)
        {
            _log($"Server error: {ex.Message}");
        }
        finally
        {
            try
            {
                await _server.StopAsync();
                _log("Server stopped successfully.");
            }
            catch (Exception ex)
            {
                _log($"Error stopping server: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _shutdownTokenSource.Dispose();
    }
}
