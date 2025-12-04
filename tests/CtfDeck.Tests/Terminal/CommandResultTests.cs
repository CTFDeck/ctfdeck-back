using Xunit;
using CtfDeck.Terminal.Terminal;

namespace CtfDeck.Tests.Terminal;

public class CommandResultTests
{
    [Fact]
    public void CommandResult_DefaultValues_ShouldBeInitialized()
    {
        var result = new CommandResult();

        Assert.Equal("", result.Output);
        Assert.Equal("", result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.WorkingDirectory);
    }

    [Fact]
    public void CommandResult_SetOutput_ShouldStoreValue()
    {
        var result = new CommandResult();
        var expectedOutput = "Test output";

        result.Output = expectedOutput;

        Assert.Equal(expectedOutput, result.Output);
    }

    [Fact]
    public void CommandResult_SetError_ShouldStoreValue()
    {
        var result = new CommandResult();
        var expectedError = "Test error";

        result.Error = expectedError;

        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void CommandResult_SetExitCode_ShouldStoreValue()
    {
        var result = new CommandResult();
        var expectedExitCode = 42;

        result.ExitCode = expectedExitCode;

        Assert.Equal(expectedExitCode, result.ExitCode);
    }

    [Fact]
    public void CommandResult_SetWorkingDirectory_ShouldStoreValue()
    {
        var result = new CommandResult();
        var expectedDirectory = "/home/user";

        result.WorkingDirectory = expectedDirectory;

        Assert.Equal(expectedDirectory, result.WorkingDirectory);
    }

    [Fact]
    public void CommandResult_SetAllProperties_ShouldStoreAllValues()
    {
        var result = new CommandResult
        {
            Output = "Success message",
            Error = "Warning message",
            ExitCode = 1,
            WorkingDirectory = "/var/log"
        };

        Assert.Equal("Success message", result.Output);
        Assert.Equal("Warning message", result.Error);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("/var/log", result.WorkingDirectory);
    }
}