using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Serialization
{
    public class SystemTypeConverter : JsonConverter<Type>
    {
        private readonly RuntimeMap _map;

        public SystemTypeConverter(RuntimeMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var qualifiedName = reader.GetString();
            if(_map.Any(run => run.DeclaredType == typeToConvert))
            {
                return Type.GetType(qualifiedName);
            }
            else
            {
                throw new TypeAccessException($"Type {qualifiedName} is not a recognized runtime type in the map for {typeToConvert.FullName} and cannot be safely read.");
            }
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            if (_map.Any(run => run.DeclaredType == value))
            {
                writer.WriteStringValue(value.FullName);
            }
            else
            {
                throw new TypeAccessException($"Type {value.FullName} is not a recognized runtime type in the map and cannot be safely written.");
            }
        }
    }
}
