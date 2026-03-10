using System.Runtime.InteropServices;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolDetectionService : IToolDetector
{
    private readonly IToolCatalogProvider _toolCatalogProvider;
    private readonly IToolPathResolver _toolPathResolver;

    public ToolDetectionService(
        IToolCatalogProvider toolCatalogProvider,
        IToolPathResolver toolPathResolver)
    {
        _toolCatalogProvider = toolCatalogProvider;
        _toolPathResolver = toolPathResolver;
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

        var installer = ResolveInstaller(tool);
        var isInstallable = installer is not null;

        if (installer is not null)
        {
            var localPath = _toolPathResolver.GetToolExecutablePath(tool.Id, installer.ExecutableName);
            if (File.Exists(localPath))
            {
                return new ToolStatusDto
                {
                    Id = tool.Id,
                    DisplayName = tool.DisplayName,
                    Description = tool.Description,
                    IsInstalled = true,
                    IsInstallable = true,
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
            IsInstalled = false,
            IsInstallable = isInstallable,
            InstalledPath = null,
            Version = installer?.Version,
            Reason = isInstallable
                ? "Tool not found locally or in PATH."
                : $"No installer available for {GetCurrentOs()}/{GetCurrentArch()}."
        };
    }

    private static ToolInstaller? ResolveInstaller(ToolDefinition tool)
    {
        var os = GetCurrentOs();
        var arch = GetCurrentArch();

        return tool.Installers.FirstOrDefault(i =>
            i.Os.Equals(os, StringComparison.OrdinalIgnoreCase) &&
            i.Arch.Equals(arch, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetCurrentOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        throw new PlatformNotSupportedException("Unsupported OS.");
    }

    private static string GetCurrentArch()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}")
        };
    }

    private static string? FindExecutableInPath(string executableName)
    {
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
