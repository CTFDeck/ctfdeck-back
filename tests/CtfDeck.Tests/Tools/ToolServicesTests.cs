using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Terminal.Features.Tools;
using FluentAssertions;

namespace CtfDeck.Tests.Tools;

public sealed class ToolCatalogServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldLoadEmbeddedCatalog()
    {
        var service = new ToolCatalogService();

        var tools = await service.GetAllAsync();

        tools.Should().NotBeEmpty();
        tools.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldBeCaseInsensitive()
    {
        var service = new ToolCatalogService();
        var first = (await service.GetAllAsync()).First();

        var result = await service.GetByIdAsync(first.Id.ToUpperInvariant());

        result.Should().NotBeNull();
        result!.Id.Should().Be(first.Id);
    }
}

public sealed class ToolPathResolverTests
{
    [Fact]
    public void PathMethods_ShouldReturnCoherentPaths()
    {
        var resolver = new ToolPathResolver();

        var root = resolver.GetToolsRootDirectory();
        var bin = resolver.GetToolsBinDirectory();
        var install = resolver.GetToolInstallDirectory("nmap");
        var exec = resolver.GetToolExecutablePath("nmap", "nmap");
        var work = resolver.GetToolWorkingDirectory("nmap");

        root.Should().NotBeNullOrWhiteSpace();
        bin.Should().Be(Path.Combine(root, "bin"));
        install.Should().Be(Path.Combine(root, "nmap"));
        exec.Should().Be(Path.Combine(bin, "nmap"));
        work.Should().Contain("tools-work");
    }
}

public sealed class ArchiveExtractorTests : IDisposable
{
    private readonly string _tempRoot;

    public ArchiveExtractorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ctfdeck-archive-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task ExtractAsync_Zip_ShouldExtractFiles()
    {
        var archivePath = Path.Combine(_tempRoot, "sample.zip");
        var outputPath = Path.Combine(_tempRoot, "out-zip");

        using (var fs = File.Create(archivePath))
        using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("bin/tool.txt");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(Encoding.UTF8.GetBytes("hello"));
        }

        var sut = new ArchiveExtractor();
        await sut.ExtractAsync(archivePath, outputPath, "zip");

