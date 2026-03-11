using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolCatalogSnapshotService
{
    private readonly IToolCatalogProvider _toolCatalogProvider;
    private readonly IToolDetector _toolDetector;

    public ToolCatalogSnapshotService(
        IToolCatalogProvider toolCatalogProvider,
        IToolDetector toolDetector)
    {
        _toolCatalogProvider = toolCatalogProvider;
        _toolDetector = toolDetector;
    }

    public async Task<IReadOnlyCollection<ToolCatalogItemDto>> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await _toolCatalogProvider.GetAllAsync(cancellationToken);
        var detected = await _toolDetector.DetectAllAsync(cancellationToken);

        var detectedById = detected.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        return catalog.Select(tool =>
        {
            detectedById.TryGetValue(tool.Id, out var status);

            return new ToolCatalogItemDto
            {
                Id = tool.Id,
                DisplayName = tool.DisplayName,
                Category = tool.Category,
                Kind = tool.Kind,
                Description = tool.Description,
                CommandTemplate = tool.CommandTemplate,
                ExternalUrl = tool.ExternalUrl,
                IsInstalled = status?.IsInstalled ?? false,
                IsInstallable = status?.IsInstallable ?? false,
                InstalledPath = status?.InstalledPath,
                Version = status?.Version,
                Reason = status?.Reason
            };
        }).ToArray();
    }
}
