using System.Text.Json;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// The format of JSON written and read in a Totem runtime
  /// </summary>
  public sealed class JsonFormat : IJsonFormat
  {
    public JsonFormat(JsonSerializerOptions options)
    {
      Options = options;
    }

    public JsonSerializerOptions Options { get; }
  }
}