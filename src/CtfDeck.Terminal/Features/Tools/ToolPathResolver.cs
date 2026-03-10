using CtfDeck.Abstractions.Ports.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolPathResolver : IToolPathResolver
{
    public string GetToolsRootDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "CtfDeck", "tools");
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "CtfDeck", "tools");
        }

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "ctfdeck", "tools");
        }

        throw new PlatformNotSupportedException("Unsupported OS.");
    }

    public string GetToolInstallDirectory(string toolId)
        => Path.Combine(GetToolsRootDirectory(), toolId);

    public string GetToolExecutablePath(string toolId, string executableName)
        => Path.Combine(GetToolInstallDirectory(toolId), executableName);

    public string GetToolWorkingDirectory(string toolId)
        => Path.Combine(Path.GetTempPath(), "CtfDeck", "tools-work", toolId);
}
