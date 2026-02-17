using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Timeline.Json
{
  /// <summary>
  /// Converts <see cref="TimelinePosition"/> to and from JSON as 64-bit integers
  /// </summary>
  /// <remarks>
  /// Javascript only supports literals up to 2^53 - 1 (9007199254740991), technically
  /// making this a lossy conversion. However, we will have more immediate issues if we
  /// ever see timeline positions at that scale.
  /// </remarks>
  public class TimelinePositionConverter : JsonConverter<TimelinePosition>
  {
    public override TimelinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if(reader.TokenType == JsonTokenType.Null)
        return TimelinePosition.None;

      return new TimelinePosition(reader.GetInt64());
    }

    public override void Write(Utf8JsonWriter writer, TimelinePosition value, JsonSerializerOptions options)
    {
      if(value.IsNone)
        writer.WriteNullValue();
      else
        writer.WriteNumberValue(value.ToInt64());
    }
  }
}