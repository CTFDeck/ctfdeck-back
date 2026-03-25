using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Session;

public static class SessionProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid sessionId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.SessionCreateResult, messageId, success, sessionId);

    public static byte[] SerializeSetActiveResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.SessionSetActiveResult, messageId, success);

    public static byte[] SerializeLoadResult(Guid messageId, bool success, SessionDto? session)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && session != null)
        {
            writer.WriteSession(session);

            foreach (var entry in session.History)
            {
                WriteHistoryEntry(writer, entry);
            }

            foreach (var target in session.Targets)
            {
                writer.WriteTarget(target);
            }
        }

        return writer.ToArray();
    }

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<SessionMetadataDto> sessions, int totalCount)
        => BinaryProtocolSerializer.SerializeList(MessageType.SessionListResult, messageId, sessions, totalCount, (w, s) => w.WriteSession(s));

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.SessionUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.SessionDeleteResult, messageId, success);

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.SessionOperationError, messageId, error);

    public static byte[] SerializeAddTargetResult(Guid messageId, bool success, Guid targetId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.SessionAddTargetResult, messageId, success, targetId);

    public static byte[] SerializeDeleteTargetResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.SessionDeleteTargetResult, messageId, success);

    public static byte[] SerializeEditTargetResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.SessionEditTargetResult, messageId, success);

    private static void WriteHistoryEntry(PooledBufferWriter writer, HistoryEntryDto entry)
    {
        writer.WriteGuid(entry.Id);
        writer.WriteInt64(entry.Timestamp.Ticks);
        writer.WriteString(entry.WorkingDirectory ?? string.Empty);
        writer.WriteString(entry.Command ?? string.Empty);
        writer.WriteString(entry.Output ?? string.Empty);
        writer.WriteInt32(entry.ExitCode);
    }
}
