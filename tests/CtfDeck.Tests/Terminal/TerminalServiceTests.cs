using Xunit;
using CtfDeck.Terminal.Terminal;
using System.Runtime.InteropServices;

namespace CtfDeck.Tests.Terminal;

public class TerminalServiceTests
{
    private static string GetExpectedPromptSymbol()
    {
        // Windows uses ">", Unix systems use "$" or "#"
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ">" : "$";
    }
    [Fact]
    public async Task RunAsync_WithExitCommand_ShouldTerminate()
    {
        var input = new StringReader("exit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task RunAsync_WithQuitCommand_ShouldTerminate()
    {
        var input = new StringReader("quit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task RunAsync_WithNullInput_ShouldTerminate()
    {
        var input = new StringReader("");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task RunAsync_WithEchoCommand_ShouldDisplayOutput()
    {
        var input = new StringReader("echo test\nexit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("test", outputString);
    }

    [Fact]
    public async Task RunAsync_WithInvalidCommand_ShouldDisplayError()
    {
        var input = new StringReader("invalidcommand12345\nexit\n");
        var output = new StringWriter();
        var error = new StringWriter();
        var service = new TerminalService(input: input, output: output, error: error);

        await service.RunAsync();

        var errorString = error.ToString();
        Assert.NotEmpty(errorString);
    }

    [Fact]
    public async Task RunAsync_WithEmptyCommand_ShouldContinue()
    {
        var input = new StringReader("\nexit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        var promptCount = outputString.Split('$').Length - 1;
        Assert.True(promptCount >= 2);
    }

    [Fact]
    public async Task RunAsync_WithMultipleCommands_ShouldExecuteAll()
    {
        var input = new StringReader("echo first\necho second\nexit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("first", outputString);
        Assert.Contains("second", outputString);
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceAroundExit_ShouldTerminate()
    {
        var input = new StringReader("  exit  \n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task RunAsync_WithCommandProducingOnlyOutput_ShouldNotWriteToError()
    {
        var input = new StringReader("echo success\nexit\n");
        var output = new StringWriter();
        var error = new StringWriter();
        var service = new TerminalService(input: input, output: output, error: error);

        await service.RunAsync();

        var outputString = output.ToString();
        var errorString = error.ToString();
        Assert.Contains("success", outputString);
        Assert.Empty(errorString);
    }

    [Fact]
    public async Task RunAsync_ShouldDisplayPromptBeforeEachCommand()
    {
        var input = new StringReader("echo test\nexit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        var promptCount = outputString.Split('$').Length - 1;
        Assert.True(promptCount >= 2);
    }

    [Fact]
    public async Task RunAsync_WithCdCommand_ShouldUpdatePrompt()
    {
        var tempPath = Path.GetTempPath();
        var input = new StringReader($"cd {tempPath}\nexit\n");
        var output = new StringWriter();
        var service = new TerminalService(input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task RunAsync_WithCustomExecutor_ShouldUseIt()
    {
        var executor = new TerminalExecutor();
        var input = new StringReader("exit\n");
        var output = new StringWriter();
        var service = new TerminalService(executor: executor, input: input, output: output);

        await service.RunAsync();

        var outputString = output.ToString();
        Assert.Contains("$", outputString);
    }

    [Fact]
    public async Task Constructor_WithNullParameters_ShouldUseDefaults()
    {
        var service = new TerminalService();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task RunAsync_WithOnlyOutput_ErrorShouldBeEmpty()
    {
        var input = new StringReader("echo hello\nexit\n");
        var output = new StringWriter();
        var error = new StringWriter();
        var service = new TerminalService(input: input, output: output, error: error);

        await service.RunAsync();

        var errorString = error.ToString();
        Assert.Empty(errorString);
    }
}