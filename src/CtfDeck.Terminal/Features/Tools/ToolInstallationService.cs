using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolInstallationService : IToolInstaller
{
    private readonly IToolPathResolver _toolPathResolver;
    private readonly ArchiveExtractor _archiveExtractor;
    private readonly IPlatformInfoProvider _platformInfoProvider;

    public ToolInstallationService(
        IToolPathResolver toolPathResolver,
        ArchiveExtractor archiveExtractor,
        IPlatformInfoProvider platformInfoProvider)
    {
        _toolPathResolver = toolPathResolver;
        _archiveExtractor = archiveExtractor;
        _platformInfoProvider = platformInfoProvider;
    }

    public async Task InstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var installer = ResolveInstaller(tool);

            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Pending,
                Message = "Preparing installation."
            });

            if (installer.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase))
            {
                await InstallWithPackageManagerAsync(tool, installer, progressCallback, requestSecretAsync, cancellationToken);
            }
            else
            {
                await InstallWithArchiveAsync(tool, installer, progressCallback, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Failed,
                Message = "Installation failed.",
                Error = ex.Message
            });
        }
    }

    public async Task UninstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var installer = ResolveInstaller(tool);

            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Uninstalling,
                Message = "Preparing uninstallation."
            });

            if (installer.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase))
            {
                await UninstallWithPackageManagerAsync(tool, installer, progressCallback, requestSecretAsync, cancellationToken);
            }
            else
            {
                await UninstallWithArchiveAsync(tool, installer, progressCallback, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Failed,
                Message = "Uninstallation failed.",
                Error = ex.Message
            });
        }
    }

    private async Task UninstallWithArchiveAsync(
        ToolDefinition tool,
        ToolInstaller installer,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken)
    {
        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Uninstalling,
            Message = "Deleting binary and directories."
        });

        var targetBinaryPath = _toolPathResolver.GetToolExecutablePath(tool.Id, installer.ExecutableName ?? tool.Id);
        if (File.Exists(targetBinaryPath))
        {
            File.Delete(targetBinaryPath);
        }

        var workDirectory = _toolPathResolver.GetToolWorkingDirectory(tool.Id);
        if (Directory.Exists(workDirectory))
        {
            Directory.Delete(workDirectory, true);
        }

        var installDirectory = _toolPathResolver.GetToolInstallDirectory(tool.Id);
        if (Directory.Exists(installDirectory))
        {
            Directory.Delete(installDirectory, true);
        }

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Success,
            Message = "Uninstallation completed.",
            ProgressPercent = 100
        });
    }

    private async Task UninstallWithPackageManagerAsync(
        ToolDefinition tool,
        ToolInstaller installer,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(installer.PackageManager) || string.IsNullOrWhiteSpace(installer.PackageId))
            throw new InvalidOperationException("Package manager installer is missing required fields.");

        var pmPath = FindExecutableInPath(installer.PackageManager);
        if (pmPath is null)
            throw new InvalidOperationException($"Package manager '{installer.PackageManager}' is not available in PATH.");

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Uninstalling,
            Message = $"Uninstalling with {installer.PackageManager}."
        });

        var needsElevation = installer.RequiresElevation ?? !OperatingSystem.IsWindows();
        var useSudo = needsElevation && !OperatingSystem.IsWindows() && !IsRunningAsRoot();
        var useWindowsUac = OperatingSystem.IsWindows() && needsElevation && !IsWindowsAdministrator();

        var (fileName, arguments) = BuildUninstallCommand(installer);

        string? sudoPassword = null;
        if (useSudo)
        {
            if (requestSecretAsync is null)
                throw new InvalidOperationException("Sudo password callback is unavailable.");

            sudoPassword = await requestSecretAsync("Sudo password required to uninstall tool", cancellationToken);
            if (string.IsNullOrEmpty(sudoPassword))
                throw new OperationCanceledException("Sudo password not provided");
        }

        int exitCode;
        if (useWindowsUac)
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Uninstalling,
                Message = "Requesting administrator privileges (UAC)..."
            });
            exitCode = await ExecuteCommandElevatedWindowsAsync(fileName, arguments, cancellationToken);
        }
        else
        {
            exitCode = await ExecuteCommandAsync(fileName, arguments, useSudo, sudoPassword, cancellationToken);
        }

        if (exitCode != 0)
            throw new InvalidOperationException($"Package manager uninstall failed with exit code {exitCode}.");

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Success,
            Message = "Uninstallation completed.",
            ProgressPercent = 100
        });
    }

    private async Task InstallWithArchiveAsync(
        ToolDefinition tool,
        ToolInstaller installer,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(installer.Url) ||
            string.IsNullOrWhiteSpace(installer.ArchiveType) ||
            string.IsNullOrWhiteSpace(installer.ExecutableName) ||
            string.IsNullOrWhiteSpace(installer.ExecutableRelativePath))
        {
            throw new InvalidOperationException("Archive installer is missing required fields.");
        }

        var installDirectory = _toolPathResolver.GetToolInstallDirectory(tool.Id);
        var binDirectory = _toolPathResolver.GetToolsBinDirectory();
        var workDirectory = _toolPathResolver.GetToolWorkingDirectory(tool.Id);

        if (Directory.Exists(workDirectory))
            Directory.Delete(workDirectory, true);

        Directory.CreateDirectory(workDirectory);
        Directory.CreateDirectory(installDirectory);
        Directory.CreateDirectory(binDirectory);

        var downloadFilePath = Path.Combine(workDirectory, GetDownloadFileName(installer));

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Downloading,
            Message = "Downloading package.",
            ProgressPercent = 0
        });

        await DownloadFileAsync(installer.Url, downloadFilePath, async percent =>
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Downloading,
                Message = "Downloading package.",
                ProgressPercent = percent
            });
        }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(installer.Sha256))
        {
            var actualHash = await ComputeSha256Async(downloadFilePath, cancellationToken);
            if (!actualHash.Equals(installer.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Downloaded file checksum mismatch.");
        }

        var extractionDirectory = Path.Combine(workDirectory, "extracted");
        Directory.CreateDirectory(extractionDirectory);

        if (!installer.ArchiveType.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Extracting,
                Message = "Extracting package."
            });

            await _archiveExtractor.ExtractAsync(downloadFilePath, extractionDirectory, installer.ArchiveType, cancellationToken);
        }

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Installing,
            Message = "Installing binary."
        });

        var sourceBinaryPath = ResolveSourceBinaryPath(installer, downloadFilePath, extractionDirectory);
        var targetBinaryPath = _toolPathResolver.GetToolExecutablePath(tool.Id, installer.ExecutableName);

        if (File.Exists(targetBinaryPath))
            File.Delete(targetBinaryPath);

        File.Copy(sourceBinaryPath, targetBinaryPath, true);

        if (!OperatingSystem.IsWindows())
            await MakeExecutableAsync(targetBinaryPath, cancellationToken);

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Verifying,
            Message = "Verifying installation."
        });

        var verified = await VerifyAsync(targetBinaryPath, tool.CheckArguments ?? "--help", cancellationToken);
        if (!verified)
            throw new InvalidOperationException("Installed binary verification failed or timed out.");

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Success,
            Message = "Installation completed.",
            ProgressPercent = 100,
            InstalledPath = targetBinaryPath
        });
    }

    private async Task InstallWithPackageManagerAsync(
        ToolDefinition tool,
        ToolInstaller installer,
        Func<ToolInstallProgressDto, Task> progressCallback,
        Func<string, CancellationToken, Task<string?>>? requestSecretAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(installer.PackageManager) || string.IsNullOrWhiteSpace(installer.PackageId))
            throw new InvalidOperationException("Package manager installer is missing required fields.");

        var pmPath = FindExecutableInPath(installer.PackageManager);
        if (pmPath is null)
            throw new InvalidOperationException($"Package manager '{installer.PackageManager}' is not available in PATH.");

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Installing,
            Message = $"Installing with {installer.PackageManager}."
        });

        var needsElevation = installer.RequiresElevation ?? !OperatingSystem.IsWindows();
        var useSudo = needsElevation && !OperatingSystem.IsWindows() && !IsRunningAsRoot();
        var useWindowsUac = OperatingSystem.IsWindows() && needsElevation && !IsWindowsAdministrator();

        var (fileName, arguments) = BuildInstallCommand(installer);
        if (!string.IsNullOrWhiteSpace(installer.InstallArgs))
            arguments = $"{arguments} {installer.InstallArgs}";

        string? sudoPassword = null;
        if (useSudo)
        {
            if (requestSecretAsync is null)
                throw new InvalidOperationException("Sudo password callback is unavailable.");

            sudoPassword = await requestSecretAsync("Sudo password required to install tool", cancellationToken);
            if (string.IsNullOrEmpty(sudoPassword))
                throw new OperationCanceledException("Sudo password not provided");
        }

        int exitCode;
        if (useWindowsUac)
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Installing,
                Message = "Requesting administrator privileges (UAC)..."
            });
            exitCode = await ExecuteCommandElevatedWindowsAsync(fileName, arguments, cancellationToken);
        }
        else
        {
            exitCode = await ExecuteCommandAsync(fileName, arguments, useSudo, sudoPassword, cancellationToken);
        }
        if (exitCode != 0)
            throw new InvalidOperationException($"Package manager install failed with exit code {exitCode}.");

        var checkCommand = tool.CheckCommand ?? tool.Id;
        var installedPath = FindExecutableInPath(checkCommand);

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Verifying,
            Message = "Verifying installation."
        });

        if (installedPath is null)
            throw new InvalidOperationException("Tool is not available in PATH after installation.");

        var verified = await VerifyAsync(installedPath, tool.CheckArguments ?? "--help", cancellationToken);
        if (!verified)
            throw new InvalidOperationException("Installed tool verification failed or timed out.");

        await progressCallback(new ToolInstallProgressDto
        {
            ToolId = tool.Id,
            State = ToolInstallState.Success,
            Message = "Installation completed.",
            ProgressPercent = 100,
            InstalledPath = installedPath
        });
    }

    private ToolInstaller ResolveInstaller(ToolDefinition tool)
    {
        var os = _platformInfoProvider.GetOs();
        var arch = _platformInfoProvider.GetArch();

        var matches = tool.Installers
            .Where(i => i.Os.Equals(os, StringComparison.OrdinalIgnoreCase) &&
                        i.Arch.Equals(arch, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            throw new InvalidOperationException($"No installer available for '{tool.Id}' on {os}/{arch}.");

        var pmMatch = matches.FirstOrDefault(i =>
            i.Type.Equals("packageManager", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(i.PackageManager) &&
            FindExecutableInPath(i.PackageManager) is not null);

        return pmMatch ?? matches.First();
    }

    private static (string fileName, string arguments) BuildInstallCommand(ToolInstaller installer)
    {
        var pm = installer.PackageManager!.ToLowerInvariant();
        var packageId = installer.PackageId!;

        return pm switch
        {
            "winget" => ("winget", $"install --id {packageId} --accept-source-agreements --accept-package-agreements --silent"),
            "apt" => ("apt-get", $"install -y {packageId}"),
            "dnf" => ("dnf", $"install -y {packageId}"),
            "pacman" => ("pacman", $"-Sy --noconfirm {packageId}"),
            "brew" => ("brew", $"install {packageId}"),
            _ => throw new InvalidOperationException($"Unsupported package manager '{installer.PackageManager}'.")
        };
    }

    private static (string fileName, string arguments) BuildUninstallCommand(ToolInstaller installer)
    {
        var pm = installer.PackageManager!.ToLowerInvariant();
        var packageId = installer.PackageId!;

        return pm switch
        {
            "winget" => ("winget", $"uninstall --id {packageId} --silent"),
            "apt" => ("apt-get", $"remove -y {packageId}"),
            "dnf" => ("dnf", $"remove -y {packageId}"),
            "pacman" => ("pacman", $"-R --noconfirm {packageId}"),
            "brew" => ("brew", $"uninstall {packageId}"),
            _ => throw new InvalidOperationException($"Unsupported package manager '{installer.PackageManager}'.")
        };
    }

    private static async Task<int> ExecuteCommandAsync(
        string fileName,
        string arguments,
        bool useSudo,
        string? sudoPassword,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = useSudo ? "sudo" : fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = useSudo,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (useSudo)
        {
            psi.ArgumentList.Add("-S");
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add("");
            psi.ArgumentList.Add(fileName);
            foreach (var arg in SplitArgs(arguments))
                psi.ArgumentList.Add(arg);
        }
        else
        {
            foreach (var arg in SplitArgs(arguments))
                psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start install process.");

        if (useSudo && !string.IsNullOrEmpty(sudoPassword))
        {
            await process.StandardInput.WriteLineAsync(sudoPassword);
            await process.StandardInput.FlushAsync();
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static async Task<int> ExecuteCommandElevatedWindowsAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start elevated install process.");
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
        {
            throw new OperationCanceledException("UAC elevation request was canceled by the user.", ex);
        }
    }

    private static void RefreshPathEnvironmentVariable()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var combinedPath = $"{machinePath}{Path.PathSeparator}{userPath}";
            Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);
        }
        catch
        {
            // Ignore registry reading or assignment errors
        }
    }

    private static IEnumerable<string> SplitArgs(string arguments)
    {
        return arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool IsWindowsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool IsRunningAsRoot()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            ? string.Equals(Environment.UserName, "root", StringComparison.Ordinal)
            : false;
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

    private static string ResolveSourceBinaryPath(ToolInstaller installer, string downloadFilePath, string extractionDirectory)
    {
        if (installer.ArchiveType!.Equals("none", StringComparison.OrdinalIgnoreCase))
            return downloadFilePath;

        var path = Path.Combine(extractionDirectory, installer.ExecutableRelativePath!.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Executable not found after extraction: {path}");

        return path;
    }

    private static string GetDownloadFileName(ToolInstaller installer)
    {
        return installer.ArchiveType!.ToLowerInvariant() switch
        {
            "zip" => "package.zip",
            "tar.gz" => "package.tar.gz",
            "none" => installer.ExecutableName!,
            _ => "package.bin"
        };
    }

    private static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        Func<double?, Task> onProgress,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destinationPath);

        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;

            if (contentLength.HasValue && contentLength.Value > 0)
            {
                var percent = Math.Round((double)totalRead / contentLength.Value * 100, 2);
                await onProgress(percent);
            }
            else
            {
                await onProgress(null);
            }
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static async Task MakeExecutableAsync(string filePath, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        psi.ArgumentList.Add("+x");
        psi.ArgumentList.Add(filePath);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start chmod.");
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"chmod failed: {error}");
        }
    }

    private static async Task<bool> VerifyAsync(string binaryPath, string arguments, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var psi = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
            return false;

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);
            var waitTask = process.WaitForExitAsync(linkedCts.Token);

            await Task.WhenAll(stdoutTask, stderrTask, waitTask);

            return process.ExitCode is 0 or 1;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return false;
        }
    }
}
