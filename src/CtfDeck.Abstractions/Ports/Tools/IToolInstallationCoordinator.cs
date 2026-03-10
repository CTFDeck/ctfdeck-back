using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolInstallationCoordinator
{
    Task<IReadOnlyCollection<ToolStatusDto>> GetInventoryAsync(CancellationToken cancellationToken = default);

    Task InstallAsync(
        IReadOnlyCollection<string> toolIds,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken = default);
}
