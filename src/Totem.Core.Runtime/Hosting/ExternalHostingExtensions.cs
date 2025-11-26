using Totem.InExternal;
using Totem.InExternal.Reports;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.InMemory.Events;
using Totem.InMemory.Reports;

namespace Totem.Hosting;

public static class ExternalHostingExtensions
{
    public static ITotemBuilder AddExternalTopicStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<ITopicStore, ExternalTopicStore>()
        .AddSingleton<IInMemoryEventBus, InMemoryEventBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalReportStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<ExternalReportStore>()
        .AddSingleton<IReportStore>(p => p.GetRequiredService<ExternalReportStore>())
        .AddSingleton<IReportReader>(p => p.GetRequiredService<ExternalReportStore>());

        return builder;
    }

    public static ITotemBuilder AddExternalWorkflowStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<IWorkflowStore, ExternalWorkflowStore>()
        .AddSingleton<IInMemoryWorkflowCommandBus, InMemoryWorkflowCommandBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalReportBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IReportBus, InMemoryReportBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalWorkflowBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IWorkflowBus, InMemoryWorkflowBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalHandlerBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IHandlerBus, InMemoryHandlerBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalNotificationBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IInMemoryNotificationBus, InMemoryNotificationBus>();

        return builder;
    }

    public static ITotemBuilder AddExternalReportBroker(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<InMemoryReportBroker>()
        .AddSingleton<IReportBroker>(p => p.GetRequiredService<InMemoryReportBroker>())
        .AddSingleton<IInMemoryReportBroker>(p => p.GetRequiredService<InMemoryReportBroker>());

        return builder;
    }

    public static ITotemBuilder AddAbstractEventStore(this ITotemBuilder builder)
    {
        builder.Services.TryAddSingleton<IAbstractEventStoreService, AbstractEventStoreService>();
        return builder;
    }
}
