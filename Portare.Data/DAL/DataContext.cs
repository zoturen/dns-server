using Microsoft.EntityFrameworkCore;
using Portare.Data.Entities;

namespace Portare.Data.DAL;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ZoneEntity> Zones { get; set; }
    public DbSet<RecordSetEntity> RecordSets { get; set; }
    public DbSet<RecordEntity> Records { get; set; }
}