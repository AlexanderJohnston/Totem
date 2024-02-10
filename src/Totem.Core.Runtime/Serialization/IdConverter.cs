using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Serialization
{
    public class IdConverter : JsonConverter<Id>
    {
        public override Id? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //var type = JsonSerializer.Deserialize<Type>(ref reader, options);
            return JsonSerializer.Deserialize<Id>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        {
            //writer.WriteStringValue(JsonSerializer.Serialize(value.GetType()));
            writer.WriteStringValue(JsonSerializer.Serialize(value));
        }
    }
}
