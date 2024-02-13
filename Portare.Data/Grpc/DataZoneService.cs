using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDataZone;
using Portare.Data.Entities;
using Portare.Data.Repositories;

namespace Portare.Data.Grpc;

public class DataZoneService(ZoneRepository zoneRepository, RecordSetRepository recordSetRepository)
    : DataZone.DataZoneBase
{
    public override async Task<InsertZoneResponse> InsertZone(InsertZoneRequest request, ServerCallContext context)
    {
        var exists = await zoneRepository.ExistsAsync(z => z.Name == request.Name);
        if (exists)
        {
            return new InsertZoneResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Zone already exists",
                    Status = GrpcStatus.Error
                },
                Zone = null
            };
        }

        var zone = new ZoneEntity
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = default
        };

        var result = await zoneRepository.AddAsync(zone);
        if (result)
        {
            var response = new InsertZoneResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Zone created",
                    Status = GrpcStatus.Ok
                },
                Zone = new GrpcZone
                {
                    Name = zone.Name,
                    CreatedAt = Timestamp.FromDateTime(zone.CreatedAt)
                }
            };
            return response;
        }

        return new InsertZoneResponse
        {
            Status = new GrpcStatusResponse
            {
                Message = "Could not create zone",
                Status = GrpcStatus.Error
            },
            Zone = null
        };
    }

    public override async Task<InsertRecordSetResponse> InsertRecordSet(InsertRecordSetRequest request,
        ServerCallContext context)
    {
        var zone = await zoneRepository.GetAsync(z => z.Name == request.ZoneName);
        if (zone == null)
        {
            return new InsertRecordSetResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Zone does not exist",
                    Status = GrpcStatus.Error
                },
                RecordSet = null
            };
        }

        var recordSetExists = await recordSetRepository.ExistsAsync(r => r.Name == request.Name);
        if (recordSetExists)
        {
            return new InsertRecordSetResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "RecordSet already exists, update record set or insert another record.",
                    Status = GrpcStatus.Error
                },
                RecordSet = null
            };
        }

        var recordSet = new RecordSetEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.RecordType,
            Class =  request.RecordClass,
            Ttl = request.Ttl,
            ZoneName = request.ZoneName,
            CreatedAt = DateTime.UtcNow,
            Records = request.Content.Select(x => new RecordEntity
            {
                Content = x.Content,
            }).ToList(),
        };

        var result = await recordSetRepository.AddAsync(recordSet);
        if (result)
        {
            var response = new InsertRecordSetResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Record set created",
                    Status = GrpcStatus.Ok
                },
                RecordSet = new GrpcRecordSet
                {
                    Id = recordSet.Id.ToString(),
                    ZoneName = recordSet.ZoneName,
                    Name = recordSet.Name,
                    RecordType = (int) recordSet.Type,
                    RecordClass = (int) recordSet.Class,
                    Ttl = recordSet.Ttl,
                    Content =
                    {
                        recordSet.Records.Select(x => new GrpcRecord
                        {
                            Id = x.Id.ToString(),
                            RecordSetId = x.RecordSetId.ToString(),
                            Content = x.Content,
                            IsDisabled = x.IsDisabled
                        })
                    }
                }
            };
            return response;
        }

        return new InsertRecordSetResponse
        {
            Status = new GrpcStatusResponse
            {
                Message = "Could not create record set",
                Status = GrpcStatus.Error
            },
            RecordSet = null
        };
    }

    public override async Task<GrpcZoneResponse> GetZoneByName(GetZoneByNameRequest request, ServerCallContext context)
    {
        var zoneEntity = await zoneRepository.GetAsync(z => z.Name == request.Name);
        if (zoneEntity != null)
        {
            return new GrpcZoneResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Zone found",
                    Status = GrpcStatus.Ok
                },
                Zone = MapZoneEntityToGrpcZone(zoneEntity)
            };
        }

        return new GrpcZoneResponse
        {
            Status = new GrpcStatusResponse
            {
                Message = "Zone not found",
                Status = GrpcStatus.Error
            },
            Zone = null
        };
    }


    private static GrpcZone MapZoneEntityToGrpcZone(ZoneEntity zoneEntity)
    {
        return new GrpcZone
        {
            Name = zoneEntity.Name,
            RecordSets =
            {
                zoneEntity.RecordSets.Select(x => new GrpcRecordSet
                {
                    Id = x.Id.ToString(),
                    ZoneName = x.ZoneName,
                    Name = x.Name,
                    RecordType = (int) x.Type,
                    RecordClass = (int) x.Class,
                    Ttl = x.Ttl,
                    Content =
                    {
                        x.Records.Select(r => new GrpcRecord
                        {
                            Id = r.Id.ToString(),
                            RecordSetId = r.RecordSetId.ToString(),
                            Content = r.Content
                        })
                    }
                })
            }
        };
    }

    public override async Task<GrpcRecordSetResponse> GetRecordSetByName(GetRecordSetByNameRequest request,
        ServerCallContext context)
    {
        var recordSets = await recordSetRepository.GetAsync(x =>
            x.Name == request.RecordSetName && x.Type == request.RecordType);
        if (recordSets != null)
        {
            return new GrpcRecordSetResponse
            {
                Status = new GrpcStatusResponse
                {
                    Message = "Record sets found",
                    Status = GrpcStatus.Ok
                },
                RecordSet = new GrpcRecordSet
                {
                    Id = recordSets.Id.ToString(),
                    ZoneName = recordSets.ZoneName,
                    Name = recordSets.Name,
                    RecordType =  (int)recordSets.Type,
                    RecordClass = (int)recordSets.Class,
                    Ttl = recordSets.Ttl,
                    Content =
                    {
                        recordSets.Records.Select(x => new GrpcRecord
                        {
                            Id = x.Id.ToString(),
                            RecordSetId = x.RecordSetId.ToString(),
                            Content = x.Content,
                            IsDisabled = x.IsDisabled
                        })
                    }
                }
            };
        }
        
        return new GrpcRecordSetResponse
        {
            Status = new GrpcStatusResponse
            {
                Message = "Record set not found",
                Status = GrpcStatus.NotFound
            },
            RecordSet = null
        };
    }
}