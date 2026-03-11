using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolInstallationCoordinator : IToolInstallationCoordinator
{
    private readonly IToolCatalogProvider _toolCatalogProvider;
    private readonly IToolDetector _toolDetector;
    private readonly IToolInstaller _toolInstaller;

    public ToolInstallationCoordinator(
        IToolCatalogProvider toolCatalogProvider,
        IToolDetector toolDetector,
        IToolInstaller toolInstaller)
    {
        _toolCatalogProvider = toolCatalogProvider;
        _toolDetector = toolDetector;
        _toolInstaller = toolInstaller;
    }

    public async Task<IReadOnlyCollection<ToolStatusDto>> GetInventoryAsync(CancellationToken cancellationToken = default)
        => await _toolDetector.DetectAllAsync(cancellationToken);

    public async Task InstallAsync(
        IReadOnlyCollection<string> toolIds,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken = default)
    {
        foreach (var toolId in toolIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tool = await _toolCatalogProvider.GetByIdAsync(toolId, cancellationToken);

            if (tool is null)
            {
                await progressCallback(new ToolInstallProgressDto
                {
                    ToolId = toolId,
                    State = ToolInstallState.Failed,
                    Message = "Unknown tool.",
                    Error = $"Unknown tool '{toolId}'."
                });
                continue;
            }

            await _toolInstaller.InstallAsync(tool, progressCallback, cancellationToken);
        }
    }
}
