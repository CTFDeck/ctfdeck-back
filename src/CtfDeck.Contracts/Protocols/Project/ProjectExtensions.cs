using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Project;

public static class ProjectExtensions
{
    public static void WriteProject(this PooledBufferWriter writer, ProjectMetadataDto meta)
    {
        writer.WriteGuid(meta.Id);
        writer.WriteString(meta.Name ?? string.Empty);
        writer.WriteString(meta.Description ?? string.Empty);
        writer.WriteInt64(meta.CreatedAt.Ticks);
        writer.WriteInt64(meta.UpdatedAt.Ticks);
        writer.WriteInt32(meta.FolderCount);
    }

    public static void WriteProject(this PooledBufferWriter writer, ProjectDto project)
    {
        writer.WriteGuid(project.Id);
        writer.WriteString(project.Name ?? string.Empty);
        writer.WriteString(project.Description ?? string.Empty);
        writer.WriteInt64(project.CreatedAt.Ticks);
        writer.WriteInt64(project.UpdatedAt.Ticks);

        writer.WriteInt32(project.Folders.Count);
        foreach (var folder in project.Folders)
        {
            writer.WriteFolder(folder);
        }
    }

    public static void WriteFolder(this PooledBufferWriter writer, ProjectFolderDto folder)
    {
        writer.WriteGuid(folder.Id);
        writer.WriteGuid(folder.ParentId ?? Guid.Empty);
        writer.WriteString(folder.Name ?? string.Empty);
        writer.WriteByte((byte)(folder.IsSystem ? 1 : 0));
    }
}
