namespace Totem.Hosting;

public static class SerilogHostingExtensions
{
    public static LoggerConfiguration ConfigureTotemRuntime(this LoggerConfiguration logger) =>
        logger
        .Enrich.With<RuntimeEnricher>()
        .Destructure.AsScalar<Id>()
        .Destructure.AsScalar<SubscriptionAddress>()
        .Destructure.AsScalar<CommandType>()
        .Destructure.AsScalar<EventHandlerType>()
        .Destructure.AsScalar<EventType>()
        .Destructure.AsScalar<NotificationHandlerType>()
        .Destructure.AsScalar<NotificationType>()
        .Destructure.AsScalar<ReportListQueryType>()
        .Destructure.AsScalar<ReportQueryType>()
        .Destructure.AsScalar<ReportRowType>()
        .Destructure.AsScalar<ReportType>()
        .Destructure.AsScalar<SubscriptionHandlerType>()
        .Destructure.AsScalar<SubscriptionType>()
        .Destructure.AsScalar<TopicType>()
        .Destructure.AsScalar<WorkflowType>();
}
