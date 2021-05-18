using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using Totem.Events;
using Totem;

namespace Decrypto.Hosting
{
    internal static class SerilogHostExtensions
    {
        internal static IHostBuilder ConfigureSerilog(this IHostBuilder builder) =>
          builder.UseSerilog((context, logger) =>
          {
              logger
              .ReadFrom.Configuration(context.Configuration)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Destructure.ByTransforming<Id>(id => id.ToShortString())
              .Destructure.ByTransforming<Type>(type => UseUnqualifiedName(type) ? type.Name : type.ToString())
              .UseEnvironment(context);

              if (Environment.UserInteractive)
              {
                  logger.WriteTo.Console().WriteTo.Debug();
              }
          });

        static bool UseUnqualifiedName(Type type) =>
            typeof(IMessage).IsAssignableFrom(type) || typeof(IEventSourced).IsAssignableFrom(type);

        static void UseEnvironment(this LoggerConfiguration logger, HostBuilderContext context)
        {
            if (!context.HostingEnvironment.IsDevelopment())
            {
                logger.MinimumLevel.Warning();
                return;
            }

            var path = context.Configuration["DREAM_LOG_PATH"] ?? @"S:\Code\totem\Logs";
            var file = $"{context.HostingEnvironment.ApplicationName}-{context.HostingEnvironment.EnvironmentName}.log";

            logger.WriteTo.File(Path.Combine(path, file));
        }
    }
}