using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outermind;
using Totem;
using Totem.App.Web;
using Quantum.Web.Wasp;
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
          services.AddWaspApi(context.Configuration);
          services.AddCors(options =>
            options.AddPolicy(CorsPolicyName, policy =>
              ConfigureCors(policy, context.Configuration)));
        })
        .BeforeMvcApp(app =>
          app.UseCors(CorsPolicyName));

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
          .AllowAnyMethod();
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
