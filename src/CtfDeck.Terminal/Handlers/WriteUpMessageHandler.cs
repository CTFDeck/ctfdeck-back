using CtfDeck.Terminal.Features.WriteUps;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.WriteUp;

namespace CtfDeck.Terminal.Handlers;

public sealed class WriteUpMessageHandler : MessageHandlerBase
{
    private readonly WriteUpService _writeUpService;

    public WriteUpMessageHandler(WriteUpService writeUpService)
    {
        _writeUpService = writeUpService;
    }

    protected override bool CanHandle(MessageType type)
        => WriteUpProtocolDeserializer.IsWriteUpMessage(type);

    protected override byte[] SerializeError(Guid messageId, string error)
        => WriteUpProtocolSerializer.SerializeError(messageId, error);

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            MessageType.WriteUpCreate => HandleCreate(data),
            MessageType.WriteUpUpdate => HandleUpdate(data),
            MessageType.WriteUpDelete => HandleDelete(data),
            MessageType.WriteUpList => HandleList(data),
            MessageType.WriteUpLoad => HandleLoad(data),
            _ => throw new InvalidOperationException($"Unknown write-up message type: {type}")
        };
    }

    private byte[] HandleCreate(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpCreateRequest(data);
        var writeUp = _writeUpService.Create(request.SessionId, request.Name);
        return WriteUpProtocolSerializer.SerializeCreateResult(request.MessageId, true, writeUp.Id);
    }

    private byte[] HandleUpdate(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpUpdateRequest(data);
        var success = _writeUpService.Update(request.WriteUpId, request.Name, request.Content);
        return WriteUpProtocolSerializer.SerializeUpdateResult(request.MessageId, success);
    }

    private byte[] HandleDelete(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpDeleteRequest(data);
        var success = _writeUpService.Delete(request.WriteUpId);
        return WriteUpProtocolSerializer.SerializeDeleteResult(request.MessageId, success);
    }

    private byte[] HandleList(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpListRequest(data);
        var result = request.SessionId == Guid.Empty
            ? _writeUpService.GetAllMetadata(request.Offset, request.Limit, request.UnassignedOnly)
            : _writeUpService.GetBySessionId(request.SessionId, request.Offset, request.Limit);
        return WriteUpProtocolSerializer.SerializeListResult(request.MessageId, result.Items, result.TotalCount);
    }

    private byte[] HandleLoad(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpLoadRequest(data);
        var writeUp = _writeUpService.GetById(request.WriteUpId);
        return WriteUpProtocolSerializer.SerializeLoadResult(request.MessageId, writeUp != null, writeUp);
    }
}
