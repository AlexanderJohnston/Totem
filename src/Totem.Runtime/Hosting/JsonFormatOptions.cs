using System.Collections.Generic;
using System.Text.Json;
using Totem.Runtime.Json;

namespace Totem.Runtime.Hosting
{
  /// <summary>
  /// Configuration for the Totem JSON format
  /// </summary>
  public class JsonFormatOptions
  {
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions();
    public List<DurableType> DurableTypes { get; } = new List<DurableType>();
  }
}