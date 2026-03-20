using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.WriteUp;

public static class WriteUpProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid writeUpId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.WriteUpCreateResult, messageId, success, writeUpId);

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<WriteUpMetadataDto> writeUps, int totalCount)
        => BinaryProtocolSerializer.SerializeList(MessageType.WriteUpListResult, messageId, writeUps, totalCount, (w, wp) => w.WriteWriteUp(wp));

    public static byte[] SerializeLoadResult(Guid messageId, bool success, WriteUpDto? writeUp)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && writeUp != null)
        {
            writer.WriteWriteUp(writeUp);
            writer.WriteString(writeUp.Content);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.WriteUpOperationError, messageId, error);
}
