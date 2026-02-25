using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Project;

public static class ProjectProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid projectId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectCreateResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(projectId);
        return writer.ToArray();
    }

    public static byte[] SerializeLoadResult(Guid messageId, bool success, ProjectDto? project)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && project != null)
        {
            WriteProject(writer, project);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<ProjectMetadataDto> projects)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListResult);
        writer.WriteGuid(messageId);

        var list = projects.ToList();
        writer.WriteInt32(list.Count);

        foreach (var meta in list)
        {
            WriteProjectMetadata(writer, meta);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectDeleteResult, messageId, success);

    public static byte[] SerializeAddFolderResult(Guid messageId, bool success, Guid folderId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectAddFolderResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(folderId);
        return writer.ToArray();
    }

    public static byte[] SerializeDeleteFolderResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectDeleteFolderResult, messageId, success);

    public static byte[] SerializeRenameFolderResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectRenameFolderResult, messageId, success);

    public static byte[] SerializeAssignSessionResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ProjectAssignSessionResult, messageId, success);

    public static byte[] SerializeWriteUpMoveResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.WriteUpMoveResult, messageId, success);

    public static byte[] SerializeListSessionsResult(Guid messageId, List<SessionMetadataDto> sessions)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListSessionsResult);
        writer.WriteGuid(messageId);
        writer.WriteInt32(sessions.Count);

        foreach (var meta in sessions)
        {
            WriteSessionMetadata(writer, meta);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeListWriteUpsResult(Guid messageId, List<WriteUpMetadataDto> writeUps)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListWriteUpsResult);
        writer.WriteGuid(messageId);
        writer.WriteInt32(writeUps.Count);

        foreach (var writeUp in writeUps)
        {
            WriteWriteUpMetadata(writer, writeUp);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.ProjectOperationError, messageId, error);

    private static void WriteProject(PooledBufferWriter writer, ProjectDto project)
    {
        writer.WriteGuid(project.Id);
        writer.WriteString(project.Name ?? string.Empty);
        writer.WriteString(project.Description ?? string.Empty);
        writer.WriteInt64(project.CreatedAt.Ticks);
        writer.WriteInt64(project.UpdatedAt.Ticks);

        writer.WriteInt32(project.Folders.Count);
        foreach (var folder in project.Folders)
        {
            WriteProjectFolder(writer, folder);
        }
    }

    private static void WriteProjectFolder(PooledBufferWriter writer, ProjectFolderDto folder)
    {
        writer.WriteGuid(folder.Id);
        writer.WriteString(folder.Name ?? string.Empty);
        writer.WriteByte((byte)(folder.IsSystem ? 1 : 0));
    }

    private static void WriteSessionMetadata(PooledBufferWriter writer, SessionMetadataDto meta)
    {
        writer.WriteGuid(meta.Id);
        writer.WriteString(meta.Name);
        writer.WriteString(meta.Description ?? string.Empty);
        writer.WriteInt64(meta.CreatedAt.Ticks);
        writer.WriteInt64(meta.UpdatedAt.Ticks);
        writer.WriteInt32(meta.HistoryCount);
        writer.WriteInt32(meta.TargetCount);
        writer.WriteGuid(meta.ProjectId ?? Guid.Empty);
    }

    private static void WriteWriteUpMetadata(PooledBufferWriter writer, WriteUpMetadataDto writeUp)
    {
        writer.WriteGuid(writeUp.Id);
        writer.WriteGuid(writeUp.SessionId);
        writer.WriteGuid(writeUp.FolderId ?? Guid.Empty);
        writer.WriteString(writeUp.Name);
        writer.WriteInt64(writeUp.CreatedAt.Ticks);
        writer.WriteInt64(writeUp.UpdatedAt.Ticks);
    }

    private static void WriteProjectMetadata(PooledBufferWriter writer, ProjectMetadataDto meta)
    {
        writer.WriteGuid(meta.Id);
        writer.WriteString(meta.Name ?? string.Empty);
        writer.WriteString(meta.Description ?? string.Empty);
        writer.WriteInt64(meta.CreatedAt.Ticks);
        writer.WriteInt64(meta.UpdatedAt.Ticks);
        writer.WriteInt32(meta.FolderCount);
        writer.WriteInt32(meta.SessionCount);
    }
}
