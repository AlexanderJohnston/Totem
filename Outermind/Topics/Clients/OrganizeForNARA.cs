using System;
using Outermind;
using System.IO;
using Totem.Timeline;

namespace Outermind.Topics
{
  // Simple topic that watches for ScanForNARA and filters by folder path containing "Originals"
  public class SupervisorForNARA : Topic
  {
    void When(ScanForNARA e)
    {
      // If the path contains "Originals" (case-insensitive), emit QuantumScanDetected with same payload
      if(e?.FolderPath != null &&
          e.FolderPath.IndexOf("Originals", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        Then(new QuantumScanDetected(e.FolderPath, e.Owner, e.ChangeType));
      }
    }
  }
}

