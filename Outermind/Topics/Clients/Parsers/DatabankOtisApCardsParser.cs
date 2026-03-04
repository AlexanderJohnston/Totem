using Outermind;
using System;
using Totem.Timeline;

namespace Quantum.Topics.Clients.Parsers
{
  /// <summary>
  /// Parses ScanForDatabankOtisAp paths into structured RollPathDetected events.
  /// </summary>
  public class DatabankOtisApCardsParser : Topic
  {
    void When(ScanForNARA e)
    {
      if (string.IsNullOrWhiteSpace(e.FolderPath))
        return;

      var segments = e.FolderPath.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      // Need at least 8 segments: server, share, client, project, pallet, stage, box, roll
      if (segments.Length < 8)
        return;

      var client = segments[2];  // e.g., "DatabankOtisApCards202518052"
      var project = segments[3];  // e.g., "1-Originals"
      var pallet = segments[4];  // e.g., "0-Verified"
      var box = segments[5];  // e.g., "Level 14 Tray 1"
      var roll = segments[6];  // e.g., "1F7057A"


      Then(new RollPathDetected(e.FolderPath, client, project, pallet, box, roll));
    }
  }
}
