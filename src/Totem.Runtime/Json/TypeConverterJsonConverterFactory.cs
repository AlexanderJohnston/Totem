using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// Bridges <see cref="System.ComponentModel.TypeConverter"/> to STJ, replicating
  /// Newtonsoft's built-in behavior of serializing [TypeConverter] types as JSON strings
  /// </summary>
  public class TypeConverterJsonConverterFactory : JsonConverterFactory
  {
    public override bool CanConvert(Type typeToConvert)
    {
      var converter = TypeDescriptor.GetConverter(typeToConvert);

      return converter.GetType() != typeof(TypeConverter)
        && converter.CanConvertFrom(typeof(string))
        && converter.CanConvertTo(typeof(string));
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
      var converterType = typeof(TypeConverterJsonConverter<>).MakeGenericType(typeToConvert);

      return (JsonConverter)Activator.CreateInstance(converterType);
    }

    class TypeConverterJsonConverter<T> : JsonConverter<T>
    {
      readonly TypeConverter _converter = TypeDescriptor.GetConverter(typeof(T));

      public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
        if(reader.TokenType == JsonTokenType.Null)
        {
          return default;
        }

        var text = reader.GetString();

        return (T)_converter.ConvertFromInvariantString(text);
      }

      public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
      {
        if(value is null)
        {
          writer.WriteNullValue();
          return;
        }

        writer.WriteStringValue(_converter.ConvertToInvariantString(value));
      }
    }
  }
}
