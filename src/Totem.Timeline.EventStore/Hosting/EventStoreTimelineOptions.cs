using System;

namespace Totem.Timeline.EventStore.Hosting
{
  /// <summary>
  /// Configures the timeline's connection to an instance of EventStore
  /// </summary>
  public class EventStoreTimelineOptions
  {
    public bool Verbose { get; set; } = false;
    public string ConnectionString { get; set; }
    public ServerOptions Server { get; set; } = new ServerOptions();
    public ConnectionOptions Connection { get; set; } = new ConnectionOptions();
    public ProjectionOptions Projections { get; set; } = new ProjectionOptions();

    public class ServerOptions
    {
      public string Name { get; set; } = "localhost";
      public int Port { get; set; } = 2113;
      public bool Insecure { get; set; } = true;
    }

    public class ConnectionOptions
    {
      public string Username { get; set; }
      public string Password { get; set; }
      public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    public class ProjectionOptions
    {
      public TimeSpan InstallTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
  }
}