using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portare.Data.Entities;

public class RecordSetEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Type { get; set; }
    public int Class { get; set; }
    public uint Ttl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    [ForeignKey(nameof(ZoneEntity))]
    public string ZoneName { get; set; }
    public List<RecordEntity> Records { get; set; } = null!;

    public ZoneEntity Zone { get; set; } = null!;
}