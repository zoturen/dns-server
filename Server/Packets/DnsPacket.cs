using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace Server.Packets;

public class DnsPacket
{
    public DnsHeader Header { get; set; } = null!;
    public DnsQuestion Question { get; set; } = null!;
    public DnsAnswer Answer { get; set; } = null!;
    
    
    
    public DnsPacket ParseRequestPacket(byte[] data)
    {
        var headerBytes = data[..(sizeof(ushort) * 6)];
        Header = new DnsHeader().Parse(headerBytes);
        Question = new DnsQuestion().Parse(data[(sizeof(ushort) * 6)..]);
        return this;
    }

    public DnsPacket CreateResponsePacket(DnsPacket dnsRequestPacket, RecordSet recordSet)
    {
        var recordData = Encoding.ASCII.GetBytes(recordSet.Records[0].Content);
        Header = dnsRequestPacket.Header;
        Header.Flags.IsResponse = true;
        Header.AnswerCount = 1; // TODO: Support multiple records
        Question = dnsRequestPacket.Question;
        Answer = new DnsAnswer().Create(Question.OriginalName, recordSet.Type, recordSet.Class, recordSet.Ttl, recordSet.DataLength, IPAddress.Parse(recordSet.Records[0].Content).GetAddressBytes()); // Set the data length to 4
        return this;
    }
    
    public byte[] Serialize()
    {
        var headerBytes = Header.SerializeHeader();
        var questionBytes = Question.SerializeQuestion();
        var namePosition = Array.IndexOf(questionBytes, Question.OriginalName[0]) + headerBytes.Length;
        var nameOffset = (ushort)namePosition;

        var answerBytes = Answer.SerializeAnswer(nameOffset);
        var data = new byte[headerBytes.Length + questionBytes.Length + answerBytes.Length];
        headerBytes.CopyTo(data, 0);
        questionBytes.CopyTo(data, headerBytes.Length);
        answerBytes.CopyTo(data, headerBytes.Length + questionBytes.Length);
        return data;
    }
  
}

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

