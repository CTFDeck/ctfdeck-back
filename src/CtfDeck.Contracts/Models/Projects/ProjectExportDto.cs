namespace CtfDeck.Contracts.Models.Projects;

public record ProjectExportMetadata(
    string Filename,
    long SizeBytes,
    int SessionCount,
    int WriteUpCount,
    DateTime ExportedAt,
    Guid ProjectId,
    bool IsAlreadyImported);

public sealed class ProjectExportDto
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public ProjectDto Project { get; set; } = null!;
    public List<Sessions.SessionDto> Sessions { get; set; } = new();
    public List<WriteUps.WriteUpDto> WriteUps { get; set; } = new();
    public List<MediaExportDto> Media { get; set; } = new();
    public List<Scripts.CustomScriptDto>? Scripts { get; set; }
}

public sealed record ExportOptions(
    bool IncludeHistory = true,
    bool IncludeTargets = true,
    bool IncludeWriteUps = true,
    bool IncludeMedia = true,
    bool IncludeScripts = true)
{
    public static ExportOptions All => new();

    public static ExportOptions FromFlags(byte flags) => new(
        IncludeHistory: (flags & 0x01) != 0,
        IncludeTargets: (flags & 0x02) != 0,
        IncludeWriteUps: (flags & 0x04) != 0,
        IncludeMedia: (flags & 0x08) != 0,
        IncludeScripts: (flags & 0x10) != 0);

    public byte ToFlags() =>
        (byte)((IncludeHistory ? 0x01 : 0) |
               (IncludeTargets ? 0x02 : 0) |
               (IncludeWriteUps ? 0x04 : 0) |
               (IncludeMedia ? 0x08 : 0) |
               (IncludeScripts ? 0x10 : 0));
}

public sealed class MediaExportDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string Data { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
