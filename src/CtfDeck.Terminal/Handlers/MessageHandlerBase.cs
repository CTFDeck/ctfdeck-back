using CtfDeck.Contracts.Transport;

namespace CtfDeck.Terminal.Handlers;

public abstract class MessageHandlerBase
{
    protected abstract bool CanHandle(MessageType type);
    protected abstract byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data);
    protected abstract byte[] SerializeError(Guid messageId, string error);
    protected Func<byte[], Task>? CurrentSendAsync { get; private set; }
    protected CancellationToken CurrentCancellationToken { get; private set; }

    public async Task<bool> TryHandleAsync(
        string clientId,
        ReadOnlyMemory<byte> message,
        Func<byte[], Task> sendAsync,
        CancellationToken cancellationToken)
    {
        var type = (MessageType)message.Span[0];

        if (!CanHandle(type))
            return false;

        Guid messageId = Guid.Empty;

        try
        {
            CurrentSendAsync = sendAsync;
            CurrentCancellationToken = cancellationToken;

            // Extract messageId from the payload [1B type][16B msgId]
            if (message.Length >= 17)
            {
                messageId = new Guid(message.Span.Slice(1, 16));
            }

            var response = Dispatch(clientId, type, message.Span);

            if (response.Length > 0)
                await sendAsync(response);

            return true;
        }
        catch (Exception ex)
        {
            // Report error with the extracted messageId (if any)
            await sendAsync(SerializeError(messageId, ex.Message));
            return true;
        }
        finally
        {
            CurrentSendAsync = null;
            CurrentCancellationToken = default;
        }
    }
}
