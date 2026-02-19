using CtfDeck.Contracts.Transport;

namespace CtfDeck.Terminal.Handlers;

public abstract class MessageHandlerBase
{
    protected abstract bool CanHandle(MessageType type);
    protected abstract byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data);
    protected abstract byte[] SerializeError(Guid messageId, string error);

    public async Task<bool> TryHandleAsync(
        string clientId,
        ReadOnlyMemory<byte> data,
        Func<byte[], CancellationToken, Task> sendAsync,
        CancellationToken cancellationToken)
    {
        if (data.Length == 0) return false;

        var type = (MessageType)data.Span[0];
        if (!CanHandle(type)) return false;

        byte[] response;

        try
        {
            response = Dispatch(clientId, type, data.Span);
        }
        catch (Exception ex)
        {
            var msgId = data.Length >= 17 ? new Guid(data.Span.Slice(1, 16)) : Guid.Empty;
            response = SerializeError(msgId, ex.Message);
        }

        await sendAsync(response, cancellationToken);
        return true;
    }
}
