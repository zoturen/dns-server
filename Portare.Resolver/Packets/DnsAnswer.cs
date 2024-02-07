using System.Buffers.Binary;
using Portare.Core.Models;

namespace Portare.Resolver.Packets;

public class DnsAnswer
{
    public byte[] Name { get; set; } = null!;
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    public uint Ttl { get; set; }
    public ushort DataLength { get; set; }
    public byte[] Data { get; set; } = null!;

    public DnsAnswer Create(byte[] name, RecordType recordType, RecordClass recordClass, uint ttl, ushort dataLength, byte[] data)
    {
        Name = name;
        Type = (ushort) recordType;
        Class = (ushort) recordClass;
        Ttl = ttl;
        DataLength = dataLength;
        Data = data;
        return this;
    }
   
    public byte[] SerializeAnswer( ushort nameOffset)
    {
        // Calculate the pointer to the domain name.
        // The first two bits are 11, and the remaining 14 bits are the offset.
        var pointer = (ushort)(0xc000 | nameOffset);

        var data = new byte[sizeof(ushort) * 4 + sizeof(uint) + Data.Length];
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0), pointer);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort)), Type);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 2), Class);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(sizeof(ushort) * 3), Ttl);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 3 + sizeof(uint)), DataLength);
        Data.CopyTo(data, sizeof(ushort) * 4 + sizeof(uint));

        return data;
    }
}