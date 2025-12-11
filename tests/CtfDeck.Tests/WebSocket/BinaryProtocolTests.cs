using CtfDeck.Terminal.WebSocket;
using FluentAssertions;

namespace CtfDeck.Tests.WebSocket;

public class BinaryProtocolTests
{
    [Fact]
    public void WebSocketCommand_FromCommand_ShouldCreateCorrectCommand()
    {
        // Arrange
        var commandText = "echo 'hello world'";
        var messageId = Guid.NewGuid();

        // Act
        var command = WebSocketCommand.FromCommand(commandText, messageId);

        // Assert
        command.CommandLength.Should().Be(commandText.Length);
        command.CommandBytes.Should().NotBeNull();
        command.MessageId.Should().Be(messageId);
        command.Command.Should().Be(commandText);
    }

    [Fact]
    public void WebSocketCommand_Serialize_ShouldProduceCorrectBinaryFormat()
    {
        // Arrange
        var commandText = "test command";
        var messageId = Guid.NewGuid();
        var command = WebSocketCommand.FromCommand(commandText, messageId);

        // Act
        var binaryData = command.Serialize();

        // Assert
        binaryData.Should().NotBeNull();
        binaryData.Length.Should().BeGreaterThan(0);

        // Verify structure: [4 bytes length] + [command bytes] + [16 bytes UUID]
        binaryData.Length.Should().Be(4 + commandText.Length + 16);
    }

    [Fact]
    public void WebSocketCommand_Deserialize_ShouldRestoreOriginalCommand()
    {
        // Arrange
        var originalCommand = "ls -la /home/user";
        var originalMessageId = Guid.NewGuid();
        var originalCommandStruct = WebSocketCommand.FromCommand(originalCommand, originalMessageId);
        var binaryData = originalCommandStruct.Serialize();

        // Act
        var deserializedCommand = WebSocketCommand.Deserialize(binaryData);

        // Assert
        deserializedCommand.CommandLength.Should().Be(originalCommand.Length);
        deserializedCommand.Command.Should().Be(originalCommand);
        deserializedCommand.MessageId.Should().Be(originalMessageId);
    }

    [Fact]
    public void WebSocketCommand_Deserialize_EmptyCommand_ShouldWork()
    {
        // Arrange
        var emptyCommand = "";
        var messageId = Guid.NewGuid();
        var originalCommand = WebSocketCommand.FromCommand(emptyCommand, messageId);
        var binaryData = originalCommand.Serialize();

        // Act
        var deserializedCommand = WebSocketCommand.Deserialize(binaryData);

        // Assert
        deserializedCommand.CommandLength.Should().Be(0);
        deserializedCommand.Command.Should().Be(emptyCommand);
        deserializedCommand.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void WebSocketCommand_RoundTrip_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var commands = new[]
        {
            "echo hello",
            "ls -la /var/log",
            "grep -r \"pattern\" /etc/",
            "curl -X POST https://api.example.com/data",
            "docker run --rm -it ubuntu:latest bash"
        };

        foreach (var commandText in commands)
        {
            var messageId = Guid.NewGuid();
            var originalCommand = WebSocketCommand.FromCommand(commandText, messageId);

            // Act
            var binaryData = originalCommand.Serialize();
            var deserializedCommand = WebSocketCommand.Deserialize(binaryData);

            // Assert
            deserializedCommand.Command.Should().Be(commandText);
            deserializedCommand.MessageId.Should().Be(messageId);
            deserializedCommand.CommandLength.Should().Be(commandText.Length);
        }
    }

    [Fact]
    public void WebSocketResponse_FromResult_ShouldCreateCorrectResponse()
    {
        // Arrange
        var exitCode = 0;
        var output = "file1.txt\nfile2.txt\n";
        var error = "";
        var messageId = Guid.NewGuid();

        // Act
        var response = WebSocketResponse.FromResult(exitCode, output, error, messageId);

        // Assert
        response.ExitCode.Should().Be(exitCode);
        response.OutputLength.Should().Be(output.Length);
        response.ErrorLength.Should().Be(error.Length);
        response.MessageId.Should().Be(messageId);
        response.Output.Should().Be(output);
        response.Error.Should().Be(error);
    }

    [Fact]
    public void WebSocketResponse_MockResponse_ShouldCreateMockResponse()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var response = WebSocketResponse.MockResponse(messageId);

