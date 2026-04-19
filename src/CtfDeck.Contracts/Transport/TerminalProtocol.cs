using System.Text;

namespace CtfDeck.Contracts.Transport;

public sealed class WebSocketCommand
{
    public Guid MessageId { get; set; }
    public string Command { get; set; } = string.Empty;
    public int CommandLength { get; set; } // For test compatibility
    public byte[] CommandBytes { get; set; } = Array.Empty<byte>(); // For test compatibility

    public static WebSocketCommand Deserialize(byte[] data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type: CommandExecute
        var cmd = reader.ReadString();
        var id = reader.ReadGuid();
        return new WebSocketCommand
        {
            MessageId = id,
            Command = cmd,
            CommandLength = cmd.Length,
            CommandBytes = Encoding.UTF8.GetBytes(cmd)
        };
    }

    public byte[] Serialize()
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandExecute);
        writer.WriteString(Command);
        writer.WriteGuid(MessageId);
        return writer.ToArray();
    }
}

public sealed class WebSocketResponse
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public Guid MessageId { get; set; }

    // For test compatibility
    public int OutputLength => Output.Length;
    public int ErrorLength => Error.Length;
    public int WorkingDirectoryLength => WorkingDirectory.Length;

    public static WebSocketResponse FromResult(int exitCode, string output, string error, string workingDir, Guid messageId)
    {
        return new WebSocketResponse
        {
            ExitCode = exitCode,
            Output = output,
            Error = error,
            WorkingDirectory = workingDir,
            MessageId = messageId
        };
    }

    public static WebSocketResponse MockResponse(Guid messageId)
    {
        return FromResult(0, "Mock response", "", "/home/user", messageId);
    }

    public static WebSocketResponse Deserialize(byte[] data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type: CompleteResponse
        var exitCode = reader.ReadInt32();
        var output = reader.ReadString();
        var error = reader.ReadString();
        var workingDir = reader.ReadString();
        var messageId = reader.ReadGuid();
        return FromResult(exitCode, output, error, workingDir, messageId);
    }

    public byte[] Serialize()
    {
        return TerminalProtocolSerializer.SerializeCompleteResponse(MessageId, ExitCode, Output, Error, WorkingDirectory);
    }
}

public sealed class StreamChunkMessage
{
    public MessageType Type { get; set; }
    public string Data { get; set; } = string.Empty;
    public Guid MessageId { get; set; }

    public static StreamChunkMessage FromData(MessageType type, string data, Guid messageId)
    {
        return new StreamChunkMessage { Type = type, Data = data, MessageId = messageId };
    }

    public static StreamChunkMessage Deserialize(byte[] data)
    {
        var reader = new BinaryProtocolReader(data);
        var type = (MessageType)reader.ReadByte();
        var id = reader.ReadGuid();
        var content = reader.ReadString();
        return new StreamChunkMessage { Type = type, MessageId = id, Data = content };
    }

    public byte[] Serialize()
    {
        return TerminalProtocolSerializer.SerializeStreamChunk(Type, MessageId, Data);
    }
}

public sealed class StreamEndMessage
{
    public Guid MessageId { get; set; }
    public int ExitCode { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;

    public static StreamEndMessage FromResult(int exitCode, string workingDirectory, Guid messageId)
    {
        return new StreamEndMessage { ExitCode = exitCode, WorkingDirectory = workingDirectory, MessageId = messageId };
    }

    public static StreamEndMessage Deserialize(byte[] data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type
        var id = reader.ReadGuid();
        var exitCode = reader.ReadInt32();
        var workingDir = reader.ReadString();
        return new StreamEndMessage { MessageId = id, ExitCode = exitCode, WorkingDirectory = workingDir };
    }

    public byte[] Serialize()
    {
        return TerminalProtocolSerializer.SerializeStreamEnd(MessageId, ExitCode, WorkingDirectory);
    }
}

public readonly ref struct WebSocketCommandReader
{
    public readonly Guid MessageId;
    public readonly string Command;
    public readonly int CommandLength;

    public WebSocketCommandReader(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type
        Command = reader.ReadString();
        CommandLength = Command.Length;
        MessageId = reader.ReadGuid();
    }

    public string GetCommand() => Command;
}

public readonly ref struct CommandKillReader
{
    public readonly Guid CommandId;
    public CommandKillReader(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type: CommandKill (6)
        CommandId = reader.ReadGuid();
    }
}

public enum CommandSignalKind : byte
{
    Interrupt = 0,
    Eof = 1
}

public readonly ref struct CommandSignalReader
{
    public readonly Guid CommandId;
    public readonly CommandSignalKind Kind;

    public CommandSignalReader(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        reader.ReadByte(); // type: CommandSignal (9)
        CommandId = reader.ReadGuid();
        Kind = (CommandSignalKind)reader.ReadByte();
    }
}

public readonly ref struct PasswordProvideReader
{
    public readonly Guid MessageId;
    public readonly string Password;
    public int PasswordLength => Password.Length;

    public PasswordProvideReader(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Password = reader.ReadString();
    }

    public string GetPassword() => Password;
}

public readonly ref struct TerminalStreamChunk
{
    public readonly MessageType Type;
    public readonly Guid MessageId;
    public readonly string Data;

    public TerminalStreamChunk(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (Type, MessageId) = reader.ReadHeader();
        Data = reader.ReadString();
    }
}

public readonly ref struct TerminalStreamEnd
{
    public readonly Guid MessageId;
    public readonly int ExitCode;
    public readonly string WorkingDirectory;

    public TerminalStreamEnd(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ExitCode = reader.ReadInt32();
        WorkingDirectory = reader.ReadString();
    }
}

public static class TerminalProtocolSerializer
{
    public static byte[] SerializeStreamChunk(MessageType type, Guid messageId, string data)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);
        writer.WriteString(data);
        return writer.ToArray();
    }

    public static byte[] SerializeStreamEnd(Guid messageId, int exitCode, string workingDirectory)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.StreamEnd);
        writer.WriteGuid(messageId);
        writer.WriteInt32(exitCode);
        writer.WriteString(workingDirectory);
        return writer.ToArray();
    }

    public static byte[] SerializeCommandKillResult(Guid commandId, bool success)
    {
        using var writer = new PooledBufferWriter(18);
        writer.WriteByte((byte)MessageType.CommandKillResult);
        writer.WriteGuid(commandId);
        writer.WriteByte(success ? (byte)1 : (byte)0);
        return writer.ToArray();
    }

    public static byte[] SerializeCompleteResponse(
        Guid messageId,
        int exitCode,
        string output,
        string error,
        string workingDirectory)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CompleteResponse);
        writer.WriteInt32(exitCode);
        writer.WriteString(output);
        writer.WriteString(error);
        writer.WriteString(workingDirectory);
        writer.WriteGuid(messageId);
        return writer.ToArray();
    }

    public static byte[] SerializePasswordRequest(Guid messageId, string prompt)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.PasswordRequest);
        writer.WriteGuid(messageId);
        writer.WriteString(prompt);
        return writer.ToArray();
    }
}
