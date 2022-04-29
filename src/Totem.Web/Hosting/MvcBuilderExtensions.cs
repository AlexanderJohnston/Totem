using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Totem.Core;
using Totem.External;

namespace Totem.Hosting;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddTotemMvc(this IMvcBuilder builder)
    {
        if(builder is null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.ConfigureApplicationPartManager(applicationPartManager =>
        {
            var map = builder.Services.GetRuntimeMap();

            applicationPartManager.FeatureProviders.Add(new CommandControllerProvider(map));
            applicationPartManager.FeatureProviders.Add(new QueryControllerProvider(map));
            applicationPartManager.FeatureProviders.Add(new InternalControllerProvider());
        });

        return builder;
    }

    public static IMvcBuilder AddTotemJsonEvents(this IMvcBuilder builder)
    {
        if(builder is null)
            throw new ArgumentNullException(nameof(builder));

        if(builder.Services is null)
            throw new ArgumentNullException(nameof(builder.Services));

        var factories = new List<JsonConverterFactory>();

        foreach(var type in builder.Services.GetRuntimeMap().Events.TypeKeys)
        {
            factories.Add(new InterfaceConverterFactory(type, typeof(IEvent)));
        }

        //builder.AddJsonOptions(options =>
        //{
        //    foreach(var factory in factories)
        //    {
        //        options.JsonSerializerOptions.Converters.Add(factory);
        //    }
        //    options.JsonSerializerOptions.Converters.Add(new SystemTypeConverter());
        //});

        var options = new JsonSerializerOptions();
        foreach(var factory in factories)
        {
            options.Converters.Add(factory);
        }
        options.Converters.Add(new SystemTypeConverter());
        builder.Services.AddSingleton(options);

        builder.Services.Configure<JsonOptions>(options =>
        {
            foreach(var factory in factories)
            {
                options.JsonSerializerOptions.Converters.Add(factory);
            }
            options.JsonSerializerOptions.Converters.Add(new SystemTypeConverter());
            //options.JsonSerializerOptions.Converters.Add(new EventConverter());
        });

        return builder;
    }
}
