namespace CtfDeck.Contracts.Models.Tools;

public class ToolDefinition
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Category { get; set; }
    public required string Kind { get; set; }
    public required string Description { get; set; }
    public string? CommandTemplate { get; set; }
    public string? ExternalUrl { get; set; }
    public string? CheckCommand { get; set; }
    public string? CheckArguments { get; set; }
    public List<ToolInstaller> Installers { get; set; } = [];
}
