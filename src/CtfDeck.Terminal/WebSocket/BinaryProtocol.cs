using System.Buffers;
using System.Text;

namespace CtfDeck.Terminal.WebSocket;

/// <summary>
/// High-performance binary protocol using ArrayPool to minimize allocations
/// </summary>
public enum MessageType : byte
{
    // Terminal messages
    CompleteResponse = 0,
    StreamOutput = 1,
    StreamError = 2,
    StreamEnd = 3,

    // Session requests (client → server)
    SessionCreate = 10,
    SessionSetActive = 11,
    SessionLoad = 12,
    SessionList = 13,
    SessionDelete = 14,
    SessionUpdateTargets = 15,

    // Session responses (server → client)
    SessionCreateResult = 20,
    SessionSetActiveResult = 21,
    SessionLoadResult = 22,
    SessionListResult = 23,
    SessionDeleteResult = 24,
    SessionOperationError = 29
}

/// <summary>
/// High-performance WebSocket command deserialization
/// </summary>
public readonly ref struct WebSocketCommandReader
{
    public readonly int CommandLength;
    public readonly ReadOnlySpan<byte> CommandBytes;
    public readonly Guid MessageId;

    public WebSocketCommandReader(ReadOnlySpan<byte> data)
    {
        CommandLength = BitConverter.ToInt32(data[..4]);
        CommandBytes = data.Slice(4, CommandLength);
        MessageId = new Guid(data.Slice(4 + CommandLength, 16));
    }

    public string GetCommand() => Encoding.UTF8.GetString(CommandBytes);
}

/// <summary>
/// Pooled buffer writer for high-performance serialization
/// </summary>
public sealed class PooledBufferWriter : IDisposable
{
    private byte[] _buffer;
    private int _position;
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

    public PooledBufferWriter(int initialCapacity = 4096)
    {
        _buffer = Pool.Rent(initialCapacity);
        _position = 0;
    }

    public int Length => _position;
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _position);
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _position);

    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    public void WriteInt32(int value)
    {
        EnsureCapacity(4);
        BitConverter.TryWriteBytes(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    public void WriteInt64(long value)
    {
        EnsureCapacity(8);
        BitConverter.TryWriteBytes(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    public void WriteGuid(Guid value)
    {
        EnsureCapacity(16);
        value.TryWriteBytes(_buffer.AsSpan(_position));
        _position += 16;
    }

    public void WriteString(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt32(byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
        _position += byteCount;
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        EnsureCapacity(bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_position));
        _position += bytes.Length;
    }

    private void EnsureCapacity(int additionalBytes)
    {
        var required = _position + additionalBytes;
        if (required <= _buffer.Length) return;

        var newSize = Math.Max(_buffer.Length * 2, required);
        var newBuffer = Pool.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        Pool.Return(_buffer);
        _buffer = newBuffer;
    }

    /// <summary>
    /// Gets the written data as a new array (for final send)
    /// </summary>
    public byte[] ToArray()
    {
        var result = new byte[_position];
        _buffer.AsSpan(0, _position).CopyTo(result);
        return result;
    }

    public void Reset() => _position = 0;

    public void Dispose()
    {
        Pool.Return(_buffer);
        _buffer = Array.Empty<byte>();
    }
}

/// <summary>
/// High-performance message serializers using pooled buffers
/// </summary>
public static class BinaryProtocolSerializer
{
    /// <summary>
    /// Serialize a streaming output chunk
    /// </summary>
    public static byte[] SerializeStreamChunk(MessageType type, Guid messageId, string data)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);
        writer.WriteString(data);
        return writer.ToArray();
    }

    /// <summary>
    /// Serialize a stream end message
    /// </summary>
    public static byte[] SerializeStreamEnd(Guid messageId, int exitCode, string workingDirectory)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.StreamEnd);
        writer.WriteGuid(messageId);
        writer.WriteInt32(exitCode);
        writer.WriteString(workingDirectory);
        return writer.ToArray();
    }

    /// <summary>
    /// Serialize a complete response (for cd and simple commands)
    /// </summary>
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
}

/// <summary>
/// Legacy structs for backward compatibility - delegates to optimized implementations
/// </summary>
public struct WebSocketCommand
{
    public int CommandLength;
    public byte[] CommandBytes;
    public Guid MessageId;

    public string Command => Encoding.UTF8.GetString(CommandBytes);



    public static WebSocketCommand Deserialize(byte[] data)
    {
        var reader = new WebSocketCommandReader(data);
        return new WebSocketCommand
        {
            CommandLength = reader.CommandLength,
            CommandBytes = reader.CommandBytes.ToArray(),
            MessageId = reader.MessageId
        };
    }

    public byte[] Serialize()
    {
        using var writer = new PooledBufferWriter();
        writer.WriteInt32(CommandLength);
        writer.WriteBytes(CommandBytes);
        writer.WriteGuid(MessageId);
        return writer.ToArray();
    }
}

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

    public byte[] Serialize() =>
        BinaryProtocolSerializer.SerializeCompleteResponse(MessageId, ExitCode, Output, Error, WorkingDirectory);

    public static WebSocketResponse Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

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

    public static WebSocketResponse MockResponse(Guid messageId) =>
        FromResult(0, "Mock response from WebSocket server", "", Environment.CurrentDirectory, messageId);
}

// Streaming message types (for high-perf serialization)
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

    public byte[] Serialize() => BinaryProtocolSerializer.SerializeStreamChunk(Type, MessageId, Data);

    public static StreamChunkMessage Deserialize(byte[] data)
    {
        var type = (MessageType)data[0];
        var messageId = new Guid(data.AsSpan(1, 16));
        var dataLength = BitConverter.ToInt32(data.AsSpan(17, 4));
        var dataBytes = data.AsSpan(21, dataLength).ToArray();

        return new StreamChunkMessage
        {
            Type = type,
            MessageId = messageId,
            DataLength = dataLength,
            DataBytes = dataBytes
        };
    }
}

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

    public byte[] Serialize() => BinaryProtocolSerializer.SerializeStreamEnd(MessageId, ExitCode, WorkingDirectory);

    public static StreamEndMessage Deserialize(byte[] data)
    {
        // Structure: [Type 1] + [Guid 16] + [ExitCode 4] + [WdLen 4] + [WdBytes...]
        var type = (MessageType)data[0];
        var messageId = new Guid(data.AsSpan(1, 16));
        var exitCode = BitConverter.ToInt32(data.AsSpan(17, 4));
        var wdLength = BitConverter.ToInt32(data.AsSpan(21, 4));
        var wdBytes = data.AsSpan(25, wdLength).ToArray();

        return new StreamEndMessage
        {
            Type = type,
            MessageId = messageId,
            ExitCode = exitCode,
            WorkingDirectoryLength = wdLength,
            WorkingDirectoryBytes = wdBytes
        };
    }
}