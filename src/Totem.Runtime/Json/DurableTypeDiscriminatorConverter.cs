using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Totem.Reflection;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// Handles polymorphic type discrimination using the "$type" property with "durable:" prefix values,
  /// maintaining backward compatibility with Newtonsoft.Json-serialized data
  /// </summary>
  public class DurableTypeDiscriminatorConverter : JsonConverterFactory
  {
    readonly IDurableTypeSet _durableTypes;

    public DurableTypeDiscriminatorConverter(IDurableTypeSet durableTypes)
    {
      _durableTypes = durableTypes;
    }

    public override bool CanConvert(Type typeToConvert) =>
      _durableTypes.TryGetOrAdd(typeToConvert, out _);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
      (JsonConverter)Activator.CreateInstance(
        typeof(DurableTypeConverter<>).MakeGenericType(typeToConvert),
        _durableTypes);

    sealed class DurableTypeConverter<T> : JsonConverter<T>
    {
      const string TypePropertyName = "$type";
      const string DurableDiscriminatorPrefix = "durable:";

      readonly IDurableTypeSet _durableTypes;

      public DurableTypeConverter(IDurableTypeSet durableTypes)
      {
        _durableTypes = durableTypes;
      }

      public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
        if(reader.TokenType != JsonTokenType.StartObject)
        {
          throw new JsonException($"Expected StartObject, got {reader.TokenType}");
        }

        var readerClone = reader;
        Type resolvedType = typeToConvert;

        if(readerClone.Read() && readerClone.TokenType == JsonTokenType.PropertyName)
        {
          var propName = readerClone.GetString();
          if(propName == TypePropertyName && readerClone.Read() && readerClone.TokenType == JsonTokenType.String)
          {
            var typeDiscriminator = readerClone.GetString();
            resolvedType = ResolveType(typeDiscriminator) ?? typeToConvert;
          }
        }

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Remove(this);

        for(int i = newOptions.Converters.Count - 1; i >= 0; i--)
        {
          if(newOptions.Converters[i] is DurableTypeDiscriminatorConverter)
          {
            newOptions.Converters.RemoveAt(i);
          }
        }

        var result = JsonSerializer.Deserialize(ref reader, resolvedType, newOptions);
        return (T)result;
      }

      public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
      {
        var type = value?.GetType() ?? typeof(T);

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Remove(this);

        for(int i = newOptions.Converters.Count - 1; i >= 0; i--)
        {
          if(newOptions.Converters[i] is DurableTypeDiscriminatorConverter)
          {
            newOptions.Converters.RemoveAt(i);
          }
        }

        if(type != typeof(T) && _durableTypes.TryGetKey(type, out var key))
        {
          using var doc = JsonSerializer.SerializeToDocument(value, type, newOptions);
          writer.WriteStartObject();
          writer.WriteString(TypePropertyName, $"{DurableDiscriminatorPrefix}{key}");

          foreach(var prop in doc.RootElement.EnumerateObject())
          {
            if(prop.Name != TypePropertyName)
            {
              prop.WriteTo(writer);
            }
          }

          writer.WriteEndObject();
        }
        else
        {
          JsonSerializer.Serialize(writer, value, type, newOptions);
        }
      }

      Type ResolveType(string discriminator)
      {
        if(discriminator == null) return null;

        if(discriminator.StartsWith(DurableDiscriminatorPrefix))
        {
          var keyStr = discriminator.Substring(DurableDiscriminatorPrefix.Length);
          if(DurableTypeKey.TryFrom(keyStr, out var parsedKey) && _durableTypes.TryGetByKey(parsedKey, out var type))
          {
            return type;
          }
        }

        // Fallback: try as assembly-qualified name for backward compat
        var parts = discriminator.Split(',');
        if(parts.Length >= 2)
        {
          var typeName = parts[0].Trim();
          var assemblyName = parts[1].Trim();
          if(TypeResolver.TryResolve(typeName, assemblyName, out var resolved))
          {
            return resolved;
          }
        }

        return null;
      }
    }
  }
}
