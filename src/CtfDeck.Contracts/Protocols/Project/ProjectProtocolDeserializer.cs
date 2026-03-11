using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Project;

public readonly ref struct ProjectCreateRequest
{
    public readonly Guid MessageId;
    public readonly string Name;
    public readonly string Description;

    public ProjectCreateRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][4B nameLen][name][4B descLen][desc]
        MessageId = new Guid(data.Slice(1, 16));

        var offset = 17;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var descLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Description = Encoding.UTF8.GetString(data.Slice(offset, descLen));
    }
}

public readonly ref struct ProjectLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;

    public ProjectLoadRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct ProjectListRequest
{
    public readonly Guid MessageId;
    public readonly int Offset;
    public readonly int Limit;

    public ProjectListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][4B offset][4B limit]
        MessageId = new Guid(data.Slice(1, 16));
        Offset = BitConverter.ToInt32(data.Slice(17, 4));
        Limit = BitConverter.ToInt32(data.Slice(21, 4));
    }
}

public class ProjectUpdateRequest
{
    public Guid MessageId { get; }
    public Guid ProjectId { get; }
    public string Name { get; }
    public string Description { get; }

    public ProjectUpdateRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId][4B nameLen][name][4B descLen][desc]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));

        var offset = 33;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var descLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Description = Encoding.UTF8.GetString(data.Slice(offset, descLen));
    }
}

public readonly ref struct ProjectDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;

    public ProjectDeleteRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
    }
}

public class ProjectAddFolderRequest
{
    public Guid MessageId { get; }
    public Guid ProjectId { get; }
    public Guid? ParentId { get; }
    public string Name { get; }

    public ProjectAddFolderRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId][16B parentId][4B nameLen][name]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));

        var parentIdRaw = new Guid(data.Slice(33, 16));
        ParentId = parentIdRaw == Guid.Empty ? null : parentIdRaw;

        var offset = 49;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
    }
}

public readonly ref struct ProjectDeleteFolderRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid FolderId;

    public ProjectDeleteFolderRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId][16B folderId]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
        FolderId = new Guid(data.Slice(33, 16));
    }
}

public class ProjectRenameFolderRequest
{
    public Guid MessageId { get; }
    public Guid ProjectId { get; }
    public Guid FolderId { get; }
    public string Name { get; }

    public ProjectRenameFolderRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId][16B folderId][4B nameLen][name]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
        FolderId = new Guid(data.Slice(33, 16));

        var offset = 49;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
    }
}

public readonly ref struct ProjectAssignSessionRequest
{
    public readonly Guid MessageId;
    public readonly Guid ProjectId;
    public readonly Guid SessionId;
    public readonly Guid FolderId;

    public ProjectAssignSessionRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B projectId][16B sessionId][16B folderId]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
        SessionId = new Guid(data.Slice(33, 16));
        FolderId = new Guid(data.Slice(49, 16));
    }
}

public readonly ref struct WriteUpMoveRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;
    public readonly Guid ProjectId;
    public readonly Guid FolderId;

    public WriteUpMoveRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B writeUpId][16B projectId][16B folderId]
        MessageId = new Guid(data.Slice(1, 16));
        WriteUpId = new Guid(data.Slice(17, 16));
        ProjectId = new Guid(data.Slice(33, 16));
        FolderId = new Guid(data.Slice(49, 16));
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
        // Format: [1B type][16B msgId][16B projectId][4B offset][4B limit]
        MessageId = new Guid(data.Slice(1, 16));
        ProjectId = new Guid(data.Slice(17, 16));
        Offset = BitConverter.ToInt32(data.Slice(33, 4));
        Limit = BitConverter.ToInt32(data.Slice(37, 4));
    }
}

public readonly ref struct ProjectListWriteUpsRequest
{
    public readonly Guid MessageId;
    public readonly Guid FolderId;
    public readonly int Offset;
    public readonly int Limit;

    public ProjectListWriteUpsRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B folderId][4B offset][4B limit]
        MessageId = new Guid(data.Slice(1, 16));
        FolderId = new Guid(data.Slice(17, 16));
        Offset = BitConverter.ToInt32(data.Slice(33, 4));
        Limit = BitConverter.ToInt32(data.Slice(37, 4));
    }
}

public static class ProjectProtocolDeserializer
{
    public static bool IsProjectMessage(MessageType type)
    {
        return (type >= MessageType.ProjectCreate && type <= MessageType.ProjectAssignSession)
            || type == MessageType.WriteUpMove
            || (type >= MessageType.ProjectListSessions && type <= MessageType.ProjectListWriteUps);
    }
}
