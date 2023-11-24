using Totem.Mvc.Conventions;
using Totem.Mvc.Models;

namespace Totem.Hosting;

public sealed class ConfigureTotemMvcOptions : IConfigureOptions<MvcOptions>
{
    readonly string _internalPrefix;
    readonly RuntimeMap _map;

    public ConfigureTotemMvcOptions(string internalPrefix, RuntimeMap map)
    {
        _internalPrefix = !string.IsNullOrWhiteSpace(internalPrefix) ? internalPrefix : throw new ArgumentOutOfRangeException(nameof(internalPrefix));
        _map = map;
    }

    public void Configure(MvcOptions options)
    {
        options.Conventions.Add(new HttpCommandConvention());
        options.Conventions.Add(new HttpReportQueryConvention());
        options.Conventions.Add(new HttpReportListQueryConvention());
        options.Conventions.Add(new InternalPrefixConvention(_internalPrefix));

        options.AllowEmptyInputInBodyModelBinding = true;

        var bodyProvider = options.ModelBinderProviders.OfType<BodyModelBinderProvider>().FirstOrDefault();

        if(bodyProvider is null)
            throw new ArgumentException($"Expected {typeof(IModelBinderProvider)} of type {typeof(BodyModelBinderProvider)}", nameof(options));

        options.ModelBinderProviders.Insert(0, new TotemModelBinderProvider(bodyProvider, _map));
    }
}
