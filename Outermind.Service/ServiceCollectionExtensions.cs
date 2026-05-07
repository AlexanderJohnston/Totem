using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outermind.Microfilm;
using Quantum.Service;
using Quantum.ServiceContracts;
using Totem.Timeline.Client;
using Totem.Timeline.EventStore.Client;

namespace Outermind.Service
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddOutermind(this IServiceCollection services)
    {
      //services.AddSingleton<SmartScanBackgroundService>(sp =>
      //{
      //  var service = new SmartScanBackgroundService();
      //  service.Watch(@"\\sbsr-film\Film\");
      //  service.Watch(@"Y:\");
      //  //service.UseReplay(@"C:\ESDB\replay\scans.log");
      //  return service;
      //});
      //services.AddSingleton<ISmartScanBackgroundService>(sp => sp.GetRequiredService<SmartScanBackgroundService>());
      //services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SmartScanBackgroundService>());
      AddOutermindParsers(services);
      AddWaspAssetService(services);
      AddMicrofilmTableSeedService(services);
      return services;
    }

    public static IServiceCollection AddOutermindParsers(this IServiceCollection services)
    {
      services.AddSingleton<IUsersLogParser, UsersLogParser>();
      return services;
    }

    public static IServiceCollection AddWaspAssetService(this IServiceCollection services)
    {
      services.AddMemoryCache();
      services.AddHttpClient<IWaspAssetService, WaspAssetService>((sp, client) =>
      {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["Wasp:BaseUrl"] ?? "https://thecrowleycompany.waspassetcloud.com";
        var token = config["Wasp:Token"] ?? ReadTokenFile();

        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(token))
        {
          client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
      });
      return services;
    }

    public static IServiceCollection AddMicrofilmTableSeedService(this IServiceCollection services)
    {
      services.AddSingleton<IClientDb, ClientDb>();
      services.AddSingleton<IHostedService, MicrofilmTableSeedService>();
      return services;
    }

    static string ReadTokenFile()
    {
      var path = Path.Combine(AppContext.BaseDirectory, "token.txt");
      return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }
  }
}
