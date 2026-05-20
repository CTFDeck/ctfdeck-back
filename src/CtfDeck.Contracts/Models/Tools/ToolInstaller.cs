namespace CtfDeck.Contracts.Models.Tools;

public class ToolInstaller
{
    public required string Os { get; set; }
    public required string Arch { get; set; }
    public required string Type { get; set; }
    public string? Url { get; set; }
    public string? ArchiveType { get; set; }
    public string? ExecutableRelativePath { get; set; }
    public string? ExecutableName { get; set; }
    public string? PackageManager { get; set; }
    public string? PackageId { get; set; }
    public string? InstallArgs { get; set; }
    public bool? RequiresElevation { get; set; }
    public string? Version { get; set; }
    public string? Sha256 { get; set; }
}
