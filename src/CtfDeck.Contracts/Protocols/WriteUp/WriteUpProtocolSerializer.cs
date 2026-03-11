using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.WriteUp;

public static class WriteUpProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid writeUpId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpCreateResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(writeUpId);
        return writer.ToArray();
    }

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, List<WriteUpMetadataDto> writeUps, int totalCount)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpListResult);
        writer.WriteGuid(messageId);
        writer.WriteInt32(writeUps.Count);
        writer.WriteInt32(totalCount);

        foreach (var writeUp in writeUps)
        {
            WriteWriteUpMetadata(writer, writeUp);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeLoadResult(Guid messageId, bool success, WriteUpDto? writeUp)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && writeUp != null)
        {
            writer.WriteGuid(writeUp.Id);
            writer.WriteGuid(writeUp.SessionId ?? Guid.Empty);
            writer.WriteGuid(writeUp.FolderId ?? Guid.Empty);
            writer.WriteString(writeUp.Name);
            writer.WriteString(writeUp.Content);
            writer.WriteInt64(writeUp.CreatedAt.Ticks);
            writer.WriteInt64(writeUp.UpdatedAt.Ticks);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.WriteUpOperationError, messageId, error);

    private static void WriteWriteUpMetadata(PooledBufferWriter writer, WriteUpMetadataDto writeUp)
    {
        writer.WriteGuid(writeUp.Id);
        writer.WriteGuid(writeUp.SessionId ?? Guid.Empty);
        writer.WriteGuid(writeUp.FolderId ?? Guid.Empty);
        writer.WriteString(writeUp.Name);
        writer.WriteInt64(writeUp.CreatedAt.Ticks);
        writer.WriteInt64(writeUp.UpdatedAt.Ticks);
    }
}
