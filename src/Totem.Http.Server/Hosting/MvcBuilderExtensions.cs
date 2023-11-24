using Totem.Mvc.Controllers;

namespace Totem.Hosting;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddTotemMvc(this IMvcBuilder builder) =>
        builder
        .AddJsonOptions(options => TotemJsonFormat.ConfigureDefaults(options.JsonSerializerOptions))
        .ConfigureApplicationPartManager(parts =>
        {
            var map = builder.Services.GetRuntimeMap();

            parts.FeatureProviders.Add(new HttpMessageControllerProvider(map));
        });
}
