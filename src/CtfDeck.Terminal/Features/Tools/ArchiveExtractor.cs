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

            case "none":
                break;

            default:
                throw new NotSupportedException($"Archive type '{archiveType}' is not supported.");
        }
    }
}
