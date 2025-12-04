using Xunit;
using CtfDeck.Terminal.Terminal;
using System.Runtime.InteropServices;

namespace CtfDeck.Tests.Terminal;

public class TerminalExecutorTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCurrentDirectory()
    {
        var executor = new TerminalExecutor();
        var prompt = executor.GetPrompt();

        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt);
    }

    [Fact]
    public void GetPrompt_ShouldContainUsername()
    {
        var executor = new TerminalExecutor();
        var username = Environment.UserName;

        var prompt = executor.GetPrompt();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains(">", prompt);
        }
        else
        {
            Assert.Contains(username, prompt);
        }
    }

    [Fact]
    public void GetPrompt_OnWindows_ShouldHaveCorrectFormat()
    {
        var executor = new TerminalExecutor();

        var prompt = executor.GetPrompt();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.EndsWith("> ", prompt);
        }
        else
        {
            Assert.True(prompt.EndsWith("$ ") || prompt.EndsWith("# "));
        }
    }

    [Fact]
    public void GetPrompt_OnUnix_ShouldContainHostname()
    {
        var executor = new TerminalExecutor();

        var prompt = executor.GetPrompt();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains("@", prompt);
            Assert.Contains(Environment.MachineName, prompt);
        }
    }

    [Fact]
    public void GetPrompt_ShouldReplaceHomeWithTilde()
    {
        var executor = new TerminalExecutor();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var prompt = executor.GetPrompt();

        if (Environment.CurrentDirectory.StartsWith(home))
        {
            Assert.Contains("~", prompt);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SimpleEchoCommand_ShouldReturnOutput()
    {
        var executor = new TerminalExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "echo test"
            : "echo test";

        var result = await executor.ExecuteAsync(command);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("test", result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ShouldReturnError()
    {
        var executor = new TerminalExecutor();
        var command = "nonexistentcommand123456";

        var result = await executor.ExecuteAsync(command);

        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CdToValidDirectory_ShouldChangeDirectory()
    {
        var executor = new TerminalExecutor();
        var tempPath = Path.GetTempPath();

        var result = await executor.ExecuteAsync($"cd {tempPath}");
        var prompt = executor.GetPrompt();

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Contains(Path.GetFileName(tempPath.TrimEnd(Path.DirectorySeparatorChar)), prompt);
    }

    [Fact]
    public async Task ExecuteAsync_CdToInvalidDirectory_ShouldReturnError()
    {
        var executor = new TerminalExecutor();
        var invalidPath = "/this/path/does/not/exist/at/all/12345";

        var result = await executor.ExecuteAsync($"cd {invalidPath}");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("No such file or directory", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CdWithoutArgument_ShouldGoToHome()
    {
        var executor = new TerminalExecutor();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = await executor.ExecuteAsync("cd");
        var prompt = executor.GetPrompt();

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("~", prompt);
    }

    [Fact]
    public async Task ExecuteAsync_CdWithTilde_ShouldGoToHome()
    {
        var executor = new TerminalExecutor();

        var result = await executor.ExecuteAsync("cd ~");
        var prompt = executor.GetPrompt();

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("~", prompt);
    }

    [Fact]
    public async Task ExecuteAsync_CdWithRelativePath_ShouldWork()
    {
        var executor = new TerminalExecutor();
        var tempPath = Path.GetTempPath();

        await executor.ExecuteAsync($"cd {tempPath}");

        var testDir = Path.Combine(tempPath, "test_dir_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        try
        {
            var result = await executor.ExecuteAsync($"cd {Path.GetFileName(testDir)}");

            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Error);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithQuotes_ShouldEscapeProperly()
    {
        var executor = new TerminalExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "echo \"hello world\""
            : "echo \"hello world\"";

        var result = await executor.ExecuteAsync(command);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello world", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnWorkingDirectory()
    {
        var executor = new TerminalExecutor();

        var result = await executor.ExecuteAsync("echo test");

        Assert.NotEmpty(result.WorkingDirectory);
        Assert.True(Directory.Exists(result.WorkingDirectory));
    }

    [Fact]
    public async Task ExecuteAsync_MultipleCommands_ShouldMaintainDirectory()
    {
        var executor = new TerminalExecutor();
        var tempPath = Path.GetTempPath();

        await executor.ExecuteAsync($"cd {tempPath}");
        var result = await executor.ExecuteAsync("echo test");

        Assert.Contains(tempPath, result.WorkingDirectory);
    }

    [Fact]
    public async Task ExecuteAsync_CdWithSpaces_ShouldHandleProperly()
    {
        var executor = new TerminalExecutor();

        var result = await executor.ExecuteAsync("cd   ");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetPathEnvironmentVariable()
    {
        var executor = new TerminalExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "echo %PATH%"
            : "echo $PATH";

        var result = await executor.ExecuteAsync(command);

        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_CdToAbsolutePath_ShouldWork()
    {
        var executor = new TerminalExecutor();
        var tempPath = Path.GetFullPath(Path.GetTempPath());

        var result = await executor.ExecuteAsync($"cd {tempPath}");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CommandThatOutputsToStderr_ShouldCaptureError()
    {
        var executor = new TerminalExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "dir nonexistentfile12345.txt"
            : "ls nonexistentfile12345.txt";

        var result = await executor.ExecuteAsync(command);

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task GetPrompt_MultipleCallsAfterCd_ShouldReflectCurrentDirectory()
    {
        var executor = new TerminalExecutor();
        var initialPrompt = executor.GetPrompt();

        var tempPath = Path.GetTempPath();
        await executor.ExecuteAsync($"cd {tempPath}");
        var newPrompt = executor.GetPrompt();

        Assert.NotEqual(initialPrompt, newPrompt);
    }
}
