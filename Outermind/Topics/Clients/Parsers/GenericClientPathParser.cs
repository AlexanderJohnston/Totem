using Outermind;
using Quantum.Topics.Clients;
using System;
using System.Linq;
using Totem.Timeline;

namespace Quantum.Topics.Clients.Parsers
{
  /// <summary>
  /// Parses any ClientScanDetected path into structured RollPathDetected events.
  /// Uses ClientProfileRegistry to determine segment positions.
  /// Universal: Client = segments[2], Box = segments[^2], Roll = segments[^1].
  /// </summary>
  public class GenericClientPathParser : Topic
  {
    void When(ClientScanDetected e)
    {
      var result = TryParse(e.FolderPath, e.ProfileName);
      if (result != null)
        Then(result);
    }

    /// <summary>
    /// Extracts Client, Project, Pallet, Box, and Roll from a folder path
    /// using the profile identified by profileName.
    /// Returns null if the path is invalid or no matching profile is found.
    /// </summary>
    public static NewRollDiscovered TryParse(string folderPath, string profileName)
    {
      if (string.IsNullOrWhiteSpace(folderPath))
        return null;

      var segments = folderPath.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      var profile = ClientProfileRegistry.Profiles
        .FirstOrDefault(p => p.ClientPrefix.Equals(profileName, StringComparison.OrdinalIgnoreCase));

      if (profile == null)
        return null;

      if (segments.Length < profile.MinSegments)
        return null;

      var client  = segments[2];
      var project = segments[3];
      var pallet  = segments[profile.PalletSegmentIndex];
      var box     = segments[segments.Length - 2];
      var roll    = segments[segments.Length - 1];

      return new NewRollDiscovered(client, pallet, box, roll, folderPath);
    }
  }
}
