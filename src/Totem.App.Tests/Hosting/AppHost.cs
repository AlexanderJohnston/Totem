using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Xunit.Sdk;
using Totem.Runtime;
using Totem.Threading;

namespace Totem.App.Tests.Hosting
{
  /// <summary>
  /// Hosts a timeline application for the duration of a test
  /// </summary>
  public abstract class AppHost : Connection
  {
    readonly TaskSource _startup = new TaskSource();
    readonly TaskSource _shutdown = new TaskSource();
    IHostApplicationLifetime _lifetimeService;

    protected override async Task Open()
    {
      BuildAndRun();

      await _startup.Task;
    }

    protected override Task Close()
    {
      _lifetimeService.StopApplication();

      return _shutdown.Task;
    }

    void BuildAndRun()
    {
      CreateBuilder().Build().RunAsync().ContinueWith(StopHost);
    }

    protected abstract IHostBuilder CreateBuilder();

    internal void SetLifetimeService(IHostApplicationLifetime lifetimeService)
    {
      _lifetimeService = lifetimeService;

      lifetimeService.ApplicationStarted.Register(_startup.SetResult);
      lifetimeService.ApplicationStopped.Register(_shutdown.SetResult);
    }

    void StopHost(Task runTask)
    {
      if(runTask.IsFaulted)
      {
        _startup.SetException(runTask.Exception);
      }

      _shutdown.TrySetResult();
    }
  }
}
