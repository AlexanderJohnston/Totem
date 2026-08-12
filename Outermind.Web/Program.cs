using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outermind;
using Scalar.AspNetCore;
using Totem;
using Totem.App.Web;
using Totem.Timeline;
using Quantum.Web.Identity;
using Quantum.Web.Wasp;
using Quantum.Web.IdentityTracking;
using Quantum.Web.ScanProcessing;
using static Totem.Timeline.FlowCall;

namespace Quantum.Web
{
  /// <summary>
  /// The web server of the Outermind area
  /// </summary>
  public static class Program
  {
    const string CorsPolicyName = "OutermindCors";

    static Task Main(string[] args) =>
      WebApp.Run<QuantumArea>(Configure(args));

    static ConfigureWebApp Configure(string[] args) =>
      new ConfigureWebApp()
        .BeforeHost(webBuilder =>
        {
          // Totem creates the default builder WITHOUT passing args, so we must apply URL binding here.
          var urls = GetUrls(args);

          if (!string.IsNullOrWhiteSpace(urls))
          {
            webBuilder.UseUrls(urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            return;
          }

          if (IsProduction())
          {
            webBuilder.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");
          }
        })
        .AfterServices((context, services) =>
        {
          services.AddOpenApi(options =>
          {
            options.AddSchemaTransformer((schema, context, _) =>
            {
              if(typeof(Event).IsAssignableFrom(context.JsonTypeInfo.Type))
              {
                schema.Properties?.Remove("when");
                schema.Properties?.Remove("fields");
              }

              return Task.CompletedTask;
            });
          });
          services.AddWaspApi(context.Configuration);
          services.AddInteractionAuth(context.Configuration, context.HostingEnvironment);
          services.Configure<InteractionIdentityOptions>(context.Configuration.GetSection("InteractionIdentity"));
          services.AddSingleton<IInteractionIdentityResolver, InteractionIdentityResolver>();
          services.Configure<ScanProcessingAccessOptions>(context.Configuration.GetSection("ScanProcessingAccess"));
          services.AddSingleton<IRegisteredScanProcessingActorResolver, RegisteredScanProcessingActorResolver>();
          services.AddSingleton<IDemoBootstrapSecretValidator, DemoBootstrapSecretValidator>();
          services.AddCors(options =>
            options.AddPolicy(CorsPolicyName, policy =>
              ConfigureCors(policy, context.Configuration)));
        })
        .BeforeMvcApp(app =>
        {
          app.UseCors(CorsPolicyName);
          app.UseAuthentication();
        })
        .AfterSignalRRoutes(routes =>
        {
          routes.MapOpenApi();
          routes.MapScalarApiReference();
        });

    static bool IsProduction() =>
      string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);

    static void ConfigureCors(CorsPolicyBuilder policy, IConfiguration configuration)
    {
      var allowedOrigins = GetAllowedOrigins(configuration);

      if(allowedOrigins.Length > 0)
      {
        policy
          .WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
        return;
      }

      policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    }

    static string[] GetAllowedOrigins(IConfiguration configuration)
    {
      var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

      if(configuredOrigins is { Length: > 0 })
      {
        return Array.FindAll(configuredOrigins, origin => !string.IsNullOrWhiteSpace(origin));
      }

      var configuredOriginList = configuration["Cors:AllowedOrigins"];

      return string.IsNullOrWhiteSpace(configuredOriginList)
        ? Array.Empty<string>()
        : configuredOriginList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    static string GetUrls(string[] args)
    {
      for (var i = 0; i < args.Length; i++)
      {
        var arg = args[i];

        if (string.Equals(arg, "--urls", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
          return args[i + 1];
        }

        const string Prefix = "--urls=";
        if (arg.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
          return arg.Substring(Prefix.Length).Trim('"');
        }
      }

      return Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
        ?? Environment.GetEnvironmentVariable("DOTNET_URLS");
    }
  }
}
