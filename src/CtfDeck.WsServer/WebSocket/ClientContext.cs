using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using CtfDeck.Terminal.Terminal;

namespace CtfDeck.WsServer.WebSocket;

public sealed class ClientContext : IDisposable
{
    public string ClientId { get; }
    public System.Net.WebSockets.WebSocket WebSocket { get; }
    public TerminalExecutor Executor { get; }
    public SemaphoreSlim SendLock { get; } = new(1, 1);
    public WebSocketSender Sender { get; }
    public ConcurrentDictionary<Guid, CancellationTokenSource> ActiveCommands { get; } = new();
    public ConcurrentDictionary<Guid, TaskCompletionSource<string?>> SudoWaiters { get; } = new();
    public ConcurrentDictionary<Guid, Action> ActiveStdinClosers { get; } = new();
    public ConcurrentDictionary<Guid, StreamWriter> ActiveStdinWriters { get; } = new();

    public ClientContext(string clientId, System.Net.WebSockets.WebSocket webSocket)
    {
        ClientId = clientId;
        WebSocket = webSocket;
        Executor = new TerminalExecutor();
        Sender = new WebSocketSender(webSocket, SendLock);
    }

    public void Dispose()
    {
        foreach (var cts in ActiveCommands.Values)
        {
            try { cts.Cancel(); cts.Dispose(); }
            catch { /* already disposed */ }
        }

        ActiveCommands.Clear();

        foreach (var close in ActiveStdinClosers.Values)
        {
            try { close(); }
            catch { /* stream already disposed */ }
        }

        ActiveStdinClosers.Clear();

        foreach (var close in ActiveStdinClosers.Values)
        {
            try { close(); }
            catch { /* stream already disposed */ }
        }

        ActiveStdinClosers.Clear();
        try
        {
            if (WebSocket.State != WebSocketState.Closed && WebSocket.State != WebSocketState.Aborted)
            {
                WebSocket.Abort();
            }
        }
        catch { /* ignore socket shutdown errors */ }

        try { WebSocket.Dispose(); }
        catch { /* ignore socket dispose errors */ }

        Executor.Dispose();
        SendLock.Dispose();
    }
}
