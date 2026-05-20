namespace CtfDeck.Contracts.Models.Tools;

public sealed class ToolInstallerDto
{
    public required string Os { get; init; }
    public required string Arch { get; init; }
    public required string Type { get; init; }
    public string? Url { get; init; }
    public string? ArchiveType { get; init; }
    public string? ExecutableRelativePath { get; init; }
    public string? ExecutableName { get; init; }
    public string? PackageManager { get; init; }
    public string? PackageId { get; init; }
    public string? InstallArgs { get; init; }
    public bool? RequiresElevation { get; init; }
    public string? Version { get; init; }
    public string? Sha256 { get; init; }
}
