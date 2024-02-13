namespace Portare.Web.Models;

public class RecordSet
{
    public string Name { get; set; } = null!;
    public RecordType Type { get; set; }
    public RecordClass Class { get; set; }
    public uint Ttl { get; set; }
    public ushort DataLength  => (ushort) Records.Sum(x => x.Content.Length);
    public List<Record> Records { get; set; } = null!;
}