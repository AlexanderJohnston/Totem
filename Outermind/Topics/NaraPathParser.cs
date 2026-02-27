using System;
using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  /// Parses ScanForNARA paths into structured RollPathDetected events.
  /// Expects: \\server\share\{Client}\{Project}\{Pallet}\{Stage}\{Box}\{Roll}
  /// </summary>
  public class NaraPathParser : Topic
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

      var client  = segments[2];  // e.g., "NARA202416724"
      var project = segments[3];  // e.g., "1-Originals3"
      var pallet  = segments[4];  // e.g., "Pallet 10"
      var stage   = segments[5];  // e.g., "04-ReadyforQP"
      var box     = segments[6];  // e.g., "Box 01"
      var roll    = segments[7];  // e.g., "Roll_1"

      Then(new RollPathDetected(e.FolderPath, client, project, pallet, stage, box, roll));
    }
  }
}
