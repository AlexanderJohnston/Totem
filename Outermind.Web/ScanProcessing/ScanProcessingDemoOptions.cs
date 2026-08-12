using System;

namespace Quantum.Web.ScanProcessing
{
  public sealed class ScanProcessingDemoOptions
  {
    public bool Enabled { get; set; }
    public bool AllowSyntheticActor { get; set; }
    public int TransitionDelayMilliseconds { get; set; } = 750;
    public int PlanLifetimeMinutes { get; set; } = 30;
  }

  public interface IScanProcessingDemoClock
  {
    DateTimeOffset UtcNow { get; }
  }

  public sealed class SystemScanProcessingDemoClock : IScanProcessingDemoClock
  {
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
  }
}
