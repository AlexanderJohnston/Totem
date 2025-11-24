using Serilog;
using Serilog.Events;
using Serilog.Sinks.SpectreConsole;
using Totem.Core;
using Totem.Hosting;
using Realizer.Hosting;
using Realizer;
using Totem.InExternal.Services;


var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services);
ConfigureSerilog(builder.Host);

var app = builder.Build();

ConfigureWebApp(app);

await app.RunAsync();

void ConfigureServices(IServiceCollection services)
{
    services
    .AddTotemRuntime(MessagesInfo.Assembly, RuntimeInfo.Assembly)
    .AddCommands(pipeline => pipeline.UseTopic())
    .AddEvents(pipeline => pipeline.UseReportBus().UseWorkflowBus().UseHandlerBus())
    .AddEventHandlers(pipeline => pipeline.UseHandler())
    .AddReportListQueries(pipeline => pipeline.UseReader())
    .AddReportQueries(pipeline => pipeline.UseReader())
    .AddSubscriptions(pipeline => pipeline.UseHandler())
    .AddNotifications(pipeline => pipeline.UseHandler())
    .AddTopics(pipeline => pipeline.UseWhenMethod())
    .AddWorkflows(pipeline => pipeline.UseWhenMethod())
    .AddReports(pipeline => pipeline.UseWhenMethod())
    .AddEventHandlerServices()
    .AddNotificationHandlerServices()
    .AddNotificationHandlerServices()
    .AddEventStoreTopicStore(config => new EventStoreConfig())
    .AddExternalTopicStore()
    .AddExternalReportStore()
    .AddExternalWorkflowStore()
    .AddExternalReportBus()
    .AddExternalWorkflowBus()
    .AddExternalHandlerBus()
    .AddExternalNotificationBus()
    .AddExternalReportBroker();

    services.AddMvc().AddTotemMvc();
    services.AddRouting().AddControllers();
    services.AddSignalR();

    services.AddTotemHttpServer();

    services
    .AddTotemTspServer()
    .AddTspServerNotifications(pipeline => pipeline.UseHub());

    services.AddRealizer();
}

void ConfigureSerilog(ConfigureHostBuilder host) =>
    host.UseSerilog((context, logger) =>
    {
        logger
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .ConfigureTotemRuntime();

        if(Environment.UserInteractive)
        {
            logger
            .WriteTo.SpectreConsole("{Timestamp:HH:mm:ss.fff} [{Level:u4}] {Message:lj}{NewLine}{Exception}", minLevel: LogEventLevel.Verbose)
            .WriteTo.Debug();
        }

        if(!context.HostingEnvironment.IsDevelopment())
        {
            logger.MinimumLevel.Warning();
            return;
        }

        var path = context.Configuration["DREAM_LOG_PATH"] ?? @"C:\Totem\Dream\Logs";
        var file = $"{context.HostingEnvironment.ApplicationName}-{context.HostingEnvironment.EnvironmentName}.log";

        logger.WriteTo.File(Path.Combine(path, file));
    });

void ConfigureWebApp(WebApplication app)
{
    if(app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        //app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/error");
    }

    app.UseCors();
    app.UseRouting();
    app.UseStaticFiles();
    //app.UseBlazorFrameworkFiles();

    app.MapControllers();
    app.MapFallbackToFile("index.html");
    app.MapTspHub();
}
