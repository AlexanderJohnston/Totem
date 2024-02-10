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
            var quotedGuid = reader.GetString();
            var substringGuid = quotedGuid.Substring(1, quotedGuid.Length - 2);
            var text = Guid.TryParse(substringGuid, out Guid attempt);
            return Id.From(attempt);
        }

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        {
            //writer.WriteStringValue(JsonSerializer.Serialize(value.GetType()));
            
            writer.WriteStringValue(value.ToString());
        }
    }
}
