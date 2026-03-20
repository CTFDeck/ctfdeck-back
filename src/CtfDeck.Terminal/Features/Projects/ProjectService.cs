using System.Text.Json;
using System.Text.RegularExpressions;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Abstractions.Ports.Projects;
using CtfDeck.Abstractions.Ports.Scripts;
using CtfDeck.Abstractions.Ports.Sessions;
using CtfDeck.Abstractions.Ports.WriteUps;
using CtfDeck.Abstractions.Ports.Media;
using System.IO;

namespace CtfDeck.Terminal.Features.Projects;

public class ProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IWriteUpRepository _writeUpRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly ICustomScriptRepository _customScriptRepository;

    private static readonly Regex FilenameSanitizationRegex = new(@"[^a-zA-Z0-9\-_]", RegexOptions.Compiled);

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
        IMediaRepository mediaRepository,
        ICustomScriptRepository customScriptRepository)
    {
        _projectRepository = projectRepository;
        _sessionRepository = sessionRepository;
        _writeUpRepository = writeUpRepository;
        _mediaRepository = mediaRepository;
        _customScriptRepository = customScriptRepository;

        if (!Directory.Exists(ExportsPath))
        {
            Directory.CreateDirectory(ExportsPath);
        }
    }

    private static string ExportsPath => Path.Combine(AppContext.BaseDirectory, "exports");

    public IEnumerable<ProjectExportMetadata> GetAvailableExports()
    {
        if (!Directory.Exists(ExportsPath)) return Enumerable.Empty<ProjectExportMetadata>();
        
        var files = Directory.GetFiles(ExportsPath, "*.json");
        var results = new List<ProjectExportMetadata>();

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                
                var root = doc.RootElement;
                var exportedAt = (root.TryGetProperty("exportedAt", out var exportedAtProp) || root.TryGetProperty("ExportedAt", out exportedAtProp))
                    ? exportedAtProp.GetDateTime() 
                    : info.LastWriteTimeUtc;

                var sessions = root.TryGetProperty("sessions", out var sessionsProp) || root.TryGetProperty("Sessions", out sessionsProp)
                    ? sessionsProp.GetArrayLength() 
                    : 0;

                var writeUps = root.TryGetProperty("writeUps", out var writeUpsProp) || root.TryGetProperty("WriteUps", out writeUpsProp)
                    ? writeUpsProp.GetArrayLength() 
                    : 0;

                results.Add(new ProjectExportMetadata(
                    info.Name,
                    info.Length,
                    sessions,
                    writeUps,
                    exportedAt));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectService] Failed to read metadata for {file}: {ex.Message}");
            }
        }

        return results.OrderByDescending(x => x.ExportedAt);
    }

    private string NormalizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return "export.json";

        // Remove extension if present to normalize the base name
        var baseName = Path.GetFileNameWithoutExtension(filename);
        
        // Replace invalid characters with underscore
        // Allowed: a-z, A-Z, 0-9, -, _
        baseName = FilenameSanitizationRegex.Replace(baseName, "_");
        
        return baseName + ".json";
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
        // 1. Delete associated sessions
        var sessions = _sessionRepository.GetByProjectId(id, 0, int.MaxValue);
        foreach (var session in sessions.Items)
        {
            _sessionRepository.Delete(session.Id);
        }

        // 2. Delete associated write-ups (using folder-based and project-based cleanup)
        // Note: Repository might have a more direct way but we use what's available
        var folderIds = _projectRepository.GetFolderIds(id);
        foreach (var folderId in folderIds)
        {
            var folderWriteUps = _writeUpRepository.GetByFolderId(folderId, 0, int.MaxValue);
            foreach (var wu in folderWriteUps.Items)
            {
                _writeUpRepository.Delete(wu.Id);
            }
        }

        // Also check for unassigned writeups in this project
        var unassignedWriteUps = _writeUpRepository.GetAllMetadata(0, int.MaxValue, false);
        foreach (var wu in unassignedWriteUps.Items.Where(x => x.ProjectId == id))
        {
            _writeUpRepository.Delete(wu.Id);
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

    public void ExportToFile(Guid projectId, string filePath, ExportOptions options)
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

        if (!options.IncludeHistory)
        {
            foreach (var session in sessions)
                session.History = new List<HistoryEntryDto>();
        }

        if (!options.IncludeTargets)
        {
            foreach (var session in sessions)
                session.Targets = new List<SessionTargetDto>();
        }

        var writeUps = new List<WriteUpDto>();
        var media = new List<MediaExportDto>();

        if (options.IncludeWriteUps)
        {
            var writeUpIds = new HashSet<Guid>();

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

            if (options.IncludeMedia)
            {
                var mediaIds = new HashSet<Guid>();
                foreach (var writeUp in writeUps)
                {
                    foreach (Match match in MediaRegex.Matches(writeUp.Content))
                    {
                        if (Guid.TryParse(match.Groups[1].Value, out var mediaId))
                            mediaIds.Add(mediaId);
                    }
                }

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
            }
        }

        var scripts = options.IncludeScripts
            ? _customScriptRepository.GetAll()
            : null;

        var export = new ProjectExportDto
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Project = project,
            Sessions = sessions,
            WriteUps = writeUps,
            Media = media,
            Scripts = scripts
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        var normalized = NormalizeFilename(filePath);
        var finalPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(ExportsPath, normalized);

        var dir = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(finalPath, json);
    }

    public Guid ImportFromFile(string filePath)
    {
        var normalized = NormalizeFilename(filePath);
        var finalPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(ExportsPath, normalized);

        if (!File.Exists(finalPath))
            throw new InvalidOperationException("File not found: " + finalPath);

        var json = File.ReadAllText(finalPath);
        var export = JsonSerializer.Deserialize<ProjectExportDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid export file");

        if (export.Version != 1)
            throw new InvalidOperationException($"Unsupported export version: {export.Version}");

        if (_projectRepository.GetById(export.Project.Id) != null)
        {
            Delete(export.Project.Id);
        }

        foreach (var session in export.Sessions)
        {
            if (_sessionRepository.GetById(session.Id) != null)
                _sessionRepository.Delete(session.Id);
        }

        foreach (var writeUp in export.WriteUps)
        {
            if (_writeUpRepository.GetById(writeUp.Id) != null)
                _writeUpRepository.Delete(writeUp.Id);
        }

        foreach (var media in export.Media)
        {
            if (_mediaRepository.GetById(media.Id) != null)
                _mediaRepository.Delete(media.Id);
        }

        try
        {
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

            if (export.Scripts != null)
            {
                foreach (var script in export.Scripts)
                {
                    _customScriptRepository.Insert(script);
                }
            }

            return export.Project.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectService] Import failed for {filePath}: {ex.Message}");
            throw;
        }
    }
}
