namespace Totem.Map.Summary;

internal sealed class RuntimeMapSummaryBuilder
{
    static readonly Id _typeIdNamespace = (Id) "98970055-1b9e-4537-a2be-b004aa6d2cb9";

    readonly Dictionary<Type, Id> _typeIdsByType = new();
    readonly List<SystemTypeSummary> _types = new();
    readonly RuntimeMap _map;

    internal RuntimeMapSummaryBuilder(RuntimeMap map) =>
        _map = map;

    internal RuntimeMapSummary Build() =>
        new(_types,
            BuildCommands().ToList(),
            BuildEvents().ToList(),
            BuildEventHandlers().ToList(),
            BuildNotifications().ToList(),
            BuildNotificationHandlers().ToList(),
            BuildReports().ToList(),
            BuildReportRows().ToList(),
            BuildReportQueries().ToList(),
            BuildReportListQueries().ToList(),
            BuildSubscriptions().ToList(),
            BuildSubscriptionHandlers().ToList(),
            BuildTopics().ToList(),
            BuildWorkflows().ToList(),
            BuildErrors(_map.Errors).ToList());

    IEnumerable<CommandSummary> BuildCommands() =>
        from command in _map.Commands
        select new CommandSummary(
            GetTypeId(command),
            GetTypeId(command.Topic),
            BuildCommandRoute(command.Route),
            BuildCommandWhen(command.When));

    TopicRouteMethodSummary? BuildCommandRoute(TopicRouteMethod? route) =>
        route is null ? null : new(BuildCommandParameter(route.Parameter));

    TopicWhenMethodSummary BuildCommandWhen(TopicWhenMethod when) =>
        new(BuildCommandParameter(when.Parameter), when.IsAsync, when.HasCancellationToken);

    CommandParameterSummary BuildCommandParameter(CommandParameter parameter) =>
        new(parameter.Info.Name ?? "",
            GetTypeId(parameter.Info.ParameterType),
            GetTypeId(parameter.Message.DeclaredType),
            parameter.HasContext);

    IEnumerable<EventSummary> BuildEvents() =>
        from e in _map.Events
        select new EventSummary(
            GetTypeId(e),
            e.Handler is null ? null : BuildEventHandler(e.Handler),
            e.ReportObservations.Select(x => GetTypeId(x.Observer)).ToList(),
            e.WorkflowObservations.Select(x => GetTypeId(x.Observer)).ToList());

    IEnumerable<EventHandlerSummary> BuildEventHandlers() =>
        _map.EventHandlers.Select(BuildEventHandler);

    EventHandlerSummary BuildEventHandler(EventHandlerType handler) =>
        new(GetTypeId(handler.DeclaredType),
            GetTypeId(handler.Event),
            GetTypeId(handler.ServiceType));

    IEnumerable<NotificationSummary> BuildNotifications() =>
        from notification in _map.Notifications
        select new NotificationSummary(GetTypeId(notification), GetTypeId(notification.Handler));

    IEnumerable<NotificationHandlerSummary> BuildNotificationHandlers() =>
        from handler in _map.NotificationHandlers
        select new NotificationHandlerSummary(
            GetTypeId(handler.DeclaredType),
            GetTypeId(handler.Notification),
            GetTypeId(handler.ServiceType));

    IEnumerable<ReportSummary> BuildReports() =>
        from report in _map.Reports
        let typeId = GetTypeId(report)
        select new ReportSummary(
            typeId,
            GetTypeId(report.Row),
            BuildObservations(report, typeId).ToList());

    IEnumerable<ReportRowSummary> BuildReportRows() =>
        from reportRow in _map.ReportRows
        let properties =
            from property in reportRow.Properties
            select new ReportRowPropertySummary(property.Name, GetTypeId(property.ValueType))
        select new ReportRowSummary(
            GetTypeId(reportRow),
            GetTypeId(reportRow.Report),
            reportRow.Info.ExternalType,
            reportRow.ReportQueries.Select(GetTypeId).ToList(),
            reportRow.ReportListQueries.Select(GetTypeId).ToList(),
            properties.ToList());

    IEnumerable<ReportQuerySummary> BuildReportQueries() =>
        from query in _map.ReportQueries
        select new ReportQuerySummary(GetTypeId(query), GetTypeId(query.Row), query.IdProperty?.Info.Name);

    IEnumerable<ReportListQuerySummary> BuildReportListQueries() =>
        from query in _map.ReportListQueries
        select new ReportListQuerySummary(GetTypeId(query), GetTypeId(query.Row));

    IEnumerable<SubscriptionSummary> BuildSubscriptions() =>
        from subscription in _map.Subscriptions
        select new SubscriptionSummary(GetTypeId(subscription), GetTypeId(subscription.Handler));

    IEnumerable<SubscriptionHandlerSummary> BuildSubscriptionHandlers() =>
        from handler in _map.SubscriptionHandlers
        select new SubscriptionHandlerSummary(
            GetTypeId(handler.DeclaredType),
            GetTypeId(handler.Subscription),
            GetTypeId(handler.ServiceType));

    IEnumerable<TopicSummary> BuildTopics() =>
        from topic in _map.Topics
        let typeId = GetTypeId(topic)
        select new TopicSummary(
            typeId,
            topic.Commands.Select(GetTypeId).ToList(),
            topic.Givens.Select(x => BuildTopicGiven(x)!).ToList());

    IEnumerable<WorkflowSummary> BuildWorkflows() =>
        from workflow in _map.Workflows
        let typeId = GetTypeId(workflow)
        select new WorkflowSummary(typeId, BuildObservations(workflow, typeId).ToList());

    IEnumerable<RuntimeMapBuildErrorSummary> BuildErrors(IReadOnlyCollection<RuntimeMapError> errors) =>
        from error in errors
        select new RuntimeMapBuildErrorSummary(
            error.Input.ToString() ?? error.InputType.ToString(),
            GetTypeId(error.InputType),
            error.Info.ToString(),
            BuildErrorDetails(error.Details),
            BuildErrors(error.InnerErrors).ToList());

    IEnumerable<ObservationSummary> BuildObservations(ObserverType observer, Id observerTypeId) =>
        from observation in observer.Observations
        select new ObservationSummary(
            observerTypeId,
            GetTypeId(observation.Event),
            BuildObserverRoute(observation.Route),
            BuildObserverGiven(observation.Given),
            BuildObserverWhen(observation.When));

    EventMethodSummary? BuildTopicGiven(TopicGivenMethod? given) =>
        given is null ? null : new(BuildEventParameter(given.Parameter));

    EventMethodSummary? BuildObserverGiven(ObserverGivenMethod? given) =>
        given is null ? null : new(BuildEventParameter(given.Parameter));

    ObserverMethodSummary? BuildObserverRoute(ObserverRouteMethod? route) =>
        route is null ? null : new(BuildEventParameter(route.Parameter));

    ObserverMethodSummary? BuildObserverWhen(ObserverWhenMethod? when) =>
        when is null ? null : new(BuildEventParameter(when.Parameter));

    EventParameterSummary BuildEventParameter(EventParameter parameter) =>
        new(parameter.Info.Name ?? "",
            GetTypeId(parameter.Info.ParameterType),
            GetTypeId(parameter.Message.DeclaredType),
            parameter.HasContext);

    static IReadOnlyDictionary<string, string> BuildErrorDetails(object? details)
    {
        var pairs = new Dictionary<string, string>();

        if(details is not null)
        {
            foreach(var property in details.GetType().GetProperties())
            {
                pairs[property.Name] = property.GetValue(details)?.ToString() ?? "";
            }
        }

        return pairs;
    }

    Id GetTypeId(Type type)
    {
        if(!_typeIdsByType.TryGetValue(type, out var id))
        {
            var fullName = type.FullName ?? "";

            id = _typeIdNamespace.DeriveId(fullName);

            _typeIdsByType[type] = id;
            _types.Add(new(id, type.Namespace ?? "", type.Name, type.FullName ?? "", fullName));
        }

        return id;
    }

    Id GetTypeId(RuntimeType type) =>
        GetTypeId(type.DeclaredType);
}
