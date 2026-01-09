using System.Text;

namespace CtfDeck.Terminal.WebSocket;

/// <summary>
/// Message types for the binary protocol
/// </summary>
public enum MessageType : byte
{
    /// <summary>Legacy complete response (backward compatible)</summary>
    CompleteResponse = 0,
    /// <summary>Streaming output chunk (stdout)</summary>
    StreamOutput = 1,
    /// <summary>Streaming error chunk (stderr)</summary>
    StreamError = 2,
    /// <summary>Stream completed with exit code and working directory</summary>
    StreamEnd = 3
}

public struct WebSocketCommand
{
    public int CommandLength;
    public byte[] CommandBytes;
    public Guid MessageId;

    public string Command => Encoding.UTF8.GetString(CommandBytes);

    public static WebSocketCommand FromCommand(string command, Guid messageId)
    {
        var commandBytes = Encoding.UTF8.GetBytes(command);
        return new WebSocketCommand
        {
            CommandLength = commandBytes.Length,
            CommandBytes = commandBytes,
            MessageId = messageId
        };
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(CommandLength);
        writer.Write(CommandBytes);
        writer.Write(MessageId.ToByteArray());

        return stream.ToArray();
    }

    public static WebSocketCommand Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var commandLength = reader.ReadInt32();
        var commandBytes = reader.ReadBytes(commandLength);
        var messageIdBytes = reader.ReadBytes(16);

        return new WebSocketCommand
        {
            CommandLength = commandLength,
            CommandBytes = commandBytes,
            MessageId = new Guid(messageIdBytes)
        };
    }
}

/// <summary>
/// Streaming output chunk message
/// </summary>
public struct StreamChunkMessage
{
    public MessageType Type;
    public Guid MessageId;
    public int DataLength;
    public byte[] DataBytes;

    public string Data => Encoding.UTF8.GetString(DataBytes);

    public static StreamChunkMessage FromData(MessageType type, string data, Guid messageId)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return new StreamChunkMessage
        {
            Type = type,
            MessageId = messageId,
            DataLength = dataBytes.Length,
            DataBytes = dataBytes
        };
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)Type);
        writer.Write(MessageId.ToByteArray());
        writer.Write(DataLength);
        writer.Write(DataBytes);

        return stream.ToArray();
    }
}

/// <summary>
/// Stream end message with exit code and working directory
/// </summary>
public struct StreamEndMessage
{
    public MessageType Type;
    public Guid MessageId;
    public int ExitCode;
    public int WorkingDirectoryLength;
    public byte[] WorkingDirectoryBytes;

    public string WorkingDirectory => Encoding.UTF8.GetString(WorkingDirectoryBytes);

    public static StreamEndMessage FromResult(int exitCode, string workingDirectory, Guid messageId)
    {
        var workingDirectoryBytes = Encoding.UTF8.GetBytes(workingDirectory);
        return new StreamEndMessage
        {
            Type = MessageType.StreamEnd,
            MessageId = messageId,
            ExitCode = exitCode,
            WorkingDirectoryLength = workingDirectoryBytes.Length,
            WorkingDirectoryBytes = workingDirectoryBytes
        };
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)Type);
        writer.Write(MessageId.ToByteArray());
        writer.Write(ExitCode);
        writer.Write(WorkingDirectoryLength);
        writer.Write(WorkingDirectoryBytes);

        return stream.ToArray();
    }
}

/// <summary>
/// Legacy complete response (kept for backward compatibility and simple commands like cd)
/// </summary>
public struct WebSocketResponse
{
    public int ExitCode;
    public int OutputLength;
    public byte[] OutputBytes;
    public int ErrorLength;
    public byte[] ErrorBytes;
    public int WorkingDirectoryLength;
    public byte[] WorkingDirectoryBytes;
    public Guid MessageId;

    public string Output => Encoding.UTF8.GetString(OutputBytes);
    public string Error => Encoding.UTF8.GetString(ErrorBytes);
    public string WorkingDirectory => Encoding.UTF8.GetString(WorkingDirectoryBytes);

    public static WebSocketResponse FromResult(int exitCode, string output, string error, string workingDirectory, Guid messageId)
    {
        var outputBytes = Encoding.UTF8.GetBytes(output);
        var errorBytes = Encoding.UTF8.GetBytes(error);
        var workingDirectoryBytes = Encoding.UTF8.GetBytes(workingDirectory);

        return new WebSocketResponse
        {
            ExitCode = exitCode,
            OutputLength = outputBytes.Length,
            OutputBytes = outputBytes,
            ErrorLength = errorBytes.Length,
            ErrorBytes = errorBytes,
            WorkingDirectoryLength = workingDirectoryBytes.Length,
            WorkingDirectoryBytes = workingDirectoryBytes,
            MessageId = messageId
        };
    }

    public static WebSocketResponse MockResponse(Guid messageId)
    {
        return FromResult(0, "Mock response from WebSocket server", "", Environment.CurrentDirectory, messageId);
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write message type first (0 = CompleteResponse for backward compatibility)
        writer.Write((byte)MessageType.CompleteResponse);
        writer.Write(ExitCode);
        writer.Write(OutputLength);
        writer.Write(OutputBytes);
        writer.Write(ErrorLength);
        writer.Write(ErrorBytes);
        writer.Write(WorkingDirectoryLength);
        writer.Write(WorkingDirectoryBytes);
        writer.Write(MessageId.ToByteArray());

        return stream.ToArray();
    }

    public static WebSocketResponse Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Skip message type byte (already checked by caller or assume CompleteResponse)
        var messageType = reader.ReadByte();
        
        var exitCode = reader.ReadInt32();
        var outputLength = reader.ReadInt32();
        var outputBytes = reader.ReadBytes(outputLength);
        var errorLength = reader.ReadInt32();
        var errorBytes = reader.ReadBytes(errorLength);
        var workingDirectoryLength = reader.ReadInt32();
        var workingDirectoryBytes = reader.ReadBytes(workingDirectoryLength);
        var messageIdBytes = reader.ReadBytes(16);

        return new WebSocketResponse
        {
            ExitCode = exitCode,
            OutputLength = outputLength,
            OutputBytes = outputBytes,
            ErrorLength = errorLength,
            ErrorBytes = errorBytes,
            WorkingDirectoryLength = workingDirectoryLength,
            WorkingDirectoryBytes = workingDirectoryBytes,
            MessageId = new Guid(messageIdBytes)
        };
    }
}