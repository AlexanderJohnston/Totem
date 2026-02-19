using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Outermind;
using Totem;
using Totem.App.Web;
using static Totem.Timeline.FlowCall;

namespace Quantum.Web
{
  /// <summary>
  /// The web server of the Outermind area
  /// </summary>
  public static class Program
  {
    static Task Main(string[] args) =>
      WebApp.Run<QuantumArea>(Configure(args));

    static ConfigureWebApp Configure(string[] args) =>
      new ConfigureWebApp().BeforeHost(webBuilder =>
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
      });

    static bool IsProduction() =>
      string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);

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