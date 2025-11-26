using EventStore.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Totem.InExternal;
using Totem.InExternal.Reports;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.InMemory.Events;
using Totem.Reports;
using Totem.Workflows;

namespace Totem.Hosting;

public static class EventStoreHostingExtensions
{
    public static ITotemBuilder AddEventStoreTopicStore(
        this ITotemBuilder builder,
        Action<EventStoreConfig> configure,
        Action<EventStoreClientSettings>? configureClient = null)
    {
        if(configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.Services.Configure(configure);

        builder.Services.TryAddSingleton<IInMemoryEventBus, InMemoryEventBus>();

        builder.Services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EventStoreConfig>>();
            var connectionString = options.Value.ConnectionString;

            if(string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("EventStore connection string cannot be null or empty.");
            }

            var settings = EventStoreClientSettings.Create(connectionString);
            configureClient?.Invoke(settings);

            return new EventStoreClient(settings);
        });

        builder.Services.TryAddSingleton<IAbstractEventStoreService, AbstractEventStoreService>();
        builder.Services.AddSingleton<ITopicStore, ExternalTopicStore>();

        builder.Services.TryAddSingleton<IInMemoryWorkflowCommandBus, InMemoryWorkflowCommandBus>();
        builder.Services.AddSingleton<IWorkflowStore, ExternalWorkflowStore>();

        builder.Services.AddSingleton<ExternalReportStore>();
        builder.Services.AddSingleton<IReportStore>(provider => provider.GetRequiredService<ExternalReportStore>());
        builder.Services.AddSingleton<IReportReader>(provider => provider.GetRequiredService<ExternalReportStore>());

        return builder;
    }
}