        File.Exists(Path.Combine(outputPath, "bin", "tool.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(outputPath, "bin", "tool.txt")).Should().Be("hello");
    }

    [Fact]
    public async Task ExtractAsync_TarGz_ShouldExtractFiles()
    {
        var archivePath = Path.Combine(_tempRoot, "sample.tar.gz");
        var outputPath = Path.Combine(_tempRoot, "out-targz");
        var tarPath = Path.Combine(_tempRoot, "sample.tar");

        await using (var tarFs = File.Create(tarPath))
        await using (var writer = new System.Formats.Tar.TarWriter(tarFs))
        {
            var data = Encoding.UTF8.GetBytes("from-tar");
            var entry = new System.Formats.Tar.PaxTarEntry(System.Formats.Tar.TarEntryType.RegularFile, "folder/file.txt")
            {
                DataStream = new MemoryStream(data)
            };
            writer.WriteEntry(entry);
        }

        await using (var tarInput = File.OpenRead(tarPath))
        await using (var gzipOut = File.Create(archivePath))
        await using (var gzip = new System.IO.Compression.GZipStream(gzipOut, System.IO.Compression.CompressionLevel.SmallestSize))
        {
            await tarInput.CopyToAsync(gzip);
        }

        var sut = new ArchiveExtractor();
        await sut.ExtractAsync(archivePath, outputPath, "tar.gz");

        File.Exists(Path.Combine(outputPath, "folder", "file.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(outputPath, "folder", "file.txt")).Should().Be("from-tar");
    }

    [Fact]
    public async Task ExtractAsync_None_ShouldOnlyEnsureDirectory()
    {
        var outputPath = Path.Combine(_tempRoot, "out-none");
        var sut = new ArchiveExtractor();

        await sut.ExtractAsync(Path.Combine(_tempRoot, "ignored"), outputPath, "none");

        Directory.Exists(outputPath).Should().BeTrue();
        Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_Unsupported_ShouldThrow()
    {
        var sut = new ArchiveExtractor();
        var action = () => sut.ExtractAsync("ignored.bin", Path.Combine(_tempRoot, "bad"), "7z");

        await action.Should().ThrowAsync<NotSupportedException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }
}

public sealed class ToolDetectionServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public ToolDetectionServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ctfdeck-tool-detect-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task DetectAsync_UnknownTool_ShouldReturnNull()
    {
        var catalog = new FakeCatalogProvider([]);
        var resolver = new TempPathResolver(_tempRoot);
        var sut = new ToolDetectionService(catalog, resolver);

        var status = await sut.DetectAsync("missing");

        status.Should().BeNull();
    }

    [Fact]
    public async Task DetectAsync_ExternalWebApp_ShouldBeNotInstallable()
    {
        var tool = ToolTestHelpers.MakeExternalTool("search");
        var sut = new ToolDetectionService(new FakeCatalogProvider([tool]), new TempPathResolver(_tempRoot));

        var status = await sut.DetectAsync(tool.Id);

        status.Should().NotBeNull();
        status!.IsInstalled.Should().BeFalse();
        status.IsInstallable.Should().BeFalse();
        status.Kind.Should().Be("externalWebApp");
    }

    [Fact]
    public async Task DetectAsync_BinaryInstalledLocally_ShouldReturnInstalledPath()
    {
        var (os, arch) = ToolTestHelpers.CurrentPlatform();
        var tool = ToolTestHelpers.MakeBinaryTool("local-tool", [new ToolInstaller
        {
            Os = os,
            Arch = arch,
            Type = "download",
            Url = "http://localhost",
            ArchiveType = "none",
            ExecutableRelativePath = "tool",
            ExecutableName = "tool",
            Version = "1.2.3"
        }]);

        var resolver = new TempPathResolver(_tempRoot);
        Directory.CreateDirectory(resolver.GetToolsBinDirectory());
        File.WriteAllText(resolver.GetToolExecutablePath(tool.Id, "tool"), "x");

        var sut = new ToolDetectionService(new FakeCatalogProvider([tool]), resolver);
        var status = await sut.DetectAsync(tool.Id);

        status.Should().NotBeNull();
        status!.IsInstalled.Should().BeTrue();
        status.IsInstallable.Should().BeTrue();
        status.Version.Should().Be("1.2.3");
        status.InstalledPath.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DetectAsync_NotInLocalButInPath_ShouldReturnPathMatch()
    {
        var tempPathDir = Path.Combine(_tempRoot, "path-bin");
        Directory.CreateDirectory(tempPathDir);

        var checkCommand = OperatingSystem.IsWindows() ? "path-tool.cmd" : "path-tool";
        var pathExe = Path.Combine(tempPathDir, checkCommand);
        File.WriteAllText(pathExe, "echo hi");

        var (os, arch) = ToolTestHelpers.CurrentPlatform();
        var tool = ToolTestHelpers.MakeBinaryTool("path-tool", [new ToolInstaller
        {
            Os = os,
            Arch = arch,
            Type = "download",
            Url = "http://localhost",
            ArchiveType = "none",
            ExecutableRelativePath = "path-tool",
            ExecutableName = "path-tool",
            Version = "9.9.9"
        }]);
        tool.CheckCommand = checkCommand;

        var previous = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", tempPathDir + Path.PathSeparator + previous);
        try
        {
            var sut = new ToolDetectionService(new FakeCatalogProvider([tool]), new TempPathResolver(_tempRoot));
            var status = await sut.DetectAsync(tool.Id);

            status.Should().NotBeNull();
            status!.IsInstalled.Should().BeTrue();
            status.InstalledPath.Should().Be(pathExe);
            status.IsInstallable.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", previous);
        }
    }

    [Fact]
    public async Task DetectAsync_NotInstallable_ShouldExplainReason()
    {
        var tool = ToolTestHelpers.MakeBinaryTool("uninstallable", []);
        var sut = new ToolDetectionService(new FakeCatalogProvider([tool]), new TempPathResolver(_tempRoot));

        var status = await sut.DetectAsync(tool.Id);

        status.Should().NotBeNull();
        status!.IsInstalled.Should().BeFalse();
        status.IsInstallable.Should().BeFalse();
        status.Reason.Should().Contain("No installer available");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }
}

public sealed class ToolInstallationCoordinatorTests
{
    [Fact]
    public async Task GetInventoryAsync_ShouldDelegateToDetector()
    {
        var expected = new[]
        {
            new ToolStatusDto
            {
                Id = "a",
                DisplayName = "A",
                Description = "desc",
                Kind = "binary",
                IsInstalled = true,
                IsInstallable = true
            }
        };

        var coordinator = new ToolInstallationCoordinator(
            new FakeCatalogProvider([]),
            new FakeDetector(expected),
            new RecordingInstaller());

        var inventory = await coordinator.GetInventoryAsync();

        inventory.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InstallAsync_UnknownTool_ShouldEmitFailedProgress()
    {
        var coordinator = new ToolInstallationCoordinator(
            new FakeCatalogProvider([]),
            new FakeDetector([]),
            new RecordingInstaller());

        var progress = new List<ToolInstallProgressDto>();
        await coordinator.InstallAsync(["missing-tool"], p =>
        {
            progress.Add(p);
            return Task.CompletedTask;
        });

        progress.Should().ContainSingle();
        progress[0].ToolId.Should().Be("missing-tool");
        progress[0].State.Should().Be(ToolInstallState.Failed);
        progress[0].Error.Should().Contain("Unknown tool");
    }

    [Fact]
    public async Task InstallAsync_ShouldInstallDistinctToolIdsCaseInsensitive()
    {
        var tool = ToolTestHelpers.MakeBinaryTool("nmap", []);
        var installer = new RecordingInstaller();
        var coordinator = new ToolInstallationCoordinator(
            new FakeCatalogProvider([tool]),
            new FakeDetector([]),
            installer);

        await coordinator.InstallAsync(["nmap", "NMAP"], _ => Task.CompletedTask);

        installer.Calls.Should().HaveCount(1);
        installer.Calls.Single().Id.Should().Be("nmap");
    }
}

public sealed class ToolInstallationServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public ToolInstallationServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ctfdeck-tool-install-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task InstallAsync_NoInstallerForPlatform_ShouldEmitFailedState()
    {
        var resolver = new TempPathResolver(_tempRoot);
        var sut = new ToolInstallationService(resolver, new ArchiveExtractor());
        var tool = ToolTestHelpers.MakeBinaryTool("no-installer", []);

        var progress = new List<ToolInstallProgressDto>();
        await sut.InstallAsync(tool, p =>
        {
            progress.Add(p);
            return Task.CompletedTask;
        });

        progress.Should().NotBeEmpty();
        progress.Last().State.Should().Be(ToolInstallState.Failed);
        progress.Last().Error.Should().Contain("No installer available");
    }

    [Fact]
    public async Task InstallAsync_ChecksumMismatch_ShouldEmitFailedState()
    {
        var bytes = Encoding.UTF8.GetBytes("not-the-right-hash");
        await using var server = await SingleResponseHttpServer.StartAsync(bytes);
        var (os, arch) = ToolTestHelpers.CurrentPlatform();
        var exeName = OperatingSystem.IsWindows() ? "fake.exe" : "fake";
        var tool = ToolTestHelpers.MakeBinaryTool("checksum-tool", [new ToolInstaller
        {
            Os = os,
            Arch = arch,
            Type = "download",
            Url = server.Url,
            ArchiveType = "none",
            ExecutableRelativePath = exeName,
            ExecutableName = exeName,
            Sha256 = "DEADBEEF"
        }]);

        var sut = new ToolInstallationService(new TempPathResolver(_tempRoot), new ArchiveExtractor());
        var progress = new List<ToolInstallProgressDto>();
        await sut.InstallAsync(tool, p =>
        {
            progress.Add(p);
            return Task.CompletedTask;
        });

        progress.Last().State.Should().Be(ToolInstallState.Failed);
        progress.Last().Error.Should().Contain("checksum mismatch");
    }

    [Fact]
    public async Task InstallAsync_WithArchiveZipAndValidBinary_ShouldSucceed()
    {
        var knownExe = ToolTestHelpers.GetKnownExecutablePath();
        var exeBytes = await File.ReadAllBytesAsync(knownExe);
        var exeName = Path.GetFileName(knownExe);
        var relativePath = Path.Combine("bin", exeName).Replace('\\', '/');
        var zipBytes = ToolTestHelpers.BuildZip(relativePath, exeBytes);
        var sha = Convert.ToHexString(SHA256.HashData(zipBytes));

        await using var server = await SingleResponseHttpServer.StartAsync(zipBytes);
        var (os, arch) = ToolTestHelpers.CurrentPlatform();
        var tool = ToolTestHelpers.MakeBinaryTool("zip-tool", [new ToolInstaller
        {
            Os = os,
            Arch = arch,
            Type = "download",
            Url = server.Url,
            ArchiveType = "zip",
            ExecutableRelativePath = relativePath,
            ExecutableName = exeName,
            Sha256 = sha
        }]);
        tool.CheckArguments = OperatingSystem.IsWindows() ? "/?" : "";

        var resolver = new TempPathResolver(_tempRoot);
        var sut = new ToolInstallationService(resolver, new ArchiveExtractor());
        var progress = new List<ToolInstallProgressDto>();

        await sut.InstallAsync(tool, p =>
        {
            progress.Add(p);
            return Task.CompletedTask;
        });

        var last = progress.Last();
        last.State.Should().Be(ToolInstallState.Success);
        last.InstalledPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(last.InstalledPath!).Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_DownloadWithoutContentLength_ShouldStillReportProgressAndFailGracefully()
    {
        var body = Encoding.UTF8.GetBytes("plain-body");
        await using var server = await SingleResponseHttpServer.StartAsync(body, includeContentLength: false);
        var (os, arch) = ToolTestHelpers.CurrentPlatform();
        var exeName = OperatingSystem.IsWindows() ? "bad.exe" : "bad";
        var tool = ToolTestHelpers.MakeBinaryTool("nocl-tool", [new ToolInstaller
        {
            Os = os,
            Arch = arch,
            Type = "download",
            Url = server.Url,
            ArchiveType = "none",
            ExecutableRelativePath = exeName,
            ExecutableName = exeName
        }]);
        tool.CheckArguments = "";

        var progress = new List<ToolInstallProgressDto>();
        var sut = new ToolInstallationService(new TempPathResolver(_tempRoot), new ArchiveExtractor());
        await sut.InstallAsync(tool, p =>
        {
            progress.Add(p);
            return Task.CompletedTask;
        });

        progress.Should().Contain(p => p.State == ToolInstallState.Downloading && p.ProgressPercent == null);
        progress.Last().State.Should().Be(ToolInstallState.Failed);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }
}

internal sealed class FakeCatalogProvider : IToolCatalogProvider
{
    private readonly IReadOnlyCollection<ToolDefinition> _tools;

    public FakeCatalogProvider(IReadOnlyCollection<ToolDefinition> tools)
    {
        _tools = tools;
    }

    public Task<IReadOnlyCollection<ToolDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_tools);

    public Task<ToolDefinition?> GetByIdAsync(string toolId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tools.FirstOrDefault(t => t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase)));
}

internal sealed class FakeDetector : IToolDetector
{
    private readonly IReadOnlyCollection<ToolStatusDto> _statuses;

    public FakeDetector(IReadOnlyCollection<ToolStatusDto> statuses)
    {
        _statuses = statuses;
    }

    public Task<IReadOnlyCollection<ToolStatusDto>> DetectAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_statuses);

    public Task<ToolStatusDto?> DetectAsync(string toolId, CancellationToken cancellationToken = default)
        => Task.FromResult(_statuses.FirstOrDefault(s => s.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase)));
}

internal sealed class RecordingInstaller : IToolInstaller
{
    public List<ToolDefinition> Calls { get; } = [];

