using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Totem.IO;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// Formats JSON in a Totem runtime using System.Text.Json
  /// </summary>
  public static class JsonFormatExtensions
  {
    //
    // To
    //

    public static string ToJson(this IJsonFormat format, object value) =>
      JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), format.Options);

    public static string ToJson(this IJsonFormat format, object value, Type type) =>
      JsonSerializer.Serialize(value, type, format.Options);

    public static JsonNode ToJsonNode(this IJsonFormat format, object value) =>
      JsonNode.Parse(JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), format.Options));

    public static JsonNode ToJsonNode(this IJsonFormat format, object value, Type type) =>
      JsonNode.Parse(JsonSerializer.Serialize(value, type, format.Options));

    public static JsonNode ToJsonNode(this IJsonFormat format, string json) =>
      JsonNode.Parse(json);

    //
    // From
    //

    public static T FromJson<T>(this IJsonFormat format, string json) =>
      JsonSerializer.Deserialize<T>(json, format.Options);

    public static object FromJson(this IJsonFormat format, string json) =>
      JsonSerializer.Deserialize<object>(json, format.Options);

    public static object FromJson(this IJsonFormat format, string json, Type type) =>
      JsonSerializer.Deserialize(json, type, format.Options);

    public static void FromJson(this IJsonFormat format, string json, object target)
    {
      var type = target.GetType();
      var source = JsonSerializer.Deserialize(json, type, format.Options);
      CopyProperties(source, target, type);
    }

    //
    // To (binary)
    //

    public static Binary ToJsonUtf8(this IJsonFormat format, object value) =>
      Binary.FromUtf8(format.ToJson(value));

    public static Binary ToJsonUtf8(this IJsonFormat format, object value, Type type) =>
      Binary.FromUtf8(format.ToJson(value, type));

    public static JsonNode ToJsonNodeUtf8(this IJsonFormat format, Binary json) =>
      format.ToJsonNode(json.ToStringUtf8());

    public static JsonNode ToJsonNodeUtf8(this IJsonFormat format, byte[] json) =>
      format.ToJsonNodeUtf8(Binary.From(json));

    //
    // From (binary)
    //

    public static T FromJsonUtf8<T>(this IJsonFormat format, Binary json) =>
      format.FromJson<T>(json.ToStringUtf8());

    public static object FromJsonUtf8(this IJsonFormat format, Binary json) =>
      format.FromJson(json.ToStringUtf8());

    public static object FromJsonUtf8(this IJsonFormat format, Binary json, Type type) =>
      format.FromJson(json.ToStringUtf8(), type);

    public static void FromJsonUtf8(this IJsonFormat format, Binary json, object target) =>
      format.FromJson(json.ToStringUtf8(), target);

    public static T FromJsonUtf8<T>(this IJsonFormat format, byte[] json) =>
      format.FromJsonUtf8<T>(Binary.From(json));

    public static object FromJsonUtf8(this IJsonFormat format, byte[] json) =>
      format.FromJsonUtf8(Binary.From(json));

    public static object FromJsonUtf8(this IJsonFormat format, byte[] json, Type type) =>
      format.FromJsonUtf8(Binary.From(json), type);

    public static void FromJsonUtf8(this IJsonFormat format, byte[] json, object target) =>
      format.FromJsonUtf8(Binary.From(json), target);

    public static T FromJsonUtf8<T>(this IJsonFormat format, Stream json) =>
      format.FromJsonUtf8<T>(Binary.From(json));

    public static object FromJsonUtf8(this IJsonFormat format, Stream json) =>
      format.FromJsonUtf8(Binary.From(json));

    public static object FromJsonUtf8(this IJsonFormat format, Stream json, Type type) =>
      format.FromJsonUtf8(Binary.From(json), type);

    public static void FromJsonUtf8(this IJsonFormat format, Stream json, object target) =>
      format.FromJsonUtf8(Binary.From(json), target);

    //
    // Details
    //

    static void CopyProperties(object source, object target, Type type)
    {
      if(source == null) return;

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic;

      foreach(var field in type.GetFields(flags))
      {
        field.SetValue(target, field.GetValue(source));
      }

      foreach(var prop in type.GetProperties(flags))
      {
        if(prop.CanRead && prop.CanWrite)
        {
          prop.SetValue(target, prop.GetValue(source));
        }
      }
    }
  }
}