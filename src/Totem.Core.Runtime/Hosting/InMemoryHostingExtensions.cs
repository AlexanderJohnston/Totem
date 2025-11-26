using Totem.InMemory;
using Totem.InMemory.Events;
using Totem.InMemory.Reports;

namespace Totem.Hosting;

public static class InMemoryHostingExtensions
{
    public static ITotemBuilder AddInMemoryTopicStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<ITopicStore, InMemoryTopicStore>()
        .AddSingleton<IInMemoryEventBus, InMemoryEventBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryReportStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<InMemoryReportStore>()
        .AddSingleton<IReportStore>(p => p.GetRequiredService<InMemoryReportStore>())
        .AddSingleton<IReportReader>(p => p.GetRequiredService<InMemoryReportStore>());

        return builder;
    }

    public static ITotemBuilder AddInMemoryWorkflowStore(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<IWorkflowStore, InMemoryWorkflowStore>()
        .AddSingleton<IInMemoryWorkflowCommandBus, InMemoryWorkflowCommandBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryReportBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IReportBus, InMemoryReportBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryWorkflowBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IWorkflowBus, InMemoryWorkflowBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryHandlerBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IHandlerBus, InMemoryHandlerBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryNotificationBus(this ITotemBuilder builder)
    {
        builder.Services.AddSingleton<IInMemoryNotificationBus, InMemoryNotificationBus>();

        return builder;
    }

    public static ITotemBuilder AddInMemoryReportBroker(this ITotemBuilder builder)
    {
        builder.Services
        .AddSingleton<InMemoryReportBroker>()
        .AddSingleton<IReportBroker>(p => p.GetRequiredService<InMemoryReportBroker>())
        .AddSingleton<IInMemoryReportBroker>(p => p.GetRequiredService<InMemoryReportBroker>());

        return builder;
    }
}
