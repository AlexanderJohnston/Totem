using Realizer.Services.Signals;
using REBL;

namespace Realizer.Hosting;

public static class ServiceCollectionExtensions
{
    public const string BaseDirectoryConfigurationKey = "Dream:Files:LocalFileStorage:BaseDirectory";

    public static IServiceCollection AddRealizer(this IServiceCollection services) =>
        services
        .AddHttpClient()
        .AddSingleton<IMultiTask, MultiTaskService>()
        .AddSingleton<IShortTermMemory<string>, ShortTermMemoryService>()
        .AddSingleton<IDownloadService, DownloadService>()
        .AddSingleton<IUnpackService, UnpackService>()
        .AddSingleton<RebelService>()
        //.AddSingleton<REBLConsole>()
        .AddSingleton<IFileStorage>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var baseDirectory = configuration[BaseDirectoryConfigurationKey];

            if(string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = Path.Combine(AppContext.BaseDirectory, "Dream.Files");
            }

            return new LocalFileStorage(baseDirectory);
        });
}
