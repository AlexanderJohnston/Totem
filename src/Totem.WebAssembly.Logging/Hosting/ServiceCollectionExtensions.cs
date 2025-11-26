using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Hosting;
using Serilog.Extensions.Logging;

namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSerilog(this IServiceCollection services, Action<LoggerConfiguration>? configure = null)
    {
        // Adapted from https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/SerilogWebHostBuilderExtensions.cs

        var configuration = new LoggerConfiguration().ConfigureTotemRuntime();

        configure?.Invoke(configuration);

        var logger = configuration.CreateLogger();
        var diagnosticContext = new DiagnosticContext(logger);

        services
        .AddSingleton(logger)
        .AddSingleton(diagnosticContext)
        .AddSingleton<IDiagnosticContext>(diagnosticContext)
        .AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));

        return services;
    }
}
