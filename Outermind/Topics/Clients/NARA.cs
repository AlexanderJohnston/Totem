using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  ///  Entry point to NARA automation and processing further down the timeline.
  /// </summary>
  public class ProjectManager : Topic
  {
    void When(ProjectClassified e)
    {
      if(e.Project.ToString() == "NARA202416724")
      {
        Then(new ScanForNARA(e.FolderPath, e.UserId, e.Scan.ChangeTag));
      }
    }
  }
}
