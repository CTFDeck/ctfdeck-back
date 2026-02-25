using CtfDeck.Abstractions.Ports.Projects;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Data.Db;
using CtfDeck.Data.PersistenceModels.Projects;

namespace CtfDeck.Data.Repositories.Projects;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly CtfDeckDbContext _context;
    private readonly object _lock = new();

    public ProjectRepository(CtfDeckDbContext context)
    {
        _context = context;
    }

    public ProjectDto Create(string name, string description)
    {
        var now = DateTime.UtcNow;
        var model = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            Folders = new List<ProjectFolder>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Report",
                    IsSystem = true
                }
            }
        };

        lock (_lock)
        {
            _context.Projects.Insert(model);
        }

        return ToDto(model);
    }

    public ProjectDto? GetById(Guid id)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(id);
            return model == null ? null : ToDto(model);
        }
    }

    public IEnumerable<ProjectMetadataDto> GetAllMetadata()
    {
        lock (_lock)
        {
            return _context.Projects
                .FindAll()
                .Select(p => new ProjectMetadataDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Description = p.Description ?? "",
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    FolderCount = p.Folders?.Count ?? 0
                })
                .ToList();
        }
    }

    public bool Update(Guid id, string name, string description)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(id);
            if (model == null) return false;

            model.Name = name;
            model.Description = description;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Projects.Update(model);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.Projects.Delete(id);
        }
    }

    public ProjectFolderDto? AddFolder(Guid projectId, string name)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(projectId);
            if (model == null) return null;

            var folder = new ProjectFolder
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsSystem = false
            };

            model.Folders ??= new List<ProjectFolder>();
            model.Folders.Add(folder);
            model.UpdatedAt = DateTime.UtcNow;

            _context.Projects.Update(model);

            return new ProjectFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                IsSystem = folder.IsSystem
            };
        }
    }

    public bool DeleteFolder(Guid projectId, Guid folderId)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(projectId);
            if (model == null) return false;

            model.Folders ??= new List<ProjectFolder>();
            var folder = model.Folders.Find(f => f.Id == folderId);
            if (folder == null) return false;
            if (folder.IsSystem) return false;

            model.Folders.Remove(folder);
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Projects.Update(model);
        }
    }

    public bool RenameFolder(Guid projectId, Guid folderId, string name)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(projectId);
            if (model == null) return false;

            model.Folders ??= new List<ProjectFolder>();
            var folder = model.Folders.Find(f => f.Id == folderId);
            if (folder == null) return false;
            if (folder.IsSystem) return false;

            folder.Name = name;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Projects.Update(model);
        }
    }

    public List<Guid> GetFolderIds(Guid projectId)
    {
        lock (_lock)
        {
            var model = _context.Projects.FindById(projectId);
            if (model == null) return new List<Guid>();

            return (model.Folders ?? new List<ProjectFolder>())
                .Select(f => f.Id)
                .ToList();
        }
    }

    private static ProjectDto ToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name ?? "",
        Description = p.Description ?? "",
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Folders = (p.Folders ?? new List<ProjectFolder>()).Select(f => new ProjectFolderDto
        {
            Id = f.Id,
            Name = f.Name ?? "",
            IsSystem = f.IsSystem
        }).ToList()
    };
}