    public Task InstallAsync(ToolDefinition tool, Func<ToolInstallProgressDto, Task> progressCallback, CancellationToken cancellationToken = default)
    {
        Calls.Add(tool);
        return Task.CompletedTask;
    }
}

internal sealed class TempPathResolver : IToolPathResolver
{
    private readonly string _root;

    public TempPathResolver(string root)
    {
        _root = root;
    }

    public string GetToolsRootDirectory() => Path.Combine(_root, "tools");

    public string GetToolsBinDirectory() => Path.Combine(GetToolsRootDirectory(), "bin");

    public string GetToolInstallDirectory(string toolId) => Path.Combine(GetToolsRootDirectory(), toolId);

    public string GetToolExecutablePath(string toolId, string executableName) => Path.Combine(GetToolsBinDirectory(), executableName);

    public string GetToolWorkingDirectory(string toolId) => Path.Combine(_root, "work", toolId);
}

internal sealed class SingleResponseHttpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _serverTask;

    private SingleResponseHttpServer(TcpListener listener, Task serverTask, string url)
    {
        _listener = listener;
        _serverTask = serverTask;
        Url = url;
    }

    public string Url { get; }

    public static async Task<SingleResponseHttpServer> StartAsync(byte[] body, bool includeContentLength = true, int statusCode = 200)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();

