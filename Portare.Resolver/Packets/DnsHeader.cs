using System.Buffers.Binary;

namespace Portare.Resolver.Packets;

public class DnsHeader
{
    public ushort Id { get; set; }
    public DnsHeaderFlags Flags { get; set; } = null!;
    public ushort QuestionCount { get; set; }
    public ushort AnswerCount { get; set; }
    public ushort AuthorityCount { get; set; }
    public ushort AdditionalCount { get; set; }

    public DnsHeader Parse(byte[] data)
    {
        Id = BitConverter.ToUInt16(data, 0);
        var flags = BitConverter.ToUInt16(data, sizeof(ushort) + 1);
        Flags = new DnsHeaderFlags().Parse(flags);
        QuestionCount = (ushort) (data[sizeof(ushort) * 2] << 8 | data[sizeof(ushort) * 2 + 1]); // shift to big endian
        AnswerCount = (ushort) (data[sizeof(ushort) * 3] << 8 | data[sizeof(ushort) * 3 + 1]); // shift to big endian
        AuthorityCount = (ushort) (data[sizeof(ushort) * 4] << 8 | data[sizeof(ushort) * 4 + 1]); // shift to big endian
        AdditionalCount = (ushort) (data[sizeof(ushort) * 5] << 8 | data[sizeof(ushort) * 5 + 1]); // shift to big endian
        return this;
    }
    
    public byte[] SerializeHeader()
    {
        var data = new byte[sizeof(ushort) * 6];
        var flags = SerializeHeaderFlags();
        var id = BitConverter.GetBytes(Id);
        id.CopyTo(data, 0);
        flags.CopyTo(data, sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 2), QuestionCount);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 3), AnswerCount);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 4), AuthorityCount);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(sizeof(ushort) * 5), AdditionalCount);
        return data;
    }

    private byte[] SerializeHeaderFlags()
    {
        var flags = new byte[sizeof(ushort)];
        var response = Flags.IsResponse ? 1 : 0;
        var opcode = Flags.Opcode << 11;
        var authoritative = Flags.IsAuthoritative ? 1 : 0;
        var truncated = Flags.IsTruncated ? 1 : 0;
        var recursionDesired = Flags.IsRecursionDesired ? 1 : 0;
        var recursionAvailable = Flags.IsRecursionAvailable ? 1 : 0;
        var z = Flags.IsZ ? 1 : 0;
        var authenticated = Flags.IsAuthenticated ? 1 : 0;
        var checkingDisabled = Flags.IsCheckingDisabled ? 1 : 0;
        var responseCode = Flags.ResponseCode;
        var flagsData = (ushort) (response << 15 | opcode << 11 | authoritative << 10 | truncated << 9 | recursionDesired << 8 | recursionAvailable << 7 | z << 6 | authenticated << 5 | checkingDisabled << 4 | responseCode);
        BinaryPrimitives.WriteUInt16BigEndian(flags, flagsData);
        return flags;
    }
}

public class DnsHeaderFlags
{
    public bool IsResponse { get; set; }
    public byte Opcode { get; set; }
    public bool IsAuthoritative { get; set; }
    public bool IsTruncated { get; set; }
    public bool IsRecursionDesired { get; set; }
    public bool IsRecursionAvailable { get; set; }
    public bool IsZ { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsCheckingDisabled { get; set; }
    public byte ResponseCode { get; set; }

    public DnsHeaderFlags Parse(ushort data)
    {
        IsResponse = (data & 0x8000) != 0;
        Opcode = (byte)((data & 0x7800) >> 11);
        IsAuthoritative = (data & 0x0400) != 0;
        IsTruncated = (data & 0x0200) != 0;
        IsRecursionDesired = (data & 0x0100) != 0;
        IsRecursionAvailable = (data & 0x0080) != 0;
        IsZ = (data & 0x0040) != 0;
        IsAuthenticated = (data & 0x0020) != 0;
        IsCheckingDisabled = (data & 0x0010) != 0;
        ResponseCode = (byte)(data & 0x000F);
        return this;
    }
}