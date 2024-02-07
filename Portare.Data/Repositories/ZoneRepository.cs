using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Portare.Data.DAL;
using Portare.Data.Entities;

namespace Portare.Data.Repositories;

public class ZoneRepository(DataContext context, ILogger<ZoneRepository> logger)
    : Repository<ZoneEntity>(context, logger)
{
    public override Task<ZoneEntity?> GetAsync(Expression<Func<ZoneEntity, bool>> predicate)
    {
        return context.Zones
            .Include(x => x.RecordSets)
            .ThenInclude(x => x.Records)
            .FirstOrDefaultAsync(predicate);
    }
}