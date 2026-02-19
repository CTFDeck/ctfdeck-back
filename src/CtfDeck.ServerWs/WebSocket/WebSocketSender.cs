using System.Net.WebSockets;

namespace CtfDeck.ServerWs.WebSocket;

public sealed class WebSocketSender
{
    private readonly System.Net.WebSockets.WebSocket _socket;
    private readonly SemaphoreSlim _sendLock;

    public WebSocketSender(System.Net.WebSockets.WebSocket socket, SemaphoreSlim sendLock)
    {
        _socket = socket;
        _sendLock = sendLock;
    }

    public async Task SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_socket.State != WebSocketState.Open) return;

        await _sendLock.WaitAsync(ct);
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    ct);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
