namespace CtfDeck.Contracts.Models.Tools;

public class ToolInstallProgressDto
{
    public required string ToolId { get; set; }
    public required ToolInstallState State { get; set; }
    public string? Message { get; set; }
    public double? ProgressPercent { get; set; }
    public string? InstalledPath { get; set; }
    public string? Error { get; set; }
}
