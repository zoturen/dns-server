using System.Net;
using System.Net.Sockets;
using GrpcDataZone;
using Portare.Core.Models;
using Portare.Resolver.Packets;

namespace Portare.Resolver;

public class UdpResolver : IHostedService, IDisposable
{
    private readonly DataZone.DataZoneClient _dataZoneClient;
    private readonly UdpClient _udpListener;
    private IPEndPoint _endPoint;
    


    public UdpResolver(IConfiguration configuration, DataZone.DataZoneClient dataZoneClient)
    {
        _dataZoneClient = dataZoneClient;
        var ip = configuration["dns:udpIp"] ?? throw new Exception("Invalid UDP IP");
        var port = int.Parse(configuration["dns:udpPort"] ?? throw new Exception("Invalid UDP Port"));
        _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _udpListener = new UdpClient(_endPoint);
    }


   


    public void Dispose()
    {
        _udpListener.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            Console.WriteLine("Starting Udp Resolver...");

            while (!cancellationToken.IsCancellationRequested)
            {
                var udpReceiveResult = await _udpListener.ReceiveAsync(cancellationToken);
                var data = udpReceiveResult.Buffer;
                var remoteEndPoint = udpReceiveResult.RemoteEndPoint;
                var dnsRequestPacket = new DnsPacket().ParseRequestPacket(data);

                switch ((RecordType) dnsRequestPacket.Question.Type)
                {
                    case RecordType.A:
                    case RecordType.AAAA:
                    case RecordType.MX:
                        await ResolveRecordAsync(dnsRequestPacket, remoteEndPoint, data);
                        break;

                    default:
                        await ResolveNotImplementedAsync(dnsRequestPacket, remoteEndPoint, data);
                        break;
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    private async Task ResolveNotImplementedAsync(DnsPacket dnsRequestPacket, IPEndPoint remoteEndPoint, byte[] data)
    {
        var notImplementedPacket = new DnsPacket().CreateResponsePacket(dnsRequestPacket, [], ResponseCode.NotImplemented);
        var notImplementedPacketBytes = notImplementedPacket.Serialize();
        await _udpListener.SendAsync(notImplementedPacketBytes, notImplementedPacketBytes.Length, remoteEndPoint);
    }
    
    private async Task ResolveRecordAsync(DnsPacket dnsRequestPacket, IPEndPoint remoteEndPoint, byte[] data)
    {
        var name = dnsRequestPacket.Question.Name;
        var recordSetResponse = await _dataZoneClient.GetRecordSetByNameAsync(new GetRecordSetByNameRequest
        {
            RecordSetName = name,
            RecordType = dnsRequestPacket.Question.Type,
        });
        
        if (recordSetResponse.Status.Status != GrpcStatus.Ok)
        {
            var noZonePacket = new DnsPacket().CreateResponsePacket(dnsRequestPacket, [], ResponseCode.NotZone).Serialize();
            await _udpListener.SendAsync(noZonePacket, noZonePacket.Length, remoteEndPoint);
            return;
        }

        var recordSet = new RecordSet
        {
            Name = recordSetResponse.RecordSet.Name,
            Type = (RecordType) recordSetResponse.RecordSet.RecordType,
            Class = (RecordClass) recordSetResponse.RecordSet.RecordClass,
            Ttl = recordSetResponse.RecordSet.Ttl,
            Records = recordSetResponse.RecordSet.Content.Select(x =>
            {
                if (x.IsDisabled)
                    return null!;
                return new Record
                {
                    Content = x.Content,
                };
            }).ToList()
        };

        var responsePacket = new DnsPacket().CreateResponsePacket(dnsRequestPacket, [recordSet], ResponseCode.NoError).Serialize();
        await _udpListener.SendAsync(responsePacket, responsePacket.Length, remoteEndPoint);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Udp Resolver Stopping...");
        _udpListener.Close();
        return Task.CompletedTask;
    }
}