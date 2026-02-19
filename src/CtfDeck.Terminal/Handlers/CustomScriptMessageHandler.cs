using CtfDeck.Terminal.Features.Scripts;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.CustomScript;

namespace CtfDeck.Terminal.Handlers;

public sealed class CustomScriptMessageHandler : MessageHandlerBase
{
    private readonly CustomScriptService _scriptService;

    public CustomScriptMessageHandler(CustomScriptService scriptService)
    {
        _scriptService = scriptService;
    }

    protected override bool CanHandle(MessageType type)
        => CustomScriptProtocolDeserializer.IsCustomScriptMessage(type);

    protected override byte[] SerializeError(Guid messageId, string error)
        => CustomScriptProtocolSerializer.SerializeError(messageId, error);

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            MessageType.CustomScriptCreate => HandleCreate(data),
            MessageType.CustomScriptUpdate => HandleUpdate(data),
            MessageType.CustomScriptDelete => HandleDelete(data),
            MessageType.CustomScriptList => HandleList(data),
            _ => throw new InvalidOperationException($"Unknown custom script message type: {type}")
        };
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
