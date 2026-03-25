using System.Text;

namespace CtfDeck.Contracts.Transport;

public ref struct BinaryProtocolReader
{
    private ReadOnlySpan<byte> _data;
    private int _offset;

    public BinaryProtocolReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _offset = 0;
    }

    public int Offset => _offset;
    public int Length => _data.Length;
    public bool Remaining => _offset < _data.Length;

    public byte ReadByte()
    {
        if (_offset >= _data.Length) throw new IndexOutOfRangeException("ReadByte: End of data reached.");
        return _data[_offset++];
    }

    public (MessageType Type, Guid MessageId) ReadHeader()
    {
        var type = (MessageType)ReadByte();
        var messageId = ReadGuid();
        return (type, messageId);
    }

    public int ReadInt32()
    {
        if (_offset + 4 > _data.Length) throw new IndexOutOfRangeException("ReadInt32: Not enough data left.");
        var result = BitConverter.ToInt32(_data.Slice(_offset, 4));
        _offset += 4;
        return result;
    }

    public long ReadInt64()
    {
        if (_offset + 8 > _data.Length) throw new IndexOutOfRangeException("ReadInt64: Not enough data left.");
        var result = BitConverter.ToInt64(_data.Slice(_offset, 8));
        _offset += 8;
        return result;
    }

    public Guid ReadGuid()
    {
        if (_offset + 16 > _data.Length) throw new IndexOutOfRangeException("ReadGuid: Not enough data left.");
        var result = new Guid(_data.Slice(_offset, 16));
        _offset += 16;
        return result;
    }

    public string ReadString()
    {
        var length = ReadInt32();
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "String length cannot be negative.");
        if (length == 0) return string.Empty;
        if (_offset + length > _data.Length) throw new IndexOutOfRangeException("ReadString: Not enough data left.");
        var result = Encoding.UTF8.GetString(_data.Slice(_offset, length));
        _offset += length;
        return result;
    }

    public List<string> ReadStringList()
    {
        var count = ReadInt32();
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "List count cannot be negative.");
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(ReadString());
        }
        return list;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "ReadBytes: length cannot be negative.");
        if (_offset + length > _data.Length) throw new IndexOutOfRangeException("ReadBytes: Not enough data left.");
        var result = _data.Slice(_offset, length);
        _offset += length;
        return result;
    }

    public void Advance(int count) => _offset += count;
}
