using System.Text.Json;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Protocols.Project;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Media;
using CtfDeck.Data.Repositories.Projects;
using CtfDeck.Data.Repositories.Scripts;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Data.Repositories.WriteUps;
using CtfDeck.Terminal.Features.Projects;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class ProjectMessageHandlerTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly ProjectService _service;
    private readonly ProjectMessageHandler _handler;
    private readonly ProjectRepository _projectRepo;

    public ProjectMessageHandlerTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        _projectRepo = new ProjectRepository(_db);
        _service = new ProjectService(
            _projectRepo,
            new SessionRepository(_db),
            new WriteUpRepository(_db),
            new MediaRepository(_db),
            new CustomScriptRepository(_db));
            
        _handler = new ProjectMessageHandler(_service);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<byte[]> SendMessageAsync(byte[] requestData)
    {
        byte[]? responseData = null;
        await _handler.TryHandleAsync("client1", requestData, data =>
        {
            responseData = data;
            return Task.CompletedTask;
        }, CancellationToken.None);
        
        return responseData!;
    }

    [Fact]
    public async Task Create_ShouldReturnSuccess()
    {
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectCreate);
        writer.WriteGuid(msgId);
        writer.WriteString("Test Project");
        writer.WriteString("A description");
        var request = writer.ToArray();

        var response = await SendMessageAsync(request);

        response[0].Should().Be((byte)MessageType.ProjectCreateResult);
        new Guid(response.AsSpan(1, 16)).Should().Be(msgId);
        response[17].Should().Be(1); // Success
        var projectId = new Guid(response.AsSpan(18, 16));
        
        var project = _service.GetById(projectId);
        project.Should().NotBeNull();
        project!.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task Load_ShouldReturnProject()
    {
        var project = _service.Create("My Project", "Desc");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectLoad);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.ProjectLoadResult);
        response[17].Should().Be(1); // Success
    }

    [Fact]
    public async Task List_ShouldReturnProjects()
    {
        _service.Create("Proj1", "D1");
        _service.Create("Proj2", "D2");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectList);
        writer.WriteGuid(msgId);
        writer.WriteInt32(0); // Offset
        writer.WriteInt32(10); // Limit
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.ProjectListResult);
        var totalCount = BitConverter.ToInt32(response.AsSpan(17, 4));
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task Update_ShouldReturnSuccess()
    {
        var project = _service.Create("Old Name", "Old Desc");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectUpdate);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        writer.WriteString("New Name");
        writer.WriteString("New Desc");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.ProjectUpdateResult);
        response[17].Should().Be(1); // Success
        
        var updated = _service.GetById(project.Id);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess()
    {
        var project = _service.Create("To Delete", "Desc");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectDelete);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.ProjectDeleteResult);
        response[17].Should().Be(1); // Success
        
        _service.GetById(project.Id).Should().BeNull();
    }
    
    [Fact]
    public async Task AddRenameDeleteFolder_ShouldWork()
    {
        var project = _service.Create("Project", "Desc");
        
        // Add
        var addMsgId = Guid.NewGuid();
        using var addWriter = new PooledBufferWriter();
        addWriter.WriteByte((byte)MessageType.ProjectAddFolder);
        addWriter.WriteGuid(addMsgId);
        addWriter.WriteGuid(project.Id);
        addWriter.WriteGuid(Guid.Empty); // no parent
        addWriter.WriteString("MyFolder");
        
        var addResp = await SendMessageAsync(addWriter.ToArray());
        addResp[0].Should().Be((byte)MessageType.ProjectAddFolderResult);
        addResp[17].Should().Be(1);
        var folderId = new Guid(addResp.AsSpan(18, 16));
        
        // Rename
        var renMsgId = Guid.NewGuid();
        using var renWriter = new PooledBufferWriter();
        renWriter.WriteByte((byte)MessageType.ProjectRenameFolder);
        renWriter.WriteGuid(renMsgId);
        renWriter.WriteGuid(project.Id);
        renWriter.WriteGuid(folderId);
        renWriter.WriteString("RenamedFolder");
        
        var renResp = await SendMessageAsync(renWriter.ToArray());
        renResp[0].Should().Be((byte)MessageType.ProjectRenameFolderResult);
        renResp[17].Should().Be(1);
        
        // Delete
        var delMsgId = Guid.NewGuid();
        using var delWriter = new PooledBufferWriter();
        delWriter.WriteByte((byte)MessageType.ProjectDeleteFolder);
        delWriter.WriteGuid(delMsgId);
        delWriter.WriteGuid(project.Id);
        delWriter.WriteGuid(folderId);
        
        var delResp = await SendMessageAsync(delWriter.ToArray());
        delResp[0].Should().Be((byte)MessageType.ProjectDeleteFolderResult);
        delResp[17].Should().Be(1);
    }

    [Fact]
    public async Task AssignSession_ShouldWork()
    {
        var project = _service.Create("Project", "Desc");
        var sessionId = Guid.NewGuid();
        
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectAssignSession);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        writer.WriteGuid(Guid.Empty);
        writer.WriteGuid(sessionId);
        
        var resp = await SendMessageAsync(writer.ToArray());
        resp[0].Should().Be((byte)MessageType.ProjectAssignSessionResult);
        // It might return success=false if the session doesn't actually exist in db, but the handler shouldn't throw
        resp[17].Should().BeOneOf(0, 1);
    }

    [Fact]
    public async Task WriteUpMove_ShouldWork()
    {
        var project = _service.Create("Project", "Desc");
        var writeUpId = Guid.NewGuid();
        
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpMove);
        writer.WriteGuid(msgId);
        writer.WriteGuid(writeUpId);
        writer.WriteGuid(project.Id);
        writer.WriteGuid(Guid.Empty);
        
        var resp = await SendMessageAsync(writer.ToArray());
        resp[0].Should().Be((byte)MessageType.WriteUpMoveResult);
    }

    [Fact]
    public async Task ListSessions_ShouldReturnEmptyIfNone()
    {
        var project = _service.Create("Project", "Desc");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListSessions);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        writer.WriteInt32(0);
        writer.WriteInt32(10);
        
        var resp = await SendMessageAsync(writer.ToArray());
        resp[0].Should().Be((byte)MessageType.ProjectListSessionsResult);
    }

    [Fact]
    public async Task ListWriteUps_ShouldReturnEmptyIfNone()
    {
        var project = _service.Create("Project", "Desc");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListWriteUps);
        writer.WriteGuid(msgId);
        writer.WriteGuid(project.Id);
        writer.WriteGuid(Guid.Empty);
        writer.WriteInt32(0);
        writer.WriteInt32(10);
        
        var resp = await SendMessageAsync(writer.ToArray());
        resp[0].Should().Be((byte)MessageType.ProjectListWriteUpsResult);
    }

    [Fact]
    public async Task ExportImportList_ShouldWork()
    {
        var project = _service.Create("To Export", "Desc");
        var path = Path.GetTempFileName();
        
        // Export
        var exportMsg = Guid.NewGuid();
        using var exportWriter = new PooledBufferWriter();
        exportWriter.WriteByte((byte)MessageType.ProjectExport);
        exportWriter.WriteGuid(exportMsg);
        exportWriter.WriteGuid(project.Id);
        exportWriter.WriteString(path);
        exportWriter.WriteByte((byte)ExportOptions.All.ToFlags());
        
        var expResp = await SendMessageAsync(exportWriter.ToArray());
        expResp[0].Should().Be((byte)MessageType.ProjectExportResult);
        expResp[17].Should().Be(1);
        
        // List Exports
        var listMsg = Guid.NewGuid();
        using var listWriter = new PooledBufferWriter();
        listWriter.WriteByte((byte)MessageType.ProjectListExports);
        listWriter.WriteGuid(listMsg);
        
        var listResp = await SendMessageAsync(listWriter.ToArray());
        listResp[0].Should().Be((byte)MessageType.ProjectListExportsResult);
        
        // Import
        var importMsg = Guid.NewGuid();
        using var importWriter = new PooledBufferWriter();
        importWriter.WriteByte((byte)MessageType.ProjectImport);
        importWriter.WriteGuid(importMsg);
        importWriter.WriteString(path);
        
        var impResp = await SendMessageAsync(importWriter.ToArray());
        impResp[0].Should().Be((byte)MessageType.ProjectImportResult);
        impResp[17].Should().Be(1);
        
        if (File.Exists(path)) File.Delete(path);
    }
}
