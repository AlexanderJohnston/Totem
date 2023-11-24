namespace Totem.Map.Builder;

internal static class BuildErrors
{
    internal static readonly ErrorInfo CommandNotFound = ErrorInfo.NotFound(nameof(CommandNotFound));
    internal static readonly ErrorInfo CommandRouteReturnTypeNotId = ErrorInfo.Fatal(nameof(CommandRouteReturnTypeNotId));
    internal static readonly ErrorInfo CommandWhenNotSingleParameter = ErrorInfo.Fatal(nameof(CommandWhenNotSingleParameter));
    internal static readonly ErrorInfo CommandWhenNotTwoParameters = ErrorInfo.Fatal(nameof(CommandWhenNotTwoParameters));
    internal static readonly ErrorInfo CommandWhenReturnTypeNotVoidOrTask = ErrorInfo.Fatal(nameof(CommandWhenReturnTypeNotVoidOrTask));
    internal static readonly ErrorInfo CommandWhenSecondParameterNotCancellationToken = ErrorInfo.Fatal(nameof(CommandWhenSecondParameterNotCancellationToken));
    internal static readonly ErrorInfo EventNotFound = ErrorInfo.NotFound(nameof(EventNotFound));
    internal static readonly ErrorInfo MessageInfoNotFound = ErrorInfo.NotFound(nameof(MessageInfoNotFound));
    internal static readonly ErrorInfo GivenReturnTypeNotVoid = ErrorInfo.Fatal(nameof(GivenReturnTypeNotVoid));
    internal static readonly ErrorInfo HandlerDuplicated = ErrorInfo.Conflict(nameof(HandlerDuplicated));
    internal static readonly ErrorInfo HandlerMessageHasError = ErrorInfo.Fatal(nameof(HandlerMessageHasError));
    internal static readonly ErrorInfo HandlerMessageNotFound = ErrorInfo.NotFound(nameof(HandlerMessageNotFound));
    internal static readonly ErrorInfo HandlerNotFound = ErrorInfo.NotFound(nameof(HandlerNotFound));
    internal static readonly ErrorInfo ObserverEventsNotFound = ErrorInfo.NotFound(nameof(ObserverEventsNotFound));
    internal static readonly ErrorInfo ObserverGivenOrWhenNotFound = ErrorInfo.NotFound(nameof(ObserverGivenOrWhenNotFound));
    internal static readonly ErrorInfo ObserverRouteReturnTypeNotIdOrIdSequence = ErrorInfo.Fatal(nameof(ObserverRouteReturnTypeNotIdOrIdSequence));
    internal static readonly ErrorInfo ObserverWhenReturnTypeNotVoid = ErrorInfo.Fatal(nameof(ObserverWhenReturnTypeNotVoid));
    internal static readonly ErrorInfo ReportListQueryHasProperties = ErrorInfo.Fatal(nameof(ReportListQueryHasProperties));
    internal static readonly ErrorInfo ReportListQueryMissingDefaultConstructor = ErrorInfo.Fatal(nameof(ReportListQueryMissingDefaultConstructor));
    internal static readonly ErrorInfo ReportNotFound = ErrorInfo.NotFound(nameof(ReportNotFound));
    internal static readonly ErrorInfo ReportQueryHasProperties = ErrorInfo.Fatal(nameof(ReportQueryHasProperties));
    internal static readonly ErrorInfo ReportQueryMissingDefaultConstructor = ErrorInfo.Fatal(nameof(ReportQueryMissingDefaultConstructor));
    internal static readonly ErrorInfo ReportQueryMissingIdConstructor = ErrorInfo.Fatal(nameof(ReportQueryMissingIdConstructor));
    internal static readonly ErrorInfo ReportQueryMissingIdProperty = ErrorInfo.NotFound(nameof(ReportQueryMissingIdProperty));
    internal static readonly ErrorInfo ReportRowDuplicated = ErrorInfo.Conflict(nameof(ReportRowDuplicated));
    internal static readonly ErrorInfo ReportRowHasError = ErrorInfo.Fatal(nameof(ReportRowHasError));
    internal static readonly ErrorInfo ReportRowInfoNotFound = ErrorInfo.NotFound(nameof(ReportRowInfoNotFound));
    internal static readonly ErrorInfo ReportRowNotFound = ErrorInfo.Fatal(nameof(ReportRowNotFound));
    internal static readonly ErrorInfo ReportRowNotQueried = ErrorInfo.Fatal(nameof(ReportRowNotQueried));
    internal static readonly ErrorInfo ReportRowNotSpecified = ErrorInfo.Fatal(nameof(ReportRowNotSpecified));
    internal static readonly ErrorInfo RuntimeMethodNotPublic = ErrorInfo.Fatal(nameof(RuntimeMethodNotPublic));
    internal static readonly ErrorInfo RuntimeMethodNotStatic = ErrorInfo.Fatal(nameof(RuntimeMethodNotStatic));
    internal static readonly ErrorInfo RuntimeMethodParameterNotMessageOrContext = ErrorInfo.Fatal(nameof(RuntimeMethodParameterNotMessageOrContext));
    internal static readonly ErrorInfo RuntimeMethodPossibleTypo = ErrorInfo.Fatal(nameof(RuntimeMethodPossibleTypo));
    internal static readonly ErrorInfo RuntimeMethodStatic = ErrorInfo.Fatal(nameof(RuntimeMethodStatic));
    internal static readonly ErrorInfo RuntimeRouteHasError = ErrorInfo.Fatal(nameof(RuntimeRouteHasError));
    internal static readonly ErrorInfo RuntimeRouteNotFound = ErrorInfo.NotFound(nameof(RuntimeRouteNotFound));
    internal static readonly ErrorInfo RuntimeWhenHasError = ErrorInfo.Fatal(nameof(RuntimeWhenHasError));
    internal static readonly ErrorInfo TopicCommandsNotFound = ErrorInfo.NotFound(nameof(TopicCommandsNotFound));
    internal static readonly ErrorInfo TopicNotFound = ErrorInfo.NotFound(nameof(TopicNotFound));
    internal static readonly ErrorInfo TopicWhenNotFound = ErrorInfo.NotFound(nameof(TopicWhenNotFound));
}
