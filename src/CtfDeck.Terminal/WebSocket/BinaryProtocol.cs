using System.Text;

namespace CtfDeck.Terminal.WebSocket;

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

public struct WebSocketResponse
{
    public int ExitCode;
    public int OutputLength;
    public byte[] OutputBytes;
    public int ErrorLength;
    public byte[] ErrorBytes;
    public Guid MessageId;

    public string Output => Encoding.UTF8.GetString(OutputBytes);
    public string Error => Encoding.UTF8.GetString(ErrorBytes);

    public static WebSocketResponse FromResult(int exitCode, string output, string error, Guid messageId)
    {
        var outputBytes = Encoding.UTF8.GetBytes(output);
        var errorBytes = Encoding.UTF8.GetBytes(error);

        return new WebSocketResponse
        {
            ExitCode = exitCode,
            OutputLength = outputBytes.Length,
            OutputBytes = outputBytes,
            ErrorLength = errorBytes.Length,
            ErrorBytes = errorBytes,
            MessageId = messageId
        };
    }

    public static WebSocketResponse MockResponse(Guid messageId)
    {
        return FromResult(0, "Mock response from WebSocket server", "", messageId);
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(ExitCode);
        writer.Write(OutputLength);
        writer.Write(OutputBytes);
        writer.Write(ErrorLength);
        writer.Write(ErrorBytes);
        writer.Write(MessageId.ToByteArray());

        return stream.ToArray();
    }

    public static WebSocketResponse Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var exitCode = reader.ReadInt32();
        var outputLength = reader.ReadInt32();
        var outputBytes = reader.ReadBytes(outputLength);
        var errorLength = reader.ReadInt32();
        var errorBytes = reader.ReadBytes(errorLength);
        var messageIdBytes = reader.ReadBytes(16);

        return new WebSocketResponse
        {
            ExitCode = exitCode,
            OutputLength = outputLength,
            OutputBytes = outputBytes,
            ErrorLength = errorLength,
            ErrorBytes = errorBytes,
            MessageId = new Guid(messageIdBytes)
        };
    }
}