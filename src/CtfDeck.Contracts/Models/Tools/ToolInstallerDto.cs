namespace CtfDeck.Contracts.Models.Tools;

public sealed class ToolInstallerDto
{
    public required string Os { get; init; }
    public required string Arch { get; init; }
    public required string Type { get; init; }
    public required string Url { get; init; }
    public required string ArchiveType { get; init; }
    public required string ExecutableRelativePath { get; init; }
    public required string ExecutableName { get; init; }
    public string? Version { get; init; }
    public string? Sha256 { get; init; }
}