        // Assert
        response.ExitCode.Should().Be(0);
        response.Output.Should().Contain("Mock response");
        response.Error.Should().BeEmpty();
        response.MessageId.Should().Be(messageId);
        response.OutputLength.Should().Be(response.Output.Length);
        response.ErrorLength.Should().Be(0);
    }

    [Fact]
    public void WebSocketResponse_Serialize_ShouldProduceCorrectBinaryFormat()
    {
        // Arrange
        var exitCode = 1;
        var output = "some output";
        var error = "some error message";
        var messageId = Guid.NewGuid();
        var response = WebSocketResponse.FromResult(exitCode, output, error, messageId);

        // Act
        var binaryData = response.Serialize();

        // Assert
        binaryData.Should().NotBeNull();
        binaryData.Length.Should().BeGreaterThan(0);

        // Verify structure: [4 bytes exitCode] + [4 bytes outputLength] + [output bytes] + [4 bytes errorLength] + [error bytes] + [16 bytes UUID]
        binaryData.Length.Should().Be(4 + 4 + output.Length + 4 + error.Length + 16);
    }

    [Fact]
    public void WebSocketResponse_Deserialize_ShouldRestoreOriginalResponse()
    {
        // Arrange
        var originalExitCode = 127;
        var originalOutput = "command not found";
        var originalError = "bash: nonexistentcommand: command not found";
        var originalMessageId = Guid.NewGuid();
        var originalResponse = WebSocketResponse.FromResult(originalExitCode, originalOutput, originalError, originalMessageId);
        var binaryData = originalResponse.Serialize();

        // Act
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.ExitCode.Should().Be(originalExitCode);
        deserializedResponse.Output.Should().Be(originalOutput);
        deserializedResponse.Error.Should().Be(originalError);
        deserializedResponse.MessageId.Should().Be(originalMessageId);
        deserializedResponse.OutputLength.Should().Be(originalOutput.Length);
        deserializedResponse.ErrorLength.Should().Be(originalError.Length);
    }

    [Fact]
    public void WebSocketResponse_Deserialize_EmptyOutputAndError_ShouldWork()
    {
        // Arrange
        var exitCode = 0;
        var emptyOutput = "";
        var emptyError = "";
        var messageId = Guid.NewGuid();
        var originalResponse = WebSocketResponse.FromResult(exitCode, emptyOutput, emptyError, messageId);
        var binaryData = originalResponse.Serialize();

        // Act
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.ExitCode.Should().Be(exitCode);
        deserializedResponse.Output.Should().Be(emptyOutput);
        deserializedResponse.Error.Should().Be(emptyError);
        deserializedResponse.OutputLength.Should().Be(0);
        deserializedResponse.ErrorLength.Should().Be(0);
        deserializedResponse.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void WebSocketResponse_RoundTrip_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var testCases = new[]
        {
            (ExitCode: 0, Output: "Success", Error: "", MessageId: Guid.NewGuid()),
            (ExitCode: 1, Output: "", Error: "Permission denied", MessageId: Guid.NewGuid()),
            (ExitCode: 127, Output: "", Error: "Command not found", MessageId: Guid.NewGuid()),
            (ExitCode: 2, Output: "Line 1\nLine 2\nLine 3", Error: "Warning message", MessageId: Guid.NewGuid())
        };

        foreach (var testCase in testCases)
        {
            var originalResponse = WebSocketResponse.FromResult(testCase.ExitCode, testCase.Output, testCase.Error, testCase.MessageId);

            // Act
            var binaryData = originalResponse.Serialize();
            var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

            // Assert
            deserializedResponse.ExitCode.Should().Be(testCase.ExitCode);
            deserializedResponse.Output.Should().Be(testCase.Output);
            deserializedResponse.Error.Should().Be(testCase.Error);
            deserializedResponse.MessageId.Should().Be(testCase.MessageId);
        }
    }

    [Fact]
    public void WebSocketCommand_Serialize_WithUnicodeText_ShouldWork()
    {
        // Arrange
        var unicodeCommand = "echo '🚀 Hello 世界 🌍'";
        var messageId = Guid.NewGuid();
        var command = WebSocketCommand.FromCommand(unicodeCommand, messageId);

        // Act
        var binaryData = command.Serialize();
        var deserializedCommand = WebSocketCommand.Deserialize(binaryData);

        // Assert
        deserializedCommand.Command.Should().Be(unicodeCommand);
        deserializedCommand.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void WebSocketResponse_Serialize_WithUnicodeText_ShouldWork()
    {
        // Arrange
        var unicodeOutput = "Files: 📄📁📋\nDone! ✅";
        var unicodeError = "Erreur: ⚠️ Accès refusé";
        var messageId = Guid.NewGuid();
        var response = WebSocketResponse.FromResult(1, unicodeOutput, unicodeError, messageId);

        // Act
        var binaryData = response.Serialize();
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.Output.Should().Be(unicodeOutput);
        deserializedResponse.Error.Should().Be(unicodeError);
        deserializedResponse.MessageId.Should().Be(messageId);
        deserializedResponse.ExitCode.Should().Be(1);
    }
}