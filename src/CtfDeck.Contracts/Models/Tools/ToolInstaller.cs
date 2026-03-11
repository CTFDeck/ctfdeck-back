namespace CtfDeck.Contracts.Models.Tools;

public class ToolInstaller
{
    public required string Os { get; set; }
    public required string Arch { get; set; }
    public required string Type { get; set; }
    public required string Url { get; set; }
    public required string ArchiveType { get; set; }
    public required string ExecutableRelativePath { get; set; }
    public required string ExecutableName { get; set; }
    public string? Version { get; set; }
    public string? Sha256 { get; set; }
}
