using System.Runtime.InteropServices;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolDetectionService : IToolDetector
{
    private readonly IToolCatalogProvider _toolCatalogProvider;
    private readonly IToolPathResolver _toolPathResolver;
    private readonly IPlatformInfoProvider _platformInfoProvider;

    public ToolDetectionService(
        IToolCatalogProvider toolCatalogProvider,
        IToolPathResolver toolPathResolver,
        IPlatformInfoProvider platformInfoProvider)
    {
        _toolCatalogProvider = toolCatalogProvider;
        _toolPathResolver = toolPathResolver;
        _platformInfoProvider = platformInfoProvider;
    }

    public async Task<IReadOnlyCollection<ToolStatusDto>> DetectAllAsync(CancellationToken cancellationToken = default)
    {
        var tools = await _toolCatalogProvider.GetAllAsync(cancellationToken);

        var results = new List<ToolStatusDto>(tools.Count);
        foreach (var tool in tools)
        {
            results.Add(await DetectInternalAsync(tool));
        }

        return results;
    }

    public async Task<ToolStatusDto?> DetectAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var tool = await _toolCatalogProvider.GetByIdAsync(toolId, cancellationToken);
        return tool is null ? null : await DetectInternalAsync(tool);
    }

    private async Task<ToolStatusDto> DetectInternalAsync(ToolDefinition tool)
    {
        await Task.CompletedTask;

        if (tool.Kind.Equals("externalWebApp", StringComparison.OrdinalIgnoreCase))
        {
            return new ToolStatusDto
            {
                Id = tool.Id,
                DisplayName = tool.DisplayName,
                Description = tool.Description,
                Kind = tool.Kind,
                IsInstalled = false,
                IsInstallable = false,
                InstalledPath = null,
                Version = null,
                Reason = null
            };
        }

        var installer = ResolveInstaller(tool);
        var isInstallable = IsInstallable(installer);

        if (installer is not null)
        {
            var exeName = installer.ExecutableName ?? tool.CheckCommand ?? tool.Id;
            var localPath = _toolPathResolver.GetToolExecutablePath(tool.Id, exeName);
            if (File.Exists(localPath))
            {
                return new ToolStatusDto
                {
                    Id = tool.Id,
                    DisplayName = tool.DisplayName,
                    Description = tool.Description,
                    Kind = tool.Kind,
                    IsInstalled = true,
                    IsInstallable = isInstallable,
                    InstalledPath = localPath,
                    Version = installer.Version,
                    Reason = null
                };
            }
        }

        var pathCommand = tool.CheckCommand ?? tool.Id;
        var pathExecutable = FindExecutableInPath(pathCommand);

        if (!string.IsNullOrWhiteSpace(pathExecutable))
        {
            return new ToolStatusDto
            {
                Id = tool.Id,
                DisplayName = tool.DisplayName,
                Description = tool.Description,
                Kind = tool.Kind,
                IsInstalled = true,
                IsInstallable = isInstallable,
                InstalledPath = pathExecutable,
                Version = installer?.Version,
                Reason = null
            };
        }

        return new ToolStatusDto
        {
            Id = tool.Id,
            DisplayName = tool.DisplayName,
            Description = tool.Description,
            Kind = tool.Kind,
            IsInstalled = false,
            IsInstallable = isInstallable,
            InstalledPath = null,
            Version = installer?.Version,
            Reason = BuildMissingReason(installer)
        };
    }

    private ToolInstaller? ResolveInstaller(ToolDefinition tool)
    {
        var os = _platformInfoProvider.GetOs();
        var arch = _platformInfoProvider.GetArch();

        var matches = tool.Installers.Where(i =>
            i.Os.Equals(os, StringComparison.OrdinalIgnoreCase) &&
            i.Arch.Equals(arch, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matches.Count == 0)
            return null;

        var pmMatch = matches.FirstOrDefault(i =>
            i.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(i.PackageManager) &&
            FindExecutableInPath(i.PackageManager) is not null);

        return pmMatch ?? matches.First();
    }

    private bool IsInstallable(ToolInstaller? installer)
    {
        if (installer is null)
            return false;

        if (!installer.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(installer.PackageManager))
            return false;

        return FindExecutableInPath(installer.PackageManager) is not null;
    }

    private string BuildMissingReason(ToolInstaller? installer)
    {
        if (installer is null)
            return $"No installer available for {_platformInfoProvider.GetOs()}/{_platformInfoProvider.GetArch()}.";

        if (installer.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(installer.PackageManager) &&
            FindExecutableInPath(installer.PackageManager) is null)
        {
            return $"Required package manager '{installer.PackageManager}' is not available in PATH.";
        }

        return "Tool not found locally or in PATH.";
    }

    private static void RefreshPathEnvironmentVariable()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var processPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";

            var separators = new[] { Path.PathSeparator };
            var allPaths = new List<string>();

            var processDirs = processPath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var machineDirs = machinePath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var userDirs = userPath.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dir in processDirs.Concat(machineDirs).Concat(userDirs))
            {
                var trimmed = dir.Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                {
                    allPaths.Add(trimmed);
                }
            }

            var combinedPath = string.Join(Path.PathSeparator, allPaths);
            Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);
        }
        catch
        {
            // Ignore registry reading or assignment errors
        }
    }

    private static string? FindExecutableInPath(string executableName)
    {
        RefreshPathEnvironmentVariable();
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var candidates = OperatingSystem.IsWindows()
            ? new[] { executableName, $"{executableName}.exe" }
            : new[] { executableName };

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(dir.Trim(), candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }
}
