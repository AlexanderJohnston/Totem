using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Quantum.Wasp.Models.Common;

public class TopLevelFilterType
{
    [JsonPropertyName("$id")]
    public string Id { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; }

    [JsonPropertyName("operator")]
    public string Operator { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; }

    [JsonPropertyName("logic")]
    public string Logic { get; set; }

    [JsonPropertyName("filters")]
    public List<TopLevelFilterType> Filters { get; set; } = new();
}
