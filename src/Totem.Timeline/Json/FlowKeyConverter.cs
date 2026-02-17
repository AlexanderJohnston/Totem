using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Totem.Timeline.Area;

namespace Totem.Timeline.Json
{
  /// <summary>
  /// Converts instances of <see cref="FlowKey"/> to and from JSON
  /// </summary>
  public class FlowKeyConverter : JsonConverter<FlowKey>
  {
    readonly AreaMap _area;

    public FlowKeyConverter(AreaMap area)
    {
      _area = area;
    }

    public override FlowKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
      reader.TokenType == JsonTokenType.Null ? null : FlowKey.From(reader.GetString(), _area);

    public override void Write(Utf8JsonWriter writer, FlowKey value, JsonSerializerOptions options)
    {
      if(value == null)
        writer.WriteNullValue();
      else
        writer.WriteStringValue(value.ToString());
    }
  }
}