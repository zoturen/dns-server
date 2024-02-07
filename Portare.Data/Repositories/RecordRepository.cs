using Portare.Data.DAL;
using Portare.Data.Entities;

namespace Portare.Data.Repositories;

public class RecordRepository(DataContext context, ILogger<RecordRepository> logger) : Repository<RecordEntity>(context, logger)
{
    
}