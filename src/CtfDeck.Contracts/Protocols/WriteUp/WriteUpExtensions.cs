using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.WriteUp;

public static class WriteUpExtensions
{
    public static void WriteWriteUp(this PooledBufferWriter writer, WriteUpMetadataDto writeUp)
    {
        writer.WriteGuid(writeUp.Id);
        writer.WriteGuid(writeUp.SessionId ?? Guid.Empty);
        writer.WriteGuid(writeUp.ProjectId ?? Guid.Empty);
        writer.WriteGuid(writeUp.FolderId ?? Guid.Empty);
        writer.WriteString(writeUp.Name);
        writer.WriteInt64(writeUp.CreatedAt.Ticks);
        writer.WriteInt64(writeUp.UpdatedAt.Ticks);
    }

    public static void WriteWriteUp(this PooledBufferWriter writer, WriteUpDto writeUp)
    {
        writer.WriteGuid(writeUp.Id);
        writer.WriteGuid(writeUp.SessionId ?? Guid.Empty);
        writer.WriteGuid(writeUp.ProjectId ?? Guid.Empty);
        writer.WriteGuid(writeUp.FolderId ?? Guid.Empty);
        writer.WriteString(writeUp.Name);
        writer.WriteInt64(writeUp.CreatedAt.Ticks);
        writer.WriteInt64(writeUp.UpdatedAt.Ticks);
    }

    public static WriteUpMetadataDto ReadWriteUp(this ref BinaryProtocolReader reader)
    {
        return new WriteUpMetadataDto
        {
            Id = reader.ReadGuid(),
            SessionId = reader.ReadGuid().NullIfEmpty(),
            ProjectId = reader.ReadGuid().NullIfEmpty(),
            FolderId = reader.ReadGuid().NullIfEmpty(),
            Name = reader.ReadString(),
            CreatedAt = new DateTime(reader.ReadInt64()),
            UpdatedAt = new DateTime(reader.ReadInt64())
        };
    }

    private static Guid? NullIfEmpty(this Guid guid) => guid == Guid.Empty ? null : guid;
}
