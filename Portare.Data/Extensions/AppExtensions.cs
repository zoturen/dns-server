using Microsoft.EntityFrameworkCore;
using Portare.Data.DAL;
using Portare.Data.Grpc;
using Portare.Data.Repositories;

namespace Portare.Data.Extensions;

public static class AppExtensions
{
    public static void AddAppServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        
        services.AddDbContext<DataContext>(x =>
        {
            x.UseNpgsql(builder.Configuration["postgres:connectionString"]);
        });

        services.AddScoped<ZoneRepository>();
        services.AddScoped<RecordSetRepository>();
        services.AddScoped<RecordRepository>();

        services.AddGrpc();
        
    }
    
    public static void UseAppServices(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<DataContext>();

            dbContext.Database.Migrate();
        }
        
        app.MapGrpcService<DataZoneService>();
    }
}