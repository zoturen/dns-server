namespace Portare.Web.Models;

public class Zone
{
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RecordSet> RecordSets { get; set; } = [];
}