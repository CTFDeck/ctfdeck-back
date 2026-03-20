using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Session;

public static class SessionExtensions
{
    public static void WriteSession(this PooledBufferWriter writer, SessionMetadataDto meta)
    {
        writer.WriteGuid(meta.Id);
        writer.WriteString(meta.Name);
        writer.WriteString(meta.Description ?? string.Empty);
        writer.WriteInt64(meta.CreatedAt.Ticks);
        writer.WriteInt64(meta.UpdatedAt.Ticks);
        writer.WriteInt32(meta.HistoryCount);
        writer.WriteInt32(meta.TargetCount);
        writer.WriteGuid(meta.ProjectId ?? Guid.Empty);
        writer.WriteGuid(meta.FolderId ?? Guid.Empty);
    }

    public static void WriteSession(this PooledBufferWriter writer, SessionDto session)
    {
        writer.WriteGuid(session.Id);
        writer.WriteString(session.Name);
        writer.WriteString(session.Description ?? string.Empty);
        writer.WriteInt64(session.CreatedAt.Ticks);
        writer.WriteInt64(session.UpdatedAt.Ticks);
        writer.WriteInt32(session.History.Count);
        writer.WriteInt32(session.Targets.Count);
        writer.WriteGuid(session.ProjectId ?? Guid.Empty);
        writer.WriteGuid(session.FolderId ?? Guid.Empty);
    }

    public static void WriteTarget(this PooledBufferWriter writer, SessionTargetDto target)
    {
        writer.WriteGuid(target.Id);
        writer.WriteString(target.Address);
        writer.WriteInt32(target.Port ?? -1);
        writer.WriteString(target.Name);
        writer.WriteString(target.Description);
        writer.WriteInt32((int)target.Type);
    }

    public static SessionTargetDto ReadTarget(this ref BinaryProtocolReader reader, Guid targetId = default)
    {
        var id = targetId == Guid.Empty ? reader.ReadGuid() : targetId;
        var address = reader.ReadString();
        var portValue = reader.ReadInt32();
        int? port = portValue == -1 ? null : portValue;
        var name = reader.ReadString();
        var description = reader.ReadString();
        var type = (TargetType)reader.ReadInt32();

        return new SessionTargetDto
        {
            Id = id,
            Address = address,
            Port = port,
            Name = name,
            Description = description,
            Type = type
        };
    }

    public static SessionMetadataDto ReadSession(this ref BinaryProtocolReader reader)
    {
        return new SessionMetadataDto
        {
            Id = reader.ReadGuid(),
            Name = reader.ReadString(),
            Description = reader.ReadString(),
            CreatedAt = new DateTime(reader.ReadInt64()),
            UpdatedAt = new DateTime(reader.ReadInt64()),
            HistoryCount = reader.ReadInt32(),
            TargetCount = reader.ReadInt32(),
            ProjectId = reader.ReadGuid().NullIfEmpty(),
            FolderId = reader.ReadGuid().NullIfEmpty()
        };
    }

    private static Guid? NullIfEmpty(this Guid guid) => guid == Guid.Empty ? null : guid;
}
