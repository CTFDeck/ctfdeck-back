using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Project;

public readonly ref struct ProjectCreateRequest
{
    public readonly Guid MessageId;
    public readonly string Name;
    public readonly string Description;

    public ProjectCreateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Name = reader.ReadString();
        Description = reader.ReadString();
    }
}

public readonly ref struct ProjectLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;

    public ProjectLoadRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
    }
}

public readonly ref struct ProjectListRequest
{
    public readonly Guid MessageId;
    public readonly int Offset;
    public readonly int Limit;

    public ProjectListRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Offset = reader.ReadInt32();
        Limit = reader.ReadInt32();
    }
}

public readonly ref struct ProjectUpdateRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly string Name;
    public readonly string Description;

    public ProjectUpdateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        Name = reader.ReadString();
        Description = reader.ReadString();
    }
}

public readonly ref struct ProjectDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;

    public ProjectDeleteRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
    }
}

public readonly ref struct ProjectAddFolderRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid? ParentId;
    public readonly string Name;

    public ProjectAddFolderRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        var parentId = reader.ReadGuid();
        ParentId = parentId == Guid.Empty ? null : parentId;
        Name = reader.ReadString();
    }
}

public readonly ref struct ProjectDeleteFolderRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid FolderId;

    public ProjectDeleteFolderRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        FolderId = reader.ReadGuid();
    }
}

public readonly ref struct ProjectRenameFolderRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid FolderId;
    public readonly string Name;

    public ProjectRenameFolderRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        FolderId = reader.ReadGuid();
        Name = reader.ReadString();
    }
}

public readonly ref struct WriteUpMoveRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;
    public readonly Guid ProjectId;
    public readonly Guid? FolderId;

    public WriteUpMoveRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        WriteUpId = reader.ReadGuid();
        ProjectId = reader.ReadGuid();
        var fid = reader.ReadGuid();
        FolderId = fid == Guid.Empty ? null : fid;
    }
}

public readonly ref struct ProjectAssignSessionRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid? FolderId;
    public readonly Guid SessionId;

    public ProjectAssignSessionRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        var folderId = reader.ReadGuid();
        FolderId = folderId == Guid.Empty ? null : folderId;
        SessionId = reader.ReadGuid();
    }
}

public readonly ref struct ProjectListSessionsRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly int Offset;
    public readonly int Limit;

    public ProjectListSessionsRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        Offset = reader.ReadInt32();
        Limit = reader.ReadInt32();
    }
}

public readonly ref struct ProjectListWriteUpsRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid? FolderId;
    public readonly int Offset;
    public readonly int Limit;

    public ProjectListWriteUpsRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        var fid = reader.ReadGuid();
        FolderId = fid == Guid.Empty ? null : fid;
        Offset = reader.ReadInt32();
        Limit = reader.ReadInt32();
    }
}

public readonly ref struct ProjectExportRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly string Path;
    public readonly ExportOptions Options;

    public ProjectExportRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ProjectId = reader.ReadGuid();
        Path = reader.ReadString();
        Options = reader.Remaining ? ExportOptions.FromFlags(reader.ReadByte()) : ExportOptions.All;
    }
}

public readonly ref struct ProjectImportRequest
{
    public readonly Guid MessageId;
    public readonly string Path;

    public ProjectImportRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Path = reader.ReadString();
    }
}

public readonly ref struct ProjectListExportsRequest
{
    public readonly Guid MessageId;

    public ProjectListExportsRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
    }
}

public static class ProjectProtocolDeserializer
{
    public static bool IsProjectMessage(MessageType type)
    {
        return (type >= MessageType.ProjectCreate && type <= MessageType.ProjectAssignSession)
            || type == MessageType.WriteUpMove
            || (type >= MessageType.ProjectListSessions && type <= MessageType.ProjectListWriteUps)
            || (type >= MessageType.ProjectExport && type <= MessageType.ProjectListExports);
    }
}
