using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Totem.Runtime.Json;

namespace Totem.Runtime.Hosting
{
  /// <summary>
  /// Configures default values for instances of <see cref="JsonFormatOptions"/>
  /// </summary>
  public class JsonFormatOptionsSetup : IConfigureOptions<JsonFormatOptions>, IPostConfigureOptions<JsonFormatOptions>
  {
    public void Configure(JsonFormatOptions options)
    {
      var settings = options.SerializerOptions;

      settings.WriteIndented = true;
      settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
      settings.DictionaryKeyPolicy = null;

      settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
      settings.Converters.Add(new TypeConverterJsonConverterFactory());
    }

    public void PostConfigure(string name, JsonFormatOptions options)
    {
      var durableTypes = new DurableTypeSet(options.DurableTypes);

      options.SerializerOptions.TypeInfoResolver = new TotemJsonTypeInfoResolver(durableTypes);
      options.SerializerOptions.Converters.Add(new DurableTypeDiscriminatorConverter(durableTypes));
    }
  }
}