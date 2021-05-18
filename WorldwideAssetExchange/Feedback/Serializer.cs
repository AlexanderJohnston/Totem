using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WaxInterfaces;

namespace Feedback
{
    public class MinerDataConverter : JsonConverter<Data>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Data) == typeToConvert;
        }
        public override Data Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Data data = new Data();
            var type = reader.TokenType;
            if (type == JsonTokenType.None 
                || type == JsonTokenType.EndObject
                || type == JsonTokenType.String) { return data; }
            var startObject = reader.Read();
            // potentially end of object
            string minerKey;
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return data;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                minerKey = reader.GetInt32().ToString();
            }
            else
            {
                minerKey = reader.GetString();
            }
            if (minerKey == "miner")
            {
                reader.Read();
                data.miner = reader.GetString();
            }
            else
            {
                while (!reader.IsFinalBlock)
                {
                    reader.Skip();
                }
                if (reader.IsFinalBlock)
                {
                    var test = JsonTokenType.Null;
                    while (test != JsonTokenType.EndObject)
                    {
                        reader.Read();
                        test = reader.TokenType;
                    }
                    return data;
                }
            }
            reader.Read();
            var nonceKey = reader.GetString();
            if (nonceKey == "nonce")
            {
                reader.Read();
                data.nonce = reader.GetString();
            }
            while (!reader.IsFinalBlock)
            {
                reader.Skip();
            }
            reader.Read();
            return data;
        }

        public override void Write(Utf8JsonWriter writer, Data value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(JsonSerializer.Serialize(value));
        }
    }
}
