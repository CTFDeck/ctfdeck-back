using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolInstallationService : IToolInstaller
{
    private readonly IToolPathResolver _toolPathResolver;
    private readonly ArchiveExtractor _archiveExtractor;

    public ToolInstallationService(
        IToolPathResolver toolPathResolver,
        ArchiveExtractor archiveExtractor)
    {
        _toolPathResolver = toolPathResolver;
        _archiveExtractor = archiveExtractor;
    }

    public async Task InstallAsync(
        ToolDefinition tool,
        Func<ToolInstallProgressDto, Task> progressCallback,
        CancellationToken cancellationToken = default)
    {
        var installer = ResolveInstaller(tool);

        try
        {
            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Pending,
                Message = "Preparing installation."
            });

            var installDirectory = _toolPathResolver.GetToolInstallDirectory(tool.Id);
            var workDirectory = _toolPathResolver.GetToolWorkingDirectory(tool.Id);

            if (Directory.Exists(workDirectory))
                Directory.Delete(workDirectory, true);

            Directory.CreateDirectory(workDirectory);
            Directory.CreateDirectory(installDirectory);

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
                throw new InvalidOperationException("Installed binary verification failed.");

            await progressCallback(new ToolInstallProgressDto
            {
                ToolId = tool.Id,
                State = ToolInstallState.Success,
                Message = "Installation completed.",
                ProgressPercent = 100,
                InstalledPath = targetBinaryPath
            });
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

    private static ToolInstaller ResolveInstaller(ToolDefinition tool)
    {
        var os = GetCurrentOs();
        var arch = GetCurrentArch();

        return tool.Installers.FirstOrDefault(i =>
                   i.Os.Equals(os, StringComparison.OrdinalIgnoreCase) &&
                   i.Arch.Equals(arch, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"No installer available for '{tool.Id}' on {os}/{arch}.");
    }

    private static string ResolveSourceBinaryPath(ToolInstaller installer, string downloadFilePath, string extractionDirectory)
    {
        if (installer.ArchiveType.Equals("none", StringComparison.OrdinalIgnoreCase))
            return downloadFilePath;

        var path = Path.Combine(extractionDirectory, installer.ExecutableRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Executable not found after extraction: {path}");

        return path;
    }

    private static string GetDownloadFileName(ToolInstaller installer)
    {
        return installer.ArchiveType.ToLowerInvariant() switch
        {
            "zip" => "package.zip",
            "tar.gz" => "package.tar.gz",
            "none" => installer.ExecutableName,
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
        var psi = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
            return false;

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode is 0 or 1;
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
}
