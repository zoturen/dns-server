using System.Buffers.Binary;
using System.Text;

namespace Portare.Resolver.Packets;

public class DnsQuestion
{
    public string Name { get; set; } = null!;
    public byte[] OriginalName { get; set; } = null!;
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    
    public DnsQuestion Parse(byte[] data)
    {
        var nameLength = data.Length - (sizeof(ushort) * 2);
        var nameBytes = data[..nameLength];
        Name = ParseName(nameBytes);
        OriginalName = nameBytes;
        Type = (ushort) (data[nameLength] << 8 | data[nameLength + 1]); // shift to big endian
        Class = (ushort) (data[nameLength + sizeof(ushort)] << 8 | data[nameLength + sizeof(ushort) + 1]); // shift to big endian
        return this;
    }

    
    private static string ParseName(byte[] data)
    {
        var nameLength = 0;
        var name = new StringBuilder();
        for (var i = 0; i < data.Length; i++)
        {
            var length = data[i];
            if (length == 0)
            {
                break;
            }
            
            var nameBytes = data[(i + 1)..(i + 1 + length)];
            name.Append(Encoding.ASCII.GetString(nameBytes));
            name.Append('.');
            nameLength += length + 1;
            i += length;
        }

        return name.ToString();
    }

    public byte[] SerializeQuestion()
    {
        var data = new byte[sizeof(ushort) * 2 + OriginalName.Length];
        OriginalName.CopyTo(data, 0);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(OriginalName.Length), Type);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(OriginalName.Length + sizeof(ushort)), Class);
        return data;
    }
}