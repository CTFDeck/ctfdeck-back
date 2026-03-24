using System.Text;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Protocols.Project;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.Project;

public class ProjectProtocolTests
{
    [Fact]
    public void ProjectCreateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var name = "My CTF Project";
        var description = "A project for HackTheBox";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectCreate);
        writer.WriteGuid(messageId);
        writer.WriteString(name);
        writer.WriteString(description);
        var data = writer.ToArray();

        var request = new ProjectCreateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Name.Should().Be(name);
        request.Description.Should().Be(description);
    }

    [Fact]
    public void ProjectLoadRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectLoad);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        var data = writer.ToArray();

        var request = new ProjectLoadRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ProjectListRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var offset = 10;
        var limit = 20;

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectList);
        writer.WriteGuid(messageId);
        writer.WriteInt32(offset);
        writer.WriteInt32(limit);
        var data = writer.ToArray();

        var request = new ProjectListRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Offset.Should().Be(offset);
        request.Limit.Should().Be(limit);
    }

    [Fact]
    public void ProjectUpdateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var name = "Updated Project";
        var description = "Updated description";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectUpdate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteString(name);
        writer.WriteString(description);
        var data = writer.ToArray();

        var request = new ProjectUpdateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.Name.Should().Be(name);
        request.Description.Should().Be(description);
    }

    [Fact]
    public void ProjectDeleteRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectDelete);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        var data = writer.ToArray();

        var request = new ProjectDeleteRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ProjectAddFolderRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var name = "Exploits";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectAddFolder);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(parentId);
        writer.WriteString(name);
        var data = writer.ToArray();

        var request = new ProjectAddFolderRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.ParentId.Should().Be(parentId);
        request.Name.Should().Be(name);
    }

    [Fact]
    public void ProjectDeleteFolderRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectDeleteFolder);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(folderId);
        var data = writer.ToArray();

        var request = new ProjectDeleteFolderRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(folderId);
    }

    [Fact]
    public void ProjectRenameFolderRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var name = "Renamed Folder";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectRenameFolder);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(folderId);
        writer.WriteString(name);
        var data = writer.ToArray();

        var request = new ProjectRenameFolderRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(folderId);
        request.Name.Should().Be(name);
    }

    [Fact]
    public void ProjectAssignSessionRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectAssignSession);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(sessionId);
        writer.WriteGuid(folderId);
        var data = writer.ToArray();

        var request = new ProjectAssignSessionRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(folderId);
        request.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void WriteUpMoveRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpMove);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(folderId);
        var data = writer.ToArray();

        var request = new WriteUpMoveRequest(data);

        request.MessageId.Should().Be(messageId);
        request.WriteUpId.Should().Be(writeUpId);
        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(folderId);
    }

    [Fact]
    public void WriteUpMoveRequest_WithEmptyGuid_ShouldDeserializeAsUnlink()
    {
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpMove);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(Guid.Empty);
        var data = writer.ToArray();

        var request = new WriteUpMoveRequest(data);

        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void SerializeCreateResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var bytes = ProjectProtocolSerializer.SerializeCreateResult(messageId, true, projectId);

        bytes[0].Should().Be((byte)MessageType.ProjectCreateResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
        var resultProjectId = new Guid(bytes.AsSpan(18, 16));
        resultProjectId.Should().Be(projectId);
    }

    [Fact]
    public void SerializeAddFolderResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var bytes = ProjectProtocolSerializer.SerializeAddFolderResult(messageId, true, folderId);

        bytes[0].Should().Be((byte)MessageType.ProjectAddFolderResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
        var resultFolderId = new Guid(bytes.AsSpan(18, 16));
        resultFolderId.Should().Be(folderId);
    }

    [Fact]
    public void SerializeListResult_ShouldSerializeMultipleEntries()
    {
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var projects = new List<ProjectMetadataDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Project 1", Description = "Desc 1", CreatedAt = now, UpdatedAt = now, FolderCount = 2, SessionCount = 3 },
            new() { Id = Guid.NewGuid(), Name = "Project 2", Description = "Desc 2", CreatedAt = now, UpdatedAt = now, FolderCount = 1, SessionCount = 0 }
        };

        var bytes = ProjectProtocolSerializer.SerializeListResult(messageId, projects, 2);

        bytes[0].Should().Be((byte)MessageType.ProjectListResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        var totalCount = BitConverter.ToInt32(bytes.AsSpan(17, 4));
        totalCount.Should().Be(2);
        var count = BitConverter.ToInt32(bytes.AsSpan(21, 4));
        count.Should().Be(2);
    }

    [Fact]
    public void SerializeLoadResult_Success_ShouldContainFullData()
    {
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var project = new ProjectDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            Description = "Test Desc",
            CreatedAt = now,
            UpdatedAt = now,
            Folders = new List<ProjectFolderDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Report", IsSystem = true },
                new() { Id = Guid.NewGuid(), Name = "Custom", IsSystem = false }
            }
        };

        var bytes = ProjectProtocolSerializer.SerializeLoadResult(messageId, true, project);

        bytes[0].Should().Be((byte)MessageType.ProjectLoadResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
        var resultId = new Guid(bytes.AsSpan(18, 16));
        resultId.Should().Be(project.Id);
    }

    [Fact]
    public void SerializeLoadResult_NotFound_ShouldBeMinimal()
    {
        var messageId = Guid.NewGuid();

        var bytes = ProjectProtocolSerializer.SerializeLoadResult(messageId, false, null);

        bytes[0].Should().Be((byte)MessageType.ProjectLoadResult);
        bytes[17].Should().Be(0);
        bytes.Length.Should().Be(18);
    }

    [Fact]
    public void IsProjectMessage_ShouldDetectCorrectRange()
    {
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectCreate).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectLoad).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectList).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectUpdate).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectDelete).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectAddFolder).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectDeleteFolder).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectRenameFolder).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectAssignSession).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.WriteUpMove).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectListSessions).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectListWriteUps).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectExport).Should().BeTrue();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.ProjectImport).Should().BeTrue();

        ProjectProtocolDeserializer.IsProjectMessage(MessageType.SessionCreate).Should().BeFalse();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.WriteUpCreate).Should().BeFalse();
        ProjectProtocolDeserializer.IsProjectMessage(MessageType.MediaUpload).Should().BeFalse();
    }

    [Fact]
    public void ProjectListSessionsRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var offset = 10;
        var limit = 20;

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListSessions);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteInt32(offset);
        writer.WriteInt32(limit);
        var data = writer.ToArray();

        var request = new ProjectListSessionsRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.Offset.Should().Be(offset);
        request.Limit.Should().Be(limit);
    }

    [Fact]
    public void ProjectListWriteUpsRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var offset = 10;
        var limit = 20;

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListWriteUps);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteGuid(folderId);
        writer.WriteInt32(offset);
        writer.WriteInt32(limit);
        var data = writer.ToArray();

        var request = new ProjectListWriteUpsRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.FolderId.Should().Be(folderId);
        request.Offset.Should().Be(offset);
        request.Limit.Should().Be(limit);
    }

    [Fact]
    public void SerializeListSessionsResult_ShouldSerializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var sessions = new List<SessionMetadataDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Session 1", Description = "Desc", CreatedAt = now, UpdatedAt = now, HistoryCount = 5, TargetCount = 2, ProjectId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "Session 2", Description = "", CreatedAt = now, UpdatedAt = now, HistoryCount = 0, TargetCount = 0, ProjectId = null }
        };

        var bytes = ProjectProtocolSerializer.SerializeListSessionsResult(messageId, sessions, 2);

        bytes[0].Should().Be((byte)MessageType.ProjectListSessionsResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        var totalCount = BitConverter.ToInt32(bytes.AsSpan(17, 4));
        totalCount.Should().Be(2);
        var count = BitConverter.ToInt32(bytes.AsSpan(21, 4));
        count.Should().Be(2);
    }

    [Fact]
    public void ProjectExportRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var path = @"C:\Users\test\export.json";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectExport);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteString(path);
        writer.WriteByte(0x1F);
        var data = writer.ToArray();

        var request = new ProjectExportRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ProjectId.Should().Be(projectId);
        request.Path.Should().Be(path);
        request.Options.Should().Be(ExportOptions.All);
    }

    [Fact]
    public void ProjectExportRequest_WithoutFlagsByte_ShouldDefaultToAll()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var path = @"C:\export.json";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectExport);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteString(path);
        var data = writer.ToArray();

        var request = new ProjectExportRequest(data);

        request.Options.Should().Be(ExportOptions.All);
    }

    [Fact]
    public void ProjectExportRequest_WithPartialFlags_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var path = "out.json";
        byte flags = 0x04 | 0x08; // writeups + media only

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectExport);
        writer.WriteGuid(messageId);
        writer.WriteGuid(projectId);
        writer.WriteString(path);
        writer.WriteByte(flags);
        var data = writer.ToArray();

        var request = new ProjectExportRequest(data);

        request.Options.IncludeHistory.Should().BeFalse();
        request.Options.IncludeTargets.Should().BeFalse();
        request.Options.IncludeWriteUps.Should().BeTrue();
        request.Options.IncludeMedia.Should().BeTrue();
        request.Options.IncludeScripts.Should().BeFalse();
    }

    [Fact]
    public void ExportOptions_FromFlags_ToFlags_ShouldRoundTrip()
    {
        for (byte flags = 0; flags <= 0x1F; flags++)
        {
            var options = ExportOptions.FromFlags(flags);
            options.ToFlags().Should().Be(flags);
        }
    }

    [Fact]
    public void ProjectImportRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var path = @"C:\Users\test\export.json";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectImport);
        writer.WriteGuid(messageId);
        writer.WriteString(path);
        var data = writer.ToArray();

        var request = new ProjectImportRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Path.Should().Be(path);
    }

    [Fact]
    public void SerializeExportResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();

        var bytes = ProjectProtocolSerializer.SerializeExportResult(messageId, true);

        bytes[0].Should().Be((byte)MessageType.ProjectExportResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
    }

    [Fact]
    public void SerializeImportResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var bytes = ProjectProtocolSerializer.SerializeImportResult(messageId, true, projectId);

        bytes[0].Should().Be((byte)MessageType.ProjectImportResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
        var resultProjectId = new Guid(bytes.AsSpan(18, 16));
        resultProjectId.Should().Be(projectId);
    }

    [Fact]
    public void SerializeListWriteUpsResult_ShouldSerializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var writeUps = new List<WriteUpMetadataDto>
        {
            new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), FolderId = Guid.NewGuid(), Name = "WriteUp 1", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), FolderId = null, Name = "WriteUp 2", CreatedAt = now, UpdatedAt = now }
        };

        var bytes = ProjectProtocolSerializer.SerializeListWriteUpsResult(messageId, writeUps, 2);

        bytes[0].Should().Be((byte)MessageType.ProjectListWriteUpsResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        var totalCount = BitConverter.ToInt32(bytes.AsSpan(17, 4));
        totalCount.Should().Be(2);
        var count = BitConverter.ToInt32(bytes.AsSpan(21, 4));
        count.Should().Be(2);
    }

    [Fact]
    public void ProjectListExportsRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ProjectListExports);
        writer.WriteGuid(messageId);
        var data = writer.ToArray();

        var request = new ProjectListExportsRequest(data);

        request.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void SerializeListExportsResult_ShouldSerializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var exports = new List<ProjectExportMetadata>
        {
            new("export1.json", 1024, 1, 2, DateTime.UtcNow, Guid.NewGuid(), true)
        };

        var bytes = ProjectProtocolSerializer.SerializeListExportsResult(messageId, exports);

        bytes[0].Should().Be((byte)MessageType.ProjectListExportsResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        BitConverter.ToInt32(bytes.AsSpan(17, 4)).Should().Be(1);
    }
}
