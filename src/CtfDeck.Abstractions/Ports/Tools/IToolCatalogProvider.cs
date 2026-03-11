using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolCatalogProvider
{
    Task<IReadOnlyCollection<ToolDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ToolDefinition?> GetByIdAsync(string toolId, CancellationToken cancellationToken = default);
}
