using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolDetector
{
    Task<IReadOnlyCollection<ToolStatusDto>> DetectAllAsync(CancellationToken cancellationToken = default);
    Task<ToolStatusDto?> DetectAsync(string toolId, CancellationToken cancellationToken = default);
}
