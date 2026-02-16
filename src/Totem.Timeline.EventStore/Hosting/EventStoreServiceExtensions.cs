using System;
using System.ComponentModel;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Totem.Runtime.Hosting;
using Totem.Runtime.Json;
using Totem.Timeline.Area;
using Totem.Timeline.Hosting;
using Totem.Timeline.Runtime;

namespace Totem.Timeline.EventStore.Hosting
{
  /// <summary>
  /// Extends <see cref="ITimelineBuilder"/> to declare the EventStore timeline database
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static class EventStoreServiceExtensions
  {
    public static IEventStoreTimelineBuilder AddEventStore(this ITimelineBuilder timeline)
    {
      timeline.ConfigureServices(services =>
      {
        services.AddSingleton<ITimelineDb>(p => new TimelineDb(
          p.GetRequiredService<EventStoreContext>(),
          p.GetRequiredService<IResumeProjection>()));

        services.AddSingleton(p => new EventStoreContext(
          p.BuildClient(),
          p.GetRequiredService<IJsonFormat>(),
          p.GetRequiredService<AreaMap>()));

        services.AddSingleton<IResumeProjection>(p => new ResumeProjection(
          p.GetRequiredService<AreaMap>(),
          p.BuildProjectionClient()));

        services.AddSingleton(p => p.BuildProjectionClient());
      });

      return new EventStoreTimelineBuilder(timeline);
    }

    public static IEventStoreTimelineBuilder BindOptionsToConfiguration(this IEventStoreTimelineBuilder timeline, string key = "totem.timeline.eventStore") =>
      timeline.ConfigureServices(services => services.BindOptionsToConfiguration<EventStoreTimelineOptions>(key));

    class EventStoreTimelineBuilder : IEventStoreTimelineBuilder
    {
      readonly ITimelineBuilder _timeline;

      internal EventStoreTimelineBuilder(ITimelineBuilder timeline)
      {
        _timeline = timeline;
      }

      public IEventStoreTimelineBuilder ConfigureServices(Action<IServiceCollection> configure)
      {
        _timeline.ConfigureServices(configure);

        return this;
      }
    }

    public static EventStoreClient BuildClient(this IServiceProvider provider)
    {
      var options = provider.GetOptions<EventStoreTimelineOptions>();
      var settings = BuildClientSettings(options, provider);
      return new EventStoreClient(settings);
    }

    internal static EventStoreProjectionManagementClient BuildProjectionClient(this IServiceProvider provider)
    {
      var options = provider.GetOptions<EventStoreTimelineOptions>();
      var settings = BuildClientSettings(options, provider);
      return new EventStoreProjectionManagementClient(settings);
    }

    static EventStoreClientSettings BuildClientSettings(EventStoreTimelineOptions options, IServiceProvider provider)
    {
      if(!string.IsNullOrEmpty(options.ConnectionString))
      {
        var settings = EventStoreClientSettings.Create(options.ConnectionString);
        settings.LoggerFactory = provider.GetService<ILoggerFactory>();
        return settings;
      }

      var tls = options.Server.Insecure ? "tls=false" : "";
      var connStr = string.IsNullOrEmpty(options.Connection.Username)
        ? $"esdb://{options.Server.Name}:{options.Server.Port}?{tls}"
        : $"esdb://{options.Connection.Username}:{options.Connection.Password}@{options.Server.Name}:{options.Server.Port}?{tls}";

      var s = EventStoreClientSettings.Create(connStr);
      s.LoggerFactory = provider.GetService<ILoggerFactory>();
      s.DefaultDeadline = options.Connection.Timeout;
      return s;
    }
  }
}