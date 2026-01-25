using CtfDeck.Terminal.WebSocket;
using FluentAssertions;
using System.Text;

namespace CtfDeck.Tests.WebSocket;

public class BinaryProtocolTests
{
    private static WebSocketCommand CreateCommand(string commandText, Guid messageId)
    {
        var bytes = Encoding.UTF8.GetBytes(commandText);
        return new WebSocketCommand
        {
            CommandLength = bytes.Length,
            CommandBytes = bytes,
            MessageId = messageId
        };
    }

    [Fact]
    public void WebSocketCommand_Serialize_ShouldProduceCorrectBinaryFormat()
    {
        // Arrange
        var commandText = "test command";
        var messageId = Guid.NewGuid();
        var command = CreateCommand(commandText, messageId);

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
        var originalCommandStruct = CreateCommand(originalCommand, originalMessageId);
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
        var originalCommand = CreateCommand(emptyCommand, messageId);
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
            var originalCommand = CreateCommand(commandText, messageId);

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
        var workingDirectory = "/home/user";
        var messageId = Guid.NewGuid();

        // Act
        var response = WebSocketResponse.FromResult(exitCode, output, error, workingDirectory, messageId);

        // Assert
        response.ExitCode.Should().Be(exitCode);
        response.OutputLength.Should().Be(output.Length);
        response.ErrorLength.Should().Be(error.Length);
        response.WorkingDirectoryLength.Should().Be(workingDirectory.Length);
        response.MessageId.Should().Be(messageId);
        response.Output.Should().Be(output);
        response.Error.Should().Be(error);
        response.WorkingDirectory.Should().Be(workingDirectory);
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
        response.WorkingDirectory.Should().NotBeEmpty();
    }

    [Fact]
    public void WebSocketResponse_Serialize_ShouldProduceCorrectBinaryFormat()
    {
        // Arrange
        var exitCode = 1;
        var output = "some output";
        var error = "some error message";
        var workingDirectory = "/home/user/project";
        var messageId = Guid.NewGuid();
        var response = WebSocketResponse.FromResult(exitCode, output, error, workingDirectory, messageId);

        // Act
        var binaryData = response.Serialize();

        // Assert
        binaryData.Should().NotBeNull();
        binaryData.Length.Should().BeGreaterThan(0);

        // Verify structure: [1 byte type] + [4 bytes exitCode] + [4 bytes outputLength] + [output bytes] + 
        //                  [4 bytes errorLength] + [error bytes] + [4 bytes wdLength] + [wd bytes] + [16 bytes UUID]
        var expectedLength = 1 + 4 + 4 + output.Length + 4 + error.Length + 4 + workingDirectory.Length + 16;
        binaryData.Length.Should().Be(expectedLength);
    }

    [Fact]
    public void WebSocketResponse_Deserialize_ShouldRestoreOriginalResponse()
    {
        // Arrange
        var originalExitCode = 127;
        var originalOutput = "command not found";
        var originalError = "bash: nonexistentcommand: command not found";
        var originalWorkingDirectory = "/home/user";
        var originalMessageId = Guid.NewGuid();
        var originalResponse = WebSocketResponse.FromResult(originalExitCode, originalOutput, originalError, originalWorkingDirectory, originalMessageId);
        var binaryData = originalResponse.Serialize();

        // Act
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.ExitCode.Should().Be(originalExitCode);
        deserializedResponse.Output.Should().Be(originalOutput);
        deserializedResponse.Error.Should().Be(originalError);
        deserializedResponse.WorkingDirectory.Should().Be(originalWorkingDirectory);
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
        var workingDirectory = "/home/user";
        var messageId = Guid.NewGuid();
        var originalResponse = WebSocketResponse.FromResult(exitCode, emptyOutput, emptyError, workingDirectory, messageId);
        var binaryData = originalResponse.Serialize();

        // Act
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.ExitCode.Should().Be(exitCode);
        deserializedResponse.Output.Should().Be(emptyOutput);
        deserializedResponse.Error.Should().Be(emptyError);
        deserializedResponse.WorkingDirectory.Should().Be(workingDirectory);
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
            (ExitCode: 0, Output: "Success", Error: "", WorkingDirectory: "/home/user", MessageId: Guid.NewGuid()),
            (ExitCode: 1, Output: "", Error: "Permission denied", WorkingDirectory: "/var/log", MessageId: Guid.NewGuid()),
            (ExitCode: 127, Output: "", Error: "Command not found", WorkingDirectory: "/tmp", MessageId: Guid.NewGuid()),
            (ExitCode: 2, Output: "Line 1\nLine 2\nLine 3", Error: "Warning message", WorkingDirectory: "/root", MessageId: Guid.NewGuid())
        };

        foreach (var testCase in testCases)
        {
            var originalResponse = WebSocketResponse.FromResult(testCase.ExitCode, testCase.Output, testCase.Error, testCase.WorkingDirectory, testCase.MessageId);

            // Act
            var binaryData = originalResponse.Serialize();
            var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

            // Assert
            deserializedResponse.ExitCode.Should().Be(testCase.ExitCode);
            deserializedResponse.Output.Should().Be(testCase.Output);
            deserializedResponse.Error.Should().Be(testCase.Error);
            deserializedResponse.WorkingDirectory.Should().Be(testCase.WorkingDirectory);
            deserializedResponse.MessageId.Should().Be(testCase.MessageId);
        }
    }

    [Fact]
    public void WebSocketCommand_Serialize_WithUnicodeText_ShouldWork()
    {
        // Arrange
        var unicodeCommand = "echo '🚀 Hello 世界 🌍'";
        var messageId = Guid.NewGuid();
        var command = CreateCommand(unicodeCommand, messageId);

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
        var workingDirectory = "/home/用户";
        var messageId = Guid.NewGuid();
        var response = WebSocketResponse.FromResult(1, unicodeOutput, unicodeError, workingDirectory, messageId);

        // Act
        var binaryData = response.Serialize();
        var deserializedResponse = WebSocketResponse.Deserialize(binaryData);

        // Assert
        deserializedResponse.Output.Should().Be(unicodeOutput);
        deserializedResponse.Error.Should().Be(unicodeError);
        deserializedResponse.WorkingDirectory.Should().Be(workingDirectory);
        deserializedResponse.MessageId.Should().Be(messageId);
        deserializedResponse.ExitCode.Should().Be(1);
    }

    [Fact]
    public void StreamChunkMessage_Serialize_ShouldWork()
    {
        // Arrange
        var data = "Hello streaming!";
        var messageId = Guid.NewGuid();
        var chunk = StreamChunkMessage.FromData(MessageType.StreamOutput, data, messageId);

        // Act
        var binaryData = chunk.Serialize();

        // Assert
        binaryData.Should().NotBeNull();
        binaryData.Length.Should().BeGreaterThan(0);
        binaryData[0].Should().Be((byte)MessageType.StreamOutput);
    }

    [Fact]
    public void StreamEndMessage_Serialize_ShouldWork()
    {
        // Arrange
        var exitCode = 0;
        var workingDirectory = "/home/user";
        var messageId = Guid.NewGuid();
        var endMessage = StreamEndMessage.FromResult(exitCode, workingDirectory, messageId);

        // Act
        var binaryData = endMessage.Serialize();

        // Assert
        binaryData.Should().NotBeNull();
        binaryData.Length.Should().BeGreaterThan(0);
        binaryData[0].Should().Be((byte)MessageType.StreamEnd);
    }
}