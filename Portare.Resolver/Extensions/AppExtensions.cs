using GrpcDataZone;

namespace Portare.Resolver.Extensions;

public static class AppExtensions
{
    public static void AddAppServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddHostedService<UdpResolver>();
        services.AddGrpcClient<DataZone.DataZoneClient>(o =>
        {
            o.Address = new Uri(builder.Configuration["grpc:dataZone"] ?? throw new Exception("Catalog GRPC address is missing"));
        });
    }
    
    public static void UseAppServices(this WebApplication app)
    {
        
        
    }
}