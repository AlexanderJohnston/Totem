using System.IO;
using Quantum.ServiceContracts;
using Totem.Timeline;

namespace Outermind.Topics
{
  // Receives QuantumScanDetected events and parses user logs if present
  public class QuantumScanManager : Topic
  {
    void When(QuantumScanDetected e, IUsersLogParser parser)
    {
      if(e == null || string.IsNullOrWhiteSpace(e.FolderPath))
      {
        return;
      }
      var logPath = Path.Combine(e.FolderPath, "users.log.text");
      if(File.Exists(logPath))
      {
        var userEvents = parser.ParseFile(logPath);
        Then(new UsersLogParsed(e.FolderPath, userEvents));
      }
    }
  }
}

