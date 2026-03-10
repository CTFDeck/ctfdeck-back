namespace CtfDeck.Contracts.Models.Tools;

public sealed class ToolDefinitionDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public string? CheckCommand { get; init; }
    public string? CheckArguments { get; init; }
    public required IReadOnlyCollection<ToolInstallerDto> Installers { get; init; }
}
