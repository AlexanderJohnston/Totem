using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Totem.Runtime.Hosting
{
  /// <summary>
  /// Extends <see cref="IServiceCollection"/> to declare hosted services
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static class HostedServiceExtensions
  {
    public static IServiceCollection AddHostedServiceWith<TAdditionalService, THostedService>(this IServiceCollection services)
      where TAdditionalService : class
      where THostedService : class, IHostedService, TAdditionalService
    {
      return services
        .AddSingleton<THostedService>()
        .AddSingleton<IHostedService>(provider => provider.GetService<THostedService>())
        .AddSingleton<TAdditionalService>(provider => provider.GetService<THostedService>());
    }
  }
}