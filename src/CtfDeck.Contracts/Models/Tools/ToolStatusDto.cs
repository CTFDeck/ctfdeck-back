namespace CtfDeck.Contracts.Models.Tools;

public class ToolStatusDto
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required bool IsInstalled { get; set; }
    public required bool IsInstallable { get; set; }
    public string? InstalledPath { get; set; }
    public string? Version { get; set; }
    public string? Reason { get; set; }
}
