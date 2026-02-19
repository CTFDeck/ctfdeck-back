using System.Net.WebSockets;
using CtfDeck.Terminal.Features.Scripts;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.CustomScript;

namespace CtfDeck.Terminal.Handlers;

public class CustomScriptMessageHandler
{
    private readonly CustomScriptService _scriptService;

    public CustomScriptMessageHandler(CustomScriptService scriptService)
    {
        _scriptService = scriptService;
    }

    public async Task<bool> TryHandleAsync(
        string clientId,
        ReadOnlyMemory<byte> data,
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken,
        SemaphoreSlim? sendLock = null)
    {
        if (data.Length == 0) return false;

        var messageType = (MessageType)data.Span[0];
        if (!CustomScriptProtocolDeserializer.IsCustomScriptMessage(messageType)) return false;

        byte[] response;

        try
        {
            response = messageType switch
            {
                MessageType.CustomScriptCreate => HandleCreate(data.Span),
                MessageType.CustomScriptUpdate => HandleUpdate(data.Span),
                MessageType.CustomScriptDelete => HandleDelete(data.Span),
                MessageType.CustomScriptList => HandleList(data.Span),
                _ => throw new InvalidOperationException($"Unknown custom script message type: {messageType}")
            };
        }
        catch (Exception ex)
        {
            var msgId = data.Length >= 17 ? new Guid(data.Span.Slice(1, 16)) : Guid.Empty;
            response = CustomScriptProtocolSerializer.SerializeError(msgId, ex.Message);
        }

        if (sendLock != null)
        {
            await sendLock.WaitAsync(cancellationToken);
            try
            {
                await webSocket.SendAsync(
                    response,
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);
            }
            finally
            {
                sendLock.Release();
            }
        }
        else
        {
            await webSocket.SendAsync(
                response,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
        }

        return true;
    }

    private byte[] HandleCreate(ReadOnlySpan<byte> data)
    {
        var request = new CustomScriptCreateRequest(data);
        var script = _scriptService.Create(request.Name, request.Category, request.Template);
        return CustomScriptProtocolSerializer.SerializeCreateResult(request.MessageId, true, script.Id);
    }

    private byte[] HandleUpdate(ReadOnlySpan<byte> data)
    {
        var request = new CustomScriptUpdateRequest(data);
        var success = _scriptService.Update(request.ScriptId, request.Name, request.Category, request.Template);
        return CustomScriptProtocolSerializer.SerializeUpdateResult(request.MessageId, success);
    }

    private byte[] HandleDelete(ReadOnlySpan<byte> data)
    {
        var request = new CustomScriptDeleteRequest(data);
        var success = _scriptService.Delete(request.ScriptId);
        return CustomScriptProtocolSerializer.SerializeDeleteResult(request.MessageId, success);
    }

    private byte[] HandleList(ReadOnlySpan<byte> data)
    {
        var request = new CustomScriptListRequest(data);
        var scripts = _scriptService.GetAll();
        return CustomScriptProtocolSerializer.SerializeListResult(request.MessageId, scripts);
    }
}