            var requestBuffer = new byte[4096];
            var requestBuilder = new StringBuilder();
            while (!requestBuilder.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                var read = await stream.ReadAsync(requestBuffer.AsMemory(0, requestBuffer.Length));
                if (read <= 0) break;
                requestBuilder.Append(Encoding.ASCII.GetString(requestBuffer, 0, read));
                if (requestBuilder.Length > 32_768) break;
            }

            var headers = new StringBuilder();
            headers.Append($"HTTP/1.1 {statusCode} {(statusCode == 200 ? "OK" : "ERROR")}\r\n");
            headers.Append("Content-Type: application/octet-stream\r\n");
            if (includeContentLength)
                headers.Append($"Content-Length: {body.Length}\r\n");
            headers.Append("Connection: close\r\n\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(headers.ToString());
            await stream.WriteAsync(headerBytes);
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        });

        await Task.Delay(10);
        return new SingleResponseHttpServer(listener, serverTask, $"http://127.0.0.1:{port}/");
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        try
        {
            await _serverTask;
        }
        catch
        {
        }
    }
}

internal static class ToolTestHelpers
{
    internal static ToolDefinition MakeBinaryTool(string id, List<ToolInstaller> installers)
    {
        return new ToolDefinition
        {
            Id = id,
            DisplayName = id,
            Description = "desc",
            Category = "recon",
            Kind = "binary",
            Installers = installers
        };
    }

    internal static ToolDefinition MakeExternalTool(string id)
    {
        return new ToolDefinition
        {
            Id = id,
            DisplayName = id,
            Description = "desc",
            Category = "web",
            Kind = "externalWebApp",
            ExternalUrl = "https://example.test"
        };
    }

    internal static (string Os, string Arch) CurrentPlatform()
    {
        var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            _ => "x64"
        };
        return (os, arch);
    }

    internal static string GetKnownExecutablePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var systemDir = Environment.SystemDirectory;
            var wherePath = Path.Combine(systemDir, "where.exe");
            if (File.Exists(wherePath))
                return wherePath;

            return Path.Combine(systemDir, "cmd.exe");
        }

        if (File.Exists("/bin/true"))
            return "/bin/true";

        if (File.Exists("/usr/bin/true"))
            return "/usr/bin/true";

        throw new FileNotFoundException("Unable to locate a known executable for install verification.");
    }

    internal static byte[] BuildZip(string relativePath, byte[] content)
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(relativePath);
            using var entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }

        return ms.ToArray();
    }
}
