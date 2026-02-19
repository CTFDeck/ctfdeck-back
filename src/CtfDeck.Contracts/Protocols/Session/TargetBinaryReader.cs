using System.Text;
using CtfDeck.Contracts.Models.Sessions;

namespace CtfDeck.Contracts.Protocols.Session;

public static class TargetBinaryReader
{
    /// <summary>
    /// Reads target fields (address, port, name, description, type) from the given offset.
    /// The targetId is passed as a parameter because the layout differs per message type.
    /// </summary>
    public static SessionTargetDto ReadFields(ReadOnlySpan<byte> data, ref int offset, Guid targetId = default)
    {
        var addrLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        var address = Encoding.UTF8.GetString(data.Slice(offset, addrLen));
        offset += addrLen;

        var portValue = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        int? port = portValue == -1 ? null : portValue;

        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        var name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var descLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        var description = Encoding.UTF8.GetString(data.Slice(offset, descLen));
        offset += descLen;

        var type = (TargetType)BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        return new SessionTargetDto
        {
            Id = targetId,
            Address = address,
            Port = port,
            Name = name,
            Description = description,
            Type = type
        };
    }
}
