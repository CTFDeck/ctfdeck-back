using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolInstaller
{
    Task InstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken = default);
}
