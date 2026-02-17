using System.Text.Json;

namespace Totem.Runtime.Json
{
  /// <summary>
  /// Describes the format of JSON written and read in a Totem runtime
  /// </summary>
  public interface IJsonFormat
  {
    JsonSerializerOptions Options { get; }
  }
}