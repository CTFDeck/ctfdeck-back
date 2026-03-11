namespace CtfDeck.Contracts.Models.Tools;

public sealed class ToolInventorySnapshotDto
{
    public required IReadOnlyCollection<ToolStatusDto> Tools { get; init; }
}
