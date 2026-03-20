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

    public byte ReadByte() => _data[_offset++];
    
    public (MessageType Type, Guid MessageId) ReadHeader()
    {
        var type = (MessageType)ReadByte();
        var messageId = ReadGuid();
        return (type, messageId);
    }

    public int ReadInt32()
    {
        var result = BitConverter.ToInt32(_data.Slice(_offset, 4));
        _offset += 4;
        return result;
    }

    public long ReadInt64()
    {
        var result = BitConverter.ToInt64(_data.Slice(_offset, 8));
        _offset += 8;
        return result;
    }

    public Guid ReadGuid()
    {
        var result = new Guid(_data.Slice(_offset, 16));
        _offset += 16;
        return result;
    }

    public string ReadString()
    {
        var length = ReadInt32();
        if (length <= 0) return string.Empty;
        var result = Encoding.UTF8.GetString(_data.Slice(_offset, length));
        _offset += length;
        return result;
    }

    public List<string> ReadStringList()
    {
        var count = ReadInt32();
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(ReadString());
        }
        return list;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var result = _data.Slice(_offset, length);
        _offset += length;
        return result;
    }

    public void Advance(int count) => _offset += count;
}
