namespace CtfDeck.Abstractions.Ports.Tools;

public interface IToolPathResolver
{
    string GetToolsRootDirectory();
    string GetToolsBinDirectory();
    string GetToolInstallDirectory(string toolId);
    string GetToolExecutablePath(string toolId, string executableName);
    string GetToolWorkingDirectory(string toolId);
}
