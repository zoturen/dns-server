using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Portare.Data.DAL;
using Portare.Data.Entities;

namespace Portare.Data.Repositories;

public class RecordSetRepository(DataContext context, ILogger<RecordSetRepository> logger) : Repository<RecordSetEntity>(context, logger)
{
    public override Task<RecordSetEntity?> GetAsync(Expression<Func<RecordSetEntity, bool>> predicate)
    {
        return context.RecordSets
            .Include(x => x.Records)
            .FirstOrDefaultAsync(predicate);
    }
}