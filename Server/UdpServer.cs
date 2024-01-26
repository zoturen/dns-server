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
    NS = 2,
    CNAME = 5,
    SOA = 6,
    PTR = 12,
    MX = 15,
    TXT = 16,
    AAAA = 28,
    SRV = 33,
    ANY = 255
}

public enum RecordClass
{
    IN = 1,
    ANY = 255
}

public class RecordSet
{
    public string Name { get; set; } = null!;
    public RecordType Type { get; set; }
    public RecordClass Class { get; set; }
    public uint Ttl { get; set; }
    public ushort DataLength { get; set; }
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
                    Name = "www.test.com.",
                    Type = RecordType.A,
                    Class = RecordClass.IN,
                    Ttl = 300,
                    DataLength = 4,
                    Records =
                    [
                        new Record
                        {
                            Content = "127.0.0.1"
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
                
                var zone = GetZoneFromFqn(dnsRequestPacket.Question.Name);
                if (zone != null!)
                {
                    var recordSet = zone.RecordSets.FirstOrDefault(r => r.Name == dnsRequestPacket.Question.Name);
                    if (recordSet != null)
                    {
                        var dnsResponsePacket = new DnsPacket().CreateResponsePacket(dnsRequestPacket, recordSet);
                        var dnsResponsePacketBytes = dnsResponsePacket.Serialize();
                        _udpListener.Send(dnsResponsePacketBytes, dnsResponsePacketBytes.Length, _endPoint);
                    }

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