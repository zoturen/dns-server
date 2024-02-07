using System.Buffers.Binary;
using System.Net;
using System.Text;
using Portare.Core.Models;

namespace Portare.Resolver.Packets;

public class DnsPacket
{
    public DnsHeader Header { get; set; } = null!;
    public DnsQuestion Question { get; set; } = null!;
    public List<DnsAnswer> Answers { get; set; } = null!;
    
    
    
    public DnsPacket ParseRequestPacket(byte[] data)
    {
        var headerBytes = data[..(sizeof(ushort) * 6)];
        Header = new DnsHeader().Parse(headerBytes);
        Question = new DnsQuestion().Parse(data[(sizeof(ushort) * 6)..]);
        return this;
    }

    public DnsPacket CreateResponsePacket(DnsPacket dnsRequestPacket, List<RecordSet> recordSets, ResponseCode responseCode) // this might not be the best way to do this and it
    {                                                                                             // and it might not need List<RecordSet> as a parameter just RecordSet
        Header = dnsRequestPacket.Header;
        Header.Flags.IsResponse = true;
        Header.Flags.ResponseCode = (byte)responseCode;
        Header.AnswerCount = (ushort)recordSets.Count; 
        Question = dnsRequestPacket.Question;
       
        var answers = new List<DnsAnswer>();

        foreach (var recordSet in recordSets)
        {
            var recordData =  recordSet.Type switch
            {
                RecordType.A => IPAddress.Parse(recordSet.Records[0].Content).GetAddressBytes(),
                RecordType.NS => Encoding.ASCII.GetBytes(recordSet.Records[0].Content),
                RecordType.CNAME => SerializeDnsLabel(recordSet.Records[0].Content),
                RecordType.SOA => Encoding.ASCII.GetBytes(recordSet.Records[0].Content),
                RecordType.PTR => Encoding.ASCII.GetBytes(recordSet.Records[0].Content),
                RecordType.MX => ((Func<byte[]>)(() => {
                    var content = recordSet.Records[0].Content.Split(' ');
                    return SerializeMxRecord(content[1], ushort.Parse(content[0]));
                }))(),
                RecordType.TXT => SerializeText(recordSet.Records[0].Content),
                RecordType.AAAA => IPAddress.Parse(recordSet.Records[0].Content).GetAddressBytes(),
                RecordType.SRV => Encoding.ASCII.GetBytes(recordSet.Records[0].Content),
                RecordType.ANY => Encoding.ASCII.GetBytes(recordSet.Records[0].Content),
                _ => Array.Empty<byte>()
            };
            Console.WriteLine($"RecordDataBytes: {BitConverter.ToString(recordData)}");

            var answer = new DnsAnswer().Create(Question.OriginalName, recordSet.Type, recordSet.Class, recordSet.Ttl, (ushort)recordData.Length, recordData);
            answers.Add(answer);
        }
        Answers = answers;
        return this;
    }
    
    public static byte[] SerializeMxRecord(string name, ushort preference)
    {
        var nameBytes = SerializeDnsLabel(name);
        var data = new byte[nameBytes.Length + sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0), preference);
        nameBytes.CopyTo(data, sizeof(ushort));
        return data;
    }
    
    public static byte[] SerializeDnsLabel(string label)
    {
        var parts = label.Split('.');
        var result = new List<byte>();
        foreach (var part in parts)
        {
            result.AddRange(SerializeText(part));
        }
        result.Add(0); // End of label
        return result.ToArray();
    }
    
    public static byte[] SerializeText(string text)
    {
        var bytes = new byte[sizeof(byte) + text.Length];
        bytes[0] = (byte)text.Length;
        Encoding.ASCII.GetBytes(text).CopyTo(bytes, sizeof(byte));
        return bytes;
    }
    
    public byte[] Serialize()
    {
        var headerBytes = Header.SerializeHeader();
        var questionBytes = Question.SerializeQuestion();
        var namePosition = Array.IndexOf(questionBytes, Question.OriginalName[0]) + headerBytes.Length;
        var nameOffset = (ushort)namePosition;
        
        var answerBytes = Answers.Select(x => x.SerializeAnswer(nameOffset)).SelectMany(x => x).ToArray();
        
        var data = new byte[headerBytes.Length + questionBytes.Length + answerBytes.Length];
        headerBytes.CopyTo(data, 0);
        questionBytes.CopyTo(data, headerBytes.Length);
        answerBytes.CopyTo(data, headerBytes.Length + questionBytes.Length);
        return data;
    }
  
}