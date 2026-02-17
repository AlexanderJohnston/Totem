using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Totem.Runtime.Hosting;

namespace Totem.Timeline.Mvc.Hosting
{
  /// <summary>
  /// Configures JSON serialization for MVC to use the Totem JSON format options
  /// </summary>
  public class WebRuntimeOptionsSetup : IPostConfigureOptions<JsonOptions>
  {
    readonly IOptions<JsonFormatOptions> _jsonFormatOptions;

    public WebRuntimeOptionsSetup(IOptions<JsonFormatOptions> jsonFormatOptions)
    {
      _jsonFormatOptions = jsonFormatOptions;
    }

    public void PostConfigure(string name, JsonOptions options)
    {
      var source = _jsonFormatOptions.Value.SerializerOptions;

      options.JsonSerializerOptions.WriteIndented = source.WriteIndented;
      options.JsonSerializerOptions.PropertyNamingPolicy = source.PropertyNamingPolicy;
      options.JsonSerializerOptions.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
      options.JsonSerializerOptions.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
      options.JsonSerializerOptions.TypeInfoResolver = source.TypeInfoResolver;

      options.JsonSerializerOptions.Converters.Clear();
      foreach(var converter in source.Converters)
      {
        options.JsonSerializerOptions.Converters.Add(converter);
      }
    }
  }
}
