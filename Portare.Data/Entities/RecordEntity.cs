using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portare.Data.Entities;

public class RecordEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [ForeignKey(nameof(RecordSetEntity))]
    public Guid RecordSetId { get; set; }
    
    public RecordSetEntity RecordSet { get; set; } = null!;
}