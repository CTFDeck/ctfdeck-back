using System.Text.Json;
using System.Text.RegularExpressions;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Abstractions.Ports.Projects;
using CtfDeck.Abstractions.Ports.Sessions;
using CtfDeck.Abstractions.Ports.WriteUps;
using CtfDeck.Abstractions.Ports.Media;

namespace CtfDeck.Terminal.Features.Projects;

public class ProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IWriteUpRepository _writeUpRepository;
    private readonly IMediaRepository _mediaRepository;

    private static readonly Regex MediaRegex = new(@"media://([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProjectService(
        IProjectRepository projectRepository,
        ISessionRepository sessionRepository,
        IWriteUpRepository writeUpRepository,
        IMediaRepository mediaRepository)
    {
        _projectRepository = projectRepository;
        _sessionRepository = sessionRepository;
        _writeUpRepository = writeUpRepository;
        _mediaRepository = mediaRepository;
    }

    public ProjectDto Create(string name, string description)
        => _projectRepository.Create(name, description);

    public ProjectDto? GetById(Guid id)
        => _projectRepository.GetById(id);

    public (IEnumerable<ProjectMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50)
    {
        var result = _projectRepository.GetAllMetadata(offset, limit);
        var projects = result.Items.ToList();

        foreach (var project in projects)
        {
            project.SessionCount = _sessionRepository.GetByProjectId(project.Id).TotalCount;
        }

        return (projects, result.TotalCount);
    }

    public bool Update(Guid id, string name, string description)
        => _projectRepository.Update(id, name, description);

    public bool Delete(Guid id)
    {
        var folderIds = _projectRepository.GetFolderIds(id);

        _sessionRepository.ClearProjectId(id);
        _writeUpRepository.ClearProjectId(id);

        foreach (var folderId in folderIds)
        {
            _writeUpRepository.ClearFolderId(folderId);
        }

        return _projectRepository.Delete(id);
    }

    public ProjectFolderDto? AddFolder(Guid projectId, string name, Guid? parentId = null)
        => _projectRepository.AddFolder(projectId, name, parentId);

    public bool DeleteFolder(Guid projectId, Guid folderId)
    {
        _writeUpRepository.ClearFolderId(folderId);
        return _projectRepository.DeleteFolder(projectId, folderId);
    }

    public bool RenameFolder(Guid projectId, Guid folderId, string name)
        => _projectRepository.RenameFolder(projectId, folderId, name);

    public bool AssignSession(Guid sessionId, Guid projectId, Guid folderId)
    {
        var actualProjectId = projectId == Guid.Empty ? (Guid?)null : projectId;
        var actualFolderId = folderId == Guid.Empty ? (Guid?)null : folderId;
        return _sessionRepository.SetProjectAndFolderId(sessionId, actualProjectId, actualFolderId);
    }

    public bool MoveWriteUp(Guid writeUpId, Guid projectId, Guid folderId)
    {
        var actualProjectId = projectId == Guid.Empty ? (Guid?)null : projectId;
        var actualFolderId = folderId == Guid.Empty ? (Guid?)null : folderId;
        return _writeUpRepository.SetProjectAndFolderId(writeUpId, actualProjectId, actualFolderId);
    }

    public (List<SessionMetadataDto> Items, int TotalCount) ListSessions(Guid projectId, int offset = 0, int limit = 50)
    {
        var result = _sessionRepository.GetByProjectId(projectId, offset, limit);
        return (result.Items.ToList(), result.TotalCount);
    }

    public (List<WriteUpMetadataDto> Items, int TotalCount) ListWriteUps(Guid folderId, int offset = 0, int limit = 50)
    {
        var result = _writeUpRepository.GetByFolderId(folderId, offset, limit);
        return (result.Items.ToList(), result.TotalCount);
    }

    public void ExportToFile(Guid projectId, string filePath)
    {
        var project = _projectRepository.GetById(projectId)
            ?? throw new InvalidOperationException("Project not found");

        var sessionResult = _sessionRepository.GetByProjectId(projectId, 0, int.MaxValue);
        var sessionMetadata = sessionResult.Items.ToList();
        var sessions = sessionMetadata
            .Select(m => _sessionRepository.GetById(m.Id))
            .Where(s => s != null)
            .Cast<SessionDto>()
            .ToList();

        var writeUpIds = new HashSet<Guid>();
        var writeUps = new List<WriteUpDto>();

        // Collect writeups from project folders
        foreach (var folder in project.Folders)
        {
            var folderWriteUpsResult = _writeUpRepository.GetByFolderId(folder.Id, 0, int.MaxValue);
            foreach (var meta in folderWriteUpsResult.Items)
            {
                if (writeUpIds.Add(meta.Id))
                {
                    var writeUp = _writeUpRepository.GetById(meta.Id);
                    if (writeUp != null)
                        writeUps.Add(writeUp);
                }
            }
        }

        // Collect writeups from project sessions (even if not in a folder)
        foreach (var session in sessions)
        {
            var sessionWriteUpsResult = _writeUpRepository.GetBySessionId(session.Id, 0, int.MaxValue);
            foreach (var meta in sessionWriteUpsResult.Items)
            {
                if (writeUpIds.Add(meta.Id))
                {
                    var writeUp = _writeUpRepository.GetById(meta.Id);
                    if (writeUp != null)
                        writeUps.Add(writeUp);
                }
            }
        }

        var mediaIds = new HashSet<Guid>();
        foreach (var writeUp in writeUps)
        {
            foreach (Match match in MediaRegex.Matches(writeUp.Content))
            {
                if (Guid.TryParse(match.Groups[1].Value, out var mediaId))
                    mediaIds.Add(mediaId);
            }
        }

        var media = new List<MediaExportDto>();
        foreach (var mediaId in mediaIds)
        {
            var mediaDto = _mediaRepository.GetById(mediaId);
            if (mediaDto != null)
            {
                media.Add(new MediaExportDto
                {
                    Id = mediaDto.Id,
                    FileName = mediaDto.FileName,
                    MimeType = mediaDto.MimeType,
                    Data = Convert.ToBase64String(mediaDto.Data),
                    CreatedAt = mediaDto.CreatedAt
                });
            }
        }

        var export = new ProjectExportDto
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Project = project,
            Sessions = sessions,
            WriteUps = writeUps,
            Media = media
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public Guid ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new InvalidOperationException("File not found");

        var json = File.ReadAllText(filePath);
        var export = JsonSerializer.Deserialize<ProjectExportDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid export file");

        if (export.Version != 1)
            throw new InvalidOperationException($"Unsupported export version: {export.Version}");

        if (_projectRepository.GetById(export.Project.Id) != null)
            throw new InvalidOperationException("Project already exists");

        foreach (var session in export.Sessions)
        {
            if (_sessionRepository.GetById(session.Id) != null)
                throw new InvalidOperationException($"Session {session.Id} already exists");
        }

        foreach (var writeUp in export.WriteUps)
        {
            if (_writeUpRepository.GetById(writeUp.Id) != null)
                throw new InvalidOperationException($"WriteUp {writeUp.Id} already exists");
        }

        foreach (var media in export.Media)
        {
            if (_mediaRepository.GetById(media.Id) != null)
                throw new InvalidOperationException($"Media {media.Id} already exists");
        }

        _projectRepository.Insert(export.Project);

        foreach (var session in export.Sessions)
        {
            _sessionRepository.Insert(session);
        }

        foreach (var writeUp in export.WriteUps)
        {
            _writeUpRepository.Insert(writeUp);
        }

        foreach (var media in export.Media)
        {
            var mediaDto = new MediaDto
            {
                Id = media.Id,
                FileName = media.FileName,
                MimeType = media.MimeType,
                Data = Convert.FromBase64String(media.Data),
                CreatedAt = media.CreatedAt
            };
            _mediaRepository.Insert(mediaDto);
        }

        return export.Project.Id;
    }
}
