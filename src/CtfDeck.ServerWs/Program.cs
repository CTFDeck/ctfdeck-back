using CtfDeck.ServerWs.WebSocket;

var server = new WebSocketServer("localhost", 42712);

var cts = new CancellationTokenSource();
bool isShuttingDown = false;

Console.CancelKeyPress += (sender, e) =>
{
    if (!isShuttingDown)
    {
        isShuttingDown = true;
        e.Cancel = true;
        Console.WriteLine("\nShutdown signal received. Stopping server...");
        cts.Cancel();
    }
    else
    {
        e.Cancel = false;
    }
};

try
{
    await server.StartAsync();

    Console.WriteLine("Press Ctrl+C to stop the server...");

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(1000, cts.Token);
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Server shutdown initiated...");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}
finally
{
    try
    {
        await server.StopAsync();
        Console.WriteLine("Server stopped successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error stopping server: {ex.Message}");
    }
}
