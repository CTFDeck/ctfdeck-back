using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;

namespace CtfDeck.Terminal.Features.Tools;

public class ArchiveExtractor
{
    public async Task ExtractAsync(
        string archivePath,
        string destinationDirectory,
        string archiveType,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        switch (archiveType.ToLowerInvariant())
        {
            case "zip":
                ZipFile.ExtractToDirectory(archivePath, destinationDirectory, overwriteFiles: true);
                break;

            case "tar.gz":
                await using (var fileStream = File.OpenRead(archivePath))
                await using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    TarFile.ExtractToDirectory(gzipStream, destinationDirectory, overwriteFiles: true);
                }
                break;

            case "7z":
                if (OperatingSystem.IsWindows())
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xf \"{archivePath}\" -C \"{destinationDirectory}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    if (process is not null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        if (process.ExitCode != 0)
                        {
                            throw new InvalidOperationException($"Tar extraction of .7z failed with exit code {process.ExitCode}");
                        }
                    }
                }
                else
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "7z",
                        Arguments = $"x \"{archivePath}\" -o\"{destinationDirectory}\" -y",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    if (process is not null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        if (process.ExitCode != 0)
                        {
                            throw new InvalidOperationException($"7z extraction failed with exit code {process.ExitCode}");
                        }
                    }
                }
                break;

            case "none":
                break;

            default:
                throw new NotSupportedException($"Archive type '{archiveType}' is not supported.");
        }
    }
}
