using CtfDeck.Terminal.Features.Projects;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.Project;

namespace CtfDeck.Terminal.Handlers;

public sealed class ProjectMessageHandler : MessageHandlerBase
{
    private readonly ProjectService _projectService;

    public ProjectMessageHandler(ProjectService projectService)
    {
        _projectService = projectService;
    }

    protected override bool CanHandle(MessageType type)
        => ProjectProtocolDeserializer.IsProjectMessage(type);

    protected override byte[] SerializeError(Guid messageId, string error)
        => ProjectProtocolSerializer.SerializeError(messageId, error);

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            MessageType.ProjectCreate => HandleCreate(data),
            MessageType.ProjectLoad => HandleLoad(data),
            MessageType.ProjectList => HandleList(data),
            MessageType.ProjectUpdate => HandleUpdate(data),
            MessageType.ProjectDelete => HandleDelete(data),
            MessageType.ProjectAddFolder => HandleAddFolder(data),
            MessageType.ProjectDeleteFolder => HandleDeleteFolder(data),
            MessageType.ProjectRenameFolder => HandleRenameFolder(data),
            MessageType.ProjectAssignSession => HandleAssignSession(data),
            MessageType.WriteUpMove => HandleWriteUpMove(data),
            MessageType.ProjectListSessions => HandleListSessions(data),
            MessageType.ProjectListWriteUps => HandleListWriteUps(data),
            MessageType.ProjectExport => HandleExport(data),
            MessageType.ProjectImport => HandleImport(data),
            _ => throw new InvalidOperationException($"Unknown project message type: {type}")
        };
    }

    private byte[] HandleCreate(ReadOnlySpan<byte> data)
    {
        var request = new ProjectCreateRequest(data);
        var project = _projectService.Create(request.Name, request.Description);
        return ProjectProtocolSerializer.SerializeCreateResult(request.MessageId, true, project.Id);
    }

    private byte[] HandleLoad(ReadOnlySpan<byte> data)
    {
        var request = new ProjectLoadRequest(data);
        var project = _projectService.GetById(request.ProjectId);
        return ProjectProtocolSerializer.SerializeLoadResult(request.MessageId, project != null, project);
    }

    private byte[] HandleList(ReadOnlySpan<byte> data)
    {
        var request = new ProjectListRequest(data);
        var result = _projectService.GetAllMetadata(request.Offset, request.Limit);
        return ProjectProtocolSerializer.SerializeListResult(request.MessageId, result.Items, result.TotalCount);
    }

    private byte[] HandleUpdate(ReadOnlySpan<byte> data)
    {
        var request = new ProjectUpdateRequest(data);
        var success = _projectService.Update(request.ProjectId, request.Name, request.Description);
        return ProjectProtocolSerializer.SerializeUpdateResult(request.MessageId, success);
    }

    private byte[] HandleDelete(ReadOnlySpan<byte> data)
    {
        var request = new ProjectDeleteRequest(data);
        var success = _projectService.Delete(request.ProjectId);
        return ProjectProtocolSerializer.SerializeDeleteResult(request.MessageId, success);
    }

    private byte[] HandleAddFolder(ReadOnlySpan<byte> data)
    {
        var request = new ProjectAddFolderRequest(data);
        var folder = _projectService.AddFolder(request.ProjectId, request.Name, request.ParentId);
        return ProjectProtocolSerializer.SerializeAddFolderResult(
            request.MessageId, folder != null, folder?.Id ?? Guid.Empty);
    }

    private byte[] HandleDeleteFolder(ReadOnlySpan<byte> data)
    {
        var request = new ProjectDeleteFolderRequest(data);
        var success = _projectService.DeleteFolder(request.ProjectId, request.FolderId);
        return ProjectProtocolSerializer.SerializeDeleteFolderResult(request.MessageId, success);
    }

    private byte[] HandleRenameFolder(ReadOnlySpan<byte> data)
    {
        var request = new ProjectRenameFolderRequest(data);
        var success = _projectService.RenameFolder(request.ProjectId, request.FolderId, request.Name);
        return ProjectProtocolSerializer.SerializeRenameFolderResult(request.MessageId, success);
    }

    private byte[] HandleAssignSession(ReadOnlySpan<byte> data)
    {
        var request = new ProjectAssignSessionRequest(data);
        var success = _projectService.AssignSession(request.SessionId, request.ProjectId, request.FolderId);
        return ProjectProtocolSerializer.SerializeAssignSessionResult(request.MessageId, success);
    }

    private byte[] HandleWriteUpMove(ReadOnlySpan<byte> data)
    {
        var request = new WriteUpMoveRequest(data);
        var success = _projectService.MoveWriteUp(request.WriteUpId, request.ProjectId, request.FolderId);
        return ProjectProtocolSerializer.SerializeWriteUpMoveResult(request.MessageId, success);
    }

    private byte[] HandleListSessions(ReadOnlySpan<byte> data)
    {
        var request = new ProjectListSessionsRequest(data);
        var result = _projectService.ListSessions(request.ProjectId, request.Offset, request.Limit);
        return ProjectProtocolSerializer.SerializeListSessionsResult(request.MessageId, result.Items, result.TotalCount);
    }

    private byte[] HandleListWriteUps(ReadOnlySpan<byte> data)
    {
        var request = new ProjectListWriteUpsRequest(data);
        var result = _projectService.ListWriteUps(request.FolderId, request.Offset, request.Limit);
        return ProjectProtocolSerializer.SerializeListWriteUpsResult(request.MessageId, result.Items, result.TotalCount);
    }

    private byte[] HandleExport(ReadOnlySpan<byte> data)
    {
        var request = new ProjectExportRequest(data);
        _projectService.ExportToFile(request.ProjectId, request.Path, request.Options);
        return ProjectProtocolSerializer.SerializeExportResult(request.MessageId, true);
    }

    private byte[] HandleImport(ReadOnlySpan<byte> data)
    {
        var request = new ProjectImportRequest(data);
        var projectId = _projectService.ImportFromFile(request.Path);
        return ProjectProtocolSerializer.SerializeImportResult(request.MessageId, true, projectId);
    }
}
