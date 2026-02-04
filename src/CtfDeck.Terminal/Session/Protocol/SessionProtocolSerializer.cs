using CtfDeck.Terminal.Session.Models;
using CtfDeck.Terminal.WebSocket;

namespace CtfDeck.Terminal.Session.Protocol;

public static class SessionProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid sessionId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionCreateResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(sessionId);
        return writer.ToArray();
    }

    public static byte[] SerializeSetActiveResult(Guid messageId, bool success)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionSetActiveResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        return writer.ToArray();
    }

    public static byte[] SerializeLoadResult(Guid messageId, bool success, Models.Session? session)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && session != null)
        {
            WriteSession(writer, session);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<SessionMetadata> sessions)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionListResult);
        writer.WriteGuid(messageId);

        var list = sessions.ToList();
        writer.WriteInt32(list.Count);

        foreach (var meta in list)
        {
            WriteSessionMetadata(writer, meta);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionDeleteResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionOperationError);
        writer.WriteGuid(messageId);
        writer.WriteString(error);
        return writer.ToArray();
    }

    private static void WriteSession(PooledBufferWriter writer, Models.Session session)
    {
        writer.WriteGuid(session.Id);
        writer.WriteString(session.Name);
        writer.WriteInt64(session.CreatedAt.Ticks);
        writer.WriteInt64(session.UpdatedAt.Ticks);

        writer.WriteInt32(session.History.Count);
        foreach (var entry in session.History)
        {
            WriteHistoryEntry(writer, entry);
        }

        writer.WriteInt32(session.Targets.Count);
        foreach (var target in session.Targets)
        {
            WriteSessionTarget(writer, target);
        }
    }

    private static void WriteHistoryEntry(PooledBufferWriter writer, HistoryEntry entry)
    {
        writer.WriteGuid(entry.Id);
        writer.WriteInt64(entry.Timestamp.Ticks);
        writer.WriteString(entry.WorkingDirectory);
        writer.WriteString(entry.Command);
        writer.WriteString(entry.Output);
        writer.WriteInt32(entry.ExitCode);
    }

    private static void WriteSessionTarget(PooledBufferWriter writer, SessionTarget target)
    {
        writer.WriteGuid(target.Id);
        writer.WriteString(target.Address);
        writer.WriteInt32(target.Port ?? -1);
        writer.WriteString(target.Name);
        writer.WriteInt32((int)target.Type);
    }

    private static void WriteSessionMetadata(PooledBufferWriter writer, SessionMetadata meta)
    {
        writer.WriteGuid(meta.Id);
        writer.WriteString(meta.Name);
        writer.WriteInt64(meta.CreatedAt.Ticks);
        writer.WriteInt64(meta.UpdatedAt.Ticks);
        writer.WriteInt32(meta.HistoryCount);
        writer.WriteInt32(meta.TargetCount);
    }
}
