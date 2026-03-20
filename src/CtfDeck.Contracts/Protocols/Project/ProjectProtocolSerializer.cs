using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Protocols.Session;
using CtfDeck.Contracts.Protocols.WriteUp;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Project;

public static class ProjectProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid projectId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.ProjectCreateResult, messageId, success, projectId);

    public static byte[] SerializeLoadResult(Guid messageId, bool success, ProjectDto? project)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && project != null)
        {
            writer.WriteProject(project);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<ProjectMetadataDto> projects, int totalCount)
        => BinaryProtocolSerializer.SerializeList(MessageType.ProjectListResult, messageId, projects, totalCount, (w, p) => w.WriteProject(p));

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectDeleteResult, messageId, success);

    public static byte[] SerializeAddFolderResult(Guid messageId, bool success, Guid folderId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.ProjectAddFolderResult, messageId, success, folderId);

    public static byte[] SerializeDeleteFolderResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectDeleteFolderResult, messageId, success);

    public static byte[] SerializeRenameFolderResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectRenameFolderResult, messageId, success);

    public static byte[] SerializeAssignSessionResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectAssignSessionResult, messageId, success);

    public static byte[] SerializeWriteUpMoveResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpMoveResult, messageId, success);

    public static byte[] SerializeListSessionsResult(Guid messageId, List<SessionMetadataDto> sessions, int totalCount)
        => BinaryProtocolSerializer.SerializeList(MessageType.ProjectListSessionsResult, messageId, sessions, totalCount, (w, s) => w.WriteSession(s));

    public static byte[] SerializeListWriteUpsResult(Guid messageId, List<WriteUpMetadataDto> writeUps, int totalCount)
        => BinaryProtocolSerializer.SerializeList(MessageType.ProjectListWriteUpsResult, messageId, writeUps, totalCount, (w, wp) => w.WriteWriteUp(wp));

    public static byte[] SerializeExportResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectExportResult, messageId, success);

    public static byte[] SerializeImportResult(Guid messageId, bool success, Guid projectId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.ProjectImportResult, messageId, success, projectId);

    public static byte[] SerializeListExportsResult(Guid messageId, IEnumerable<ProjectExportMetadata> exports)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListExportsResult);
        writer.WriteGuid(messageId);

        var list = exports.ToList();
        writer.WriteInt32(list.Count);

        foreach (var export in list)
        {
            writer.WriteString(export.Filename);
            writer.WriteInt64(export.SizeBytes);
            writer.WriteInt32(export.SessionCount);
            writer.WriteInt32(export.WriteUpCount);
            writer.WriteInt64(((DateTimeOffset)export.ExportedAt).ToUnixTimeMilliseconds());
            writer.WriteGuid(export.ProjectId);
            writer.WriteByte((byte)(export.IsAlreadyImported ? 1 : 0));
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.ProjectOperationError, messageId, error);
}
