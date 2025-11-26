namespace Totem.Map.Summary;

public sealed class RuntimeMapSummary
{
    public RuntimeMapSummary(
        IReadOnlyList<SystemTypeSummary> types,
        IReadOnlyList<CommandSummary> commands,
        IReadOnlyList<EventSummary> events,
        IReadOnlyList<EventHandlerSummary> eventHandlers,
        IReadOnlyList<NotificationSummary> notifications,
        IReadOnlyList<NotificationHandlerSummary> notificationHandlers,
        IReadOnlyList<ReportSummary> reports,
        IReadOnlyList<ReportRowSummary> reportRows,
        IReadOnlyList<ReportQuerySummary> reportQueries,
        IReadOnlyList<ReportListQuerySummary> reportListQueries,
        IReadOnlyList<SubscriptionSummary> subscriptions,
        IReadOnlyList<SubscriptionHandlerSummary> subscriptionHandlers,
        IReadOnlyList<TopicSummary> topics,
        IReadOnlyList<WorkflowSummary> workflows,
        IReadOnlyList<RuntimeMapBuildErrorSummary> errors)
    {
        Types = types;
        Commands = commands;
        Events = events;
        EventHandlers = eventHandlers;
        Notifications = notifications;
        NotificationHandlers = notificationHandlers;
        Reports = reports;
        ReportRows = reportRows;
        ReportQueries = reportQueries;
        ReportListQueries = reportListQueries;
        Subscriptions = subscriptions;
        SubscriptionHandlers = subscriptionHandlers;
        Topics = topics;
        Workflows = workflows;
        Errors = errors;
    }

    public IReadOnlyList<SystemTypeSummary> Types { get; }
    public IReadOnlyList<CommandSummary> Commands { get; }
    public IReadOnlyList<EventSummary> Events { get; }
    public IReadOnlyList<EventHandlerSummary> EventHandlers { get; }
    public IReadOnlyList<NotificationSummary> Notifications { get; }
    public IReadOnlyList<NotificationHandlerSummary> NotificationHandlers { get; }
    public IReadOnlyList<ReportSummary> Reports { get; }
    public IReadOnlyList<ReportRowSummary> ReportRows { get; }
    public IReadOnlyList<ReportQuerySummary> ReportQueries { get; }
    public IReadOnlyList<ReportListQuerySummary> ReportListQueries { get; }
    public IReadOnlyList<SubscriptionSummary> Subscriptions { get; }
    public IReadOnlyList<SubscriptionHandlerSummary> SubscriptionHandlers { get; }
    public IReadOnlyList<TopicSummary> Topics { get; }
    public IReadOnlyList<WorkflowSummary> Workflows { get; }
    public IReadOnlyList<RuntimeMapBuildErrorSummary> Errors { get; }
}
