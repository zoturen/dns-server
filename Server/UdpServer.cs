using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Packets;

namespace Server;

public class Zone
{
    public string Name { get; set; } = null!;
    public List<RecordSet> RecordSets { get; set; } = [];
}


public enum RecordType
{
    A = 0x0001,
    NS = 0x0002,
    CNAME = 0x0005,
    SOA = 0x0006,
    PTR = 0x000c,
    MX = 0x000f,
    TXT = 0x0010,
    AAAA = 0x001c,
    SRV = 0x0021,
    ANY = 0x00ff
}

public enum RecordClass
{
    IN = 0x0001,
    ANY = 0x00ff
}

public class RecordSet
{
    public string Name { get; set; } = null!;
    public RecordType Type { get; set; }
    public RecordClass Class { get; set; }
    public uint Ttl { get; set; }
    public ushort DataLength  => (ushort) Records.Sum(x => x.Content.Length);
    public List<Record> Records { get; set; } = null!;
}

public class Record
{
    public string Content { get; set; } = null!;
}

public class UdpServer : IDisposable
{
    private readonly UdpClient _udpListener;
    private IPEndPoint _endPoint;
    private bool _isRunning = true;
    private List<Zone> _zones = [];
    
    public UdpServer(string ip, int port)
    {
        _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _udpListener = new UdpClient(_endPoint);
        _zones.Add(new Zone
        {
            Name = "test.com.",
            RecordSets = new List<RecordSet>
            {
                new ()
                {
                    Name = "test.com.",
                    Type = RecordType.A,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "127.0.0.1"
                        }
                    ]
                },
                new ()
                {
                    Name = "test.com.",
                    Type = RecordType.A,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "127.0.0.12"
                        }
                    ]
                },
                new ()
                {
                    Name = "www1.test.com.",
                    Type = RecordType.CNAME,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "test.com"
                        }
                    ]
                },
                new ()
                {
                    Name = "test.com.",
                    Type = RecordType.MX,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "10 mail.test.com"
                        }
                    ]
                },
                new ()
                {
                    Name = "_acme-challange.test.com.",
                    Type = RecordType.TXT,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "admklm23m243lkmfkmesf_234nfkjmawmkq323__adwk"
                        }
                    ]
                },
                new ()
                {
                    Name = "test.com.",
                    Type = RecordType.AAAA,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    Records =
                    [
                        new Record
                        {
                            Content = "db37:3d06:ceba:e990:13af:65c4:d1a9:456c"
                        }
                    ]
                }
            }
        });
                
    }
    
    public Zone GetZone(string name)
    {
        return _zones.FirstOrDefault(z => z.Name == name) ?? null!;
    }

    public Zone GetZoneFromFqn(string fqn)
    {
        var parts = fqn.Split('.');
        var zoneName = new StringBuilder();
        Zone zone = null!;
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (zoneName.Length > 0)
            {
                zoneName.Insert(0, '.');
            }

            zoneName.Insert(0, parts[i]);
            zone = GetZone(zoneName.ToString() + '.');

            if (zone != null!)
            {
                break;
            }
        }
        if (fqn.EndsWith('.'))
        {
            zoneName.Append('.');
        }

        return zone ?? null!;
    }
 
    public void Start()
    {
        Console.WriteLine("Starting DNS Server");
        var apiThread = new Thread(() =>
        {
            while (_isRunning)
            {
                var data = _udpListener.Receive(ref _endPoint);
                var dnsRequestPacket = new DnsPacket().ParseRequestPacket(data);

                Console.WriteLine($"DnsRequestId: {dnsRequestPacket.Header.Id}");
                Console.WriteLine($"DnsRequestType: {dnsRequestPacket.Question.Type}");
                
                var zone = GetZoneFromFqn(dnsRequestPacket.Question.Name);
                if (zone != null!)
                {
                   
                    var recordSets = zone.RecordSets.Where(r => r.Name == dnsRequestPacket.Question.Name && r.Type == (RecordType)dnsRequestPacket.Question.Type).ToList();
                    var responsePacket = new DnsPacket().CreateResponsePacket(dnsRequestPacket, recordSets);
                    
                    var dnsResponsePacketBytes = responsePacket.Serialize();
                    _udpListener.Send(dnsResponsePacketBytes, dnsResponsePacketBytes.Length, _endPoint);
                    Console.WriteLine($"DnsRequestBytes: {BitConverter.ToString(data)}");
                    Console.WriteLine($"DnsResponseBytes: {BitConverter.ToString(dnsResponsePacketBytes)}");
                    
                }
                
            }

            Console.WriteLine("DNS Server Stopped");
        });
        apiThread.Start();

    }

    public void Close()
    {
        _isRunning = false;
    }
    
    public void Dispose()
    {
        _udpListener.Dispose();
    }
}