namespace CtfDeck.Contracts.Models.Tools;

public sealed class ToolInstallRequestDto
{
    public required IReadOnlyCollection<string> ToolIds { get; init; }
}
