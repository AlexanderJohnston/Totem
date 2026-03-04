using Outermind;
using System;
using System.Collections.Generic;
using System.Text;
using Totem.Timeline;

namespace Quantum.Topics.Clients.Parsers
{
  public class NotreDameUnivParser : Topic
  {
    void When(ScanForNotreDame e)
    {
      if (string.IsNullOrWhiteSpace(e.FolderPath))
        return;

      var segments = e.FolderPath.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      // Need at least 8 segments: server, share, client, project, pallet, stage, box, roll
      if (segments.Length < 8)
        return;

      var client = segments[2];  // e.g., "NotreDameUniv202518539"
      var project = segments[3];  // e.g., "01-Originals"
      var pallet = segments[4];  // e.g., "0-Copied and Moved to be QCd"
      var box = segments[5];  // e.g., "Box 129"
      var roll = segments[6];  // e.g., "Folder 3"

      Then(new RollPathDetected(e.FolderPath, client, project, pallet, box, roll));
    }
  }
}
