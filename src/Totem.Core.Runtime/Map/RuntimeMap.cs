using Totem.Map.Contexts;
using Totem.Map.Summary;

namespace Totem.Map;

[DebuggerDisplay("Count = {Count}")]
public sealed class RuntimeMap : IReadOnlyCollection<RuntimeType>
{
    readonly CommandContextFactory _commandContextFactory;
    readonly EventContextFactory _eventContextFactory;
    readonly EventHandlerContextFactory _eventHandlerContextFactory;
    readonly NotificationContextFactory _notificationContextFactory;
    readonly ReportContextFactory _reportContextFactory;
    readonly ReportQueryContextFactory _reportQueryContextFactory;
    readonly ReportListQueryContextFactory _reportListQueryContextFactory;
    readonly SubscriptionContextFactory _subscriptionContextFactory;
    readonly TopicContextFactory _topicContextFactory;
    readonly WorkflowContextFactory _workflowContextFactory;

    internal RuntimeMap(
        RuntimeTypeCollection<CommandType> commands,
        RuntimeTypeCollection<EventType> events,
        RuntimeTypeCollection<EventHandlerType> eventHandlers,
        RuntimeTypeCollection<NotificationType> notifications,
        RuntimeTypeCollection<NotificationHandlerType> notificationHandlers,
        RuntimeTypeCollection<ReportType> reports,
        RuntimeTypeCollection<ReportRowType> reportRows,
        RuntimeTypeCollection<ReportQueryType> reportQueries,
        RuntimeTypeCollection<ReportListQueryType> reportListQueries,
        RuntimeTypeCollection<SubscriptionType> subscriptions,
        RuntimeTypeCollection<SubscriptionHandlerType> subscriptionHandlers,
        RuntimeTypeCollection<TopicType> topics,
        RuntimeTypeCollection<WorkflowType> workflows,
        IReadOnlyCollection<RuntimeMapError> errors)
    {
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

        Count = Commands.Count
            + Events.Count
            + EventHandlers.Count
            + Notifications.Count
            + NotificationHandlers.Count
            + Reports.Count
            + ReportRows.Count
            + ReportQueries.Count
            + ReportListQueries.Count
            + Subscriptions.Count
            + SubscriptionHandlers.Count
            + Topics.Count
            + Workflows.Count;

        Summary = new RuntimeMapSummaryBuilder(this).Build();

        _commandContextFactory = new(this);
        _eventContextFactory = new(this);
        _eventHandlerContextFactory = new(this);
        _notificationContextFactory = new(this);
        _reportContextFactory = new(this);
        _reportQueryContextFactory = new(this);
        _reportListQueryContextFactory = new(this);
        _subscriptionContextFactory = new(this);
        _topicContextFactory = new();
        _workflowContextFactory = new(this);
    }

    public RuntimeTypeCollection<CommandType> Commands { get; }
    public RuntimeTypeCollection<EventType> Events { get; }
    public RuntimeTypeCollection<EventHandlerType> EventHandlers { get; }
    public RuntimeTypeCollection<NotificationType> Notifications { get; }
    public RuntimeTypeCollection<NotificationHandlerType> NotificationHandlers { get; }
    public RuntimeTypeCollection<ReportType> Reports { get; }
    public RuntimeTypeCollection<ReportRowType> ReportRows { get; }
    public RuntimeTypeCollection<ReportQueryType> ReportQueries { get; }
    public RuntimeTypeCollection<ReportListQueryType> ReportListQueries { get; }
    public RuntimeTypeCollection<SubscriptionType> Subscriptions { get; }
    public RuntimeTypeCollection<SubscriptionHandlerType> SubscriptionHandlers { get; }
    public RuntimeTypeCollection<TopicType> Topics { get; }
    public RuntimeTypeCollection<WorkflowType> Workflows { get; }
    public IReadOnlyCollection<RuntimeMapError> Errors { get; }

    public int Count { get; }
    public RuntimeMapSummary Summary { get; }

    public IEnumerator<RuntimeType> GetEnumerator() =>
        Commands
        .AsEnumerable<RuntimeType>()
        .Concat(Events)
        .Concat(EventHandlers)
        .Concat(Notifications)
        .Concat(NotificationHandlers)
        .Concat(Reports)
        .Concat(ReportRows)
        .Concat(ReportQueries)
        .Concat(ReportListQueries)
        .Concat(Subscriptions)
        .Concat(SubscriptionHandlers)
        .Concat(Topics)
        .Concat(Workflows)
        .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    public ICommandContext<ICommand> CreateContext(CommandEnvelope envelope) =>
        _commandContextFactory.Create(envelope);

    public IEventContext<IEvent> CreateContext(EventEnvelope envelope) =>
        _eventContextFactory.Create(envelope);

    public INotificationContext<INotification> CreateContext(NotificationEnvelope envelope) =>
        _notificationContextFactory.Create(envelope);

    public IReportQueryContext<IReportQuery> CreateContext(ReportQueryEnvelope envelope) =>
        _reportQueryContextFactory.Create(envelope);

    public IReportListQueryContext<IReportListQuery> CreateContext(ReportListQueryEnvelope envelope) =>
        _reportListQueryContextFactory.Create(envelope);

    public ISubscriptionContext<ISubscription> CreateContext(SubscriptionEnvelope envelope) =>
        _subscriptionContextFactory.Create(envelope);

    public IEventHandlerContext<IEvent> CreateEventHandlerContext(EventEnvelope envelope) =>
        _eventHandlerContextFactory.Create(envelope);

    public IReportContext<IEvent> CreateReportContext(EventEnvelope envelope, ObserverRoute route) =>
        _reportContextFactory.Create(envelope, route);

    public ITopicContext<ICommand> CreateTopicContext(ICommandContext<ICommand> commandContext, TopicRoute route) =>
        _topicContextFactory.Create(commandContext, route);

    public IWorkflowContext<IEvent> CreateWorkflowContext(EventEnvelope envelope, ObserverRoute route) =>
        _workflowContextFactory.Create(envelope, route);
}
