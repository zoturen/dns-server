using System.ComponentModel.DataAnnotations;

namespace Portare.Data.Entities;

public class ZoneEntity
{
    [Key]
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RecordSetEntity> RecordSets { get; set; } = [];
}