using System.Text.Json.Serialization;

namespace Quantum.Wasp.Models.Common;

public class SortDescriptor
{
    [JsonPropertyName("Field")]
    public string Field { get; set; }

    [JsonPropertyName("Dir")]
    public string Dir { get; set; }
}
