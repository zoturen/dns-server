
using GrpcDataZone;
using Portare.Core.Models;
using Portare.Web.Models;

namespace Portare.Web.Endpoints;

public static class ZoneEndpoints
{
    public static void MapZoneEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/zones/{name}", async (string name, DataZone.DataZoneClient dataZoneClient) =>
        {
            var zoneResponse = await dataZoneClient.GetZoneByNameAsync(new GetZoneByNameRequest
            {
                Name = name
            });
            if (zoneResponse.Status.Status != GrpcStatus.Ok)
            {
                return Results.NotFound(zoneResponse.Status.Message);
            }
            
            var zone = new Zone
            {
                Name = zoneResponse.Zone.Name,
                CreatedAt = zoneResponse.Zone.CreatedAt.ToDateTime(),
                UpdatedAt = zoneResponse.Zone.UpdatedAt.ToDateTime(),
                RecordSets = zoneResponse.Zone.RecordSets.Select(x => new RecordSet
                {
                    Name = x.Name,
                    Type = (RecordType) x.RecordType,
                    Class = (RecordClass) x.RecordClass,
                    Ttl = x.Ttl,
                    Records = x.Content.Select(r => new Record
                    {
                        Content = r.Content,
                        IsDisabled = r.IsDisabled
                    }).ToList()
                }).ToList()

            };
            
            return Results.Ok(zone);
        });
        
        endpoints.MapPost("/zones", async (ZoneRequest zoneRequest, DataZone.DataZoneClient dataZoneClient) =>
        {
            var response = await dataZoneClient.InsertZoneAsync(new InsertZoneRequest
            {
                Name = zoneRequest.Name
            });
            if (response.Status.Status != GrpcStatus.Ok)
            {
                return Results.BadRequest(response.Status.Message);
            }
            return Results.Created($"/zones/{zoneRequest.Name}", zoneRequest);
        });
        
        endpoints.MapPost("/zones/{name}/recordsets", async (string name, RecordSetRequest recordSetRequest, DataZone.DataZoneClient dataZoneClient) =>
        {
            var response = await dataZoneClient.InsertRecordSetAsync(new InsertRecordSetRequest
            {
                Name = recordSetRequest.Name,
                ZoneName = name,
                RecordType = (int) recordSetRequest.Type,
                RecordClass = (int) recordSetRequest.Class,
                Ttl = recordSetRequest.Ttl,
                Content = { recordSetRequest.Records.Select(x => new ContentRequest
                {
                    Content = x.Content,
                    IsDisabled = x.IsDisabled
                })}
            });
            if (response.Status.Status != GrpcStatus.Ok)
            {
                return Results.BadRequest(response.Status.Message);
            }
            var recordSet = new RecordSet
            {
                Name = response.RecordSet.Name,
                Type = (RecordType) response.RecordSet.RecordType,
                Class = (RecordClass) response.RecordSet.RecordClass,
                Ttl = response.RecordSet.Ttl,
                Records = response.RecordSet.Content.Select(x => new Record
                {
                    Content = x.Content
                }).ToList()
            };
            return Results.Created($"/zones/{name}/recordsets/{response.RecordSet.Id}", recordSet);
        });
    }
}