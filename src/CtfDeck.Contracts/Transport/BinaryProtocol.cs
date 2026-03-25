using System.Buffers;
using System.Text;

namespace CtfDeck.Contracts.Transport;

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
    public static byte[] SerializeSimpleResult(MessageType type, Guid messageId, bool success)
    {
        using var writer = new PooledBufferWriter(18);
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        return writer.ToArray();
    }

    public static byte[] SerializeError(MessageType errorType, Guid messageId, string error)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)errorType);
        writer.WriteGuid(messageId);
        writer.WriteString(error);
        return writer.ToArray();
    }

    public static byte[] SerializeResultWithId(MessageType type, Guid messageId, bool success, Guid id)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(id);
        return writer.ToArray();
    }

    public static byte[] SerializeList<T>(MessageType type, Guid messageId, IEnumerable<T> items, int totalCount, Action<PooledBufferWriter, T> writeItem)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);

        var list = items.ToList();
        writer.WriteInt32(list.Count);
        writer.WriteInt32(totalCount);

        foreach (var item in list)
        {
            writeItem(writer, item);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeList<T>(MessageType type, Guid messageId, IEnumerable<T> items, Action<PooledBufferWriter, T> writeItem)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);
        writer.WriteGuid(messageId);

        var list = items.ToList();
        writer.WriteInt32(list.Count);

        foreach (var item in list)
        {
            writeItem(writer, item);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeList<T>(MessageType type, IEnumerable<T> items, Action<PooledBufferWriter, T> writeItem)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)type);

        var list = items.ToList();
        writer.WriteInt32(list.Count);

        foreach (var item in list)
        {
            writeItem(writer, item);
        }

        return writer.ToArray();
    }
}
