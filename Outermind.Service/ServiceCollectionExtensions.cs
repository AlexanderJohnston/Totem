using Microsoft.Extensions.DependencyInjection;
using Quantum.Service;
using Quantum.ServiceContracts;

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
      return services;
    }

    public static IServiceCollection AddOutermindParsers(this IServiceCollection services)
    {
      services.AddSingleton<IUsersLogParser, UsersLogParser>();
      return services;
    }
  }
}
