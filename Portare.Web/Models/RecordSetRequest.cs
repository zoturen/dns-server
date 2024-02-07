using System.Text.Json.Serialization;
using Portare.Core.Models;

namespace Portare.Web.Models;

public class RecordSetRequest
{
    public string Name { get; set; } = null!;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RecordType Type { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RecordClass Class { get; set; }
    public uint Ttl { get; set; }
    public List<RecordRequest> Records { get; set; } = null!;
}