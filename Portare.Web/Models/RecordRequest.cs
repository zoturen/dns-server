namespace Portare.Web.Models;

public class RecordRequest
{
    public Guid? RecordSetId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsDisabled { get; set; }
}