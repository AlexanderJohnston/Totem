using Outermind.SmartScanning;
using Totem.Timeline;

namespace Outermind.Topics
{
  public class FolderClassifierTopic : Topic
  {
    readonly FolderClassifier _classifier = new FolderClassifier();

    void When(ScanDetected e)
    {
      var snapshot = _classifier.Process(e.Scan);

      //Emit one event per folder.
      Then(new FolderClassified(snapshot, e.UpdatedAtUtc, e.Scan, e.UserId));
    }
  }
}
