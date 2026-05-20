using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolInstallationCoordinator
{
    Task<IReadOnlyCollection<ToolStatusDto>> GetInventoryAsync(CancellationToken cancellationToken = default);

    Task InstallAsync(
        IReadOnlyCollection<string> toolIds,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default);

    Task UninstallAsync(
        IReadOnlyCollection<string> toolIds,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default);
}
