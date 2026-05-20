using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolInstaller
{
    Task InstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default);

    Task UninstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default);
}
