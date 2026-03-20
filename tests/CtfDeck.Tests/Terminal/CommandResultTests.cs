using CtfDeck.Terminal.Terminal.Core;

namespace CtfDeck.Tests.Terminal;

public class CommandResultTests
{
    [Fact]
    public void CommandResult_Success_ShouldHaveCorrectDefaults()
    {
        var wd = "/home/user";
        var result = CommandResult.Success(wd);

        Assert.Equal("", result.Output);
        Assert.Equal("", result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(wd, result.WorkingDirectory);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CommandResult_Failure_ShouldHaveCorrectDefaults()
    {
        var wd = "/home/user";
        var error = "Some error";
        var result = CommandResult.Failure(wd, error);

        Assert.Equal("", result.Output);
        Assert.Equal(error, result.Error);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(wd, result.WorkingDirectory);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CommandResult_Initialization_ShouldStoreValues()
    {
        var result = new CommandResult
        {
            Output = "Output",
            Error = "Error",
            ExitCode = 123,
            WorkingDirectory = "/wd"
        };

        Assert.Equal("Output", result.Output);
        Assert.Equal("Error", result.Error);
        Assert.Equal(123, result.ExitCode);
        Assert.Equal("/wd", result.WorkingDirectory);
    }

    [Fact]
    public void CommandResult_Success_WithOutput_ShouldStoreOutput()
    {
        var wd = "/wd";
        var output = "output";
        var result = CommandResult.Success(wd, output);

        Assert.Equal(output, result.Output);
        Assert.Equal(wd, result.WorkingDirectory);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void CommandResult_Failure_WithCustomExitCode_ShouldStoreExitCode()
    {
        var wd = "/wd";
        var error = "error";
        var exitCode = 5;
        var result = CommandResult.Failure(wd, error, exitCode);

        Assert.Equal(error, result.Error);
        Assert.Equal(exitCode, result.ExitCode);
        Assert.Equal(wd, result.WorkingDirectory);
    }
}