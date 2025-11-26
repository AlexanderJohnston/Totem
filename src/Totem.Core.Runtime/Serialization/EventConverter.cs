using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Serialization
{
    public class EventConverter : JsonConverter<IEvent>
    {
        public override IEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var type = JsonSerializer.Deserialize<Type>(ref reader, options);
            return (IEvent) JsonSerializer.Deserialize(ref reader, type, options);
        }

        public override void Write(Utf8JsonWriter writer, IEvent value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(JsonSerializer.Serialize(value.GetType()));
            writer.WriteStringValue(JsonSerializer.Serialize(value));
        }
    }
}
