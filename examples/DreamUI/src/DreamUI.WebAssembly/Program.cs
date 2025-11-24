var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

ConfigureServices(builder.Services, builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();

static void ConfigureServices(IServiceCollection services, string baseAddress)
{
    services
    .AddTotemRuntime(MessagesInfo.Assembly, RuntimeInfo.Assembly)
    .AddCommands(pipeline => pipeline.UseTopic())
    .AddEvents(pipeline => pipeline.UseHandlerBus().UseReportBus().UseWorkflowBus())
    .AddEventHandlers(pipeline => pipeline.UseHandler())
    .AddReportQueries(pipeline => pipeline.UseReader())
    .AddReportListQueries(pipeline => pipeline.UseReader())
    .AddSubscriptions(pipeline => pipeline.UseHandler())
    .AddNotifications(pipeline => pipeline.UseHandler())
    .AddTopics(pipeline => pipeline.UseWhenMethod())
    .AddReports(pipeline => pipeline.UseWhenMethod())
    .AddWorkflows(pipeline => pipeline.UseWhenMethod())
    .AddEventHandlerServices()
    .AddSubscriptionHandlerServices()
    .AddNotificationHandlerServices()
    .AddExternalTopicStore()
    .AddExternalWorkflowStore()
    .AddExternalReportStore()
    .AddExternalReportBus()
    .AddExternalWorkflowBus()
    .AddExternalHandlerBus()
    .AddExternalNotificationBus()
    .AddExternalReportBroker();

    services
    .AddTotemHttpClient()
    .AddHttpCommands(pipeline => pipeline.UseRequest())
    .AddHttpReportQueries(pipeline => pipeline.UseRequest())
    .AddHttpReportListQueries(pipeline => pipeline.UseRequest())
    .AddHttpReportBindings();

    services
    .AddTotemTspClient()
    .AddTspSubscriptions(pipeline => pipeline.UseHub())
    .ConfigureWebAssembly(baseAddress);

    services.AddTotemWebClient();
    services.AddSerilog();
}
