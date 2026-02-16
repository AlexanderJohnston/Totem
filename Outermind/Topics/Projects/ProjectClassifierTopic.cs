using System;
using System.IO;
using Totem.Timeline;

namespace Outermind.Topics
{
  public class ProjectClassifierTopic : Topic
  {
    void When(ScanDetected e)
    {
      var folderPath = NormalizePath(e.Scan.FolderPath);
      var project = ClassifyProject(folderPath);
      var user = !string.IsNullOrWhiteSpace(e.Scan.LastModifiedBy) ? Totem.Id.From(e.Scan.LastModifiedBy.Replace('\\', '_')) : Totem.Id.From("Unknown");

      Then(new ProjectClassified(project, folderPath, e.Scan, e.UserId));
    }

    static string ClassifyProject(string fullPath)
    {
      var root = TryGetRoot(fullPath);
      if(root.Length == 0 || root.Length > fullPath.Length)
      {
        return "Unknown";
      }

      var remainder = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      if(string.IsNullOrEmpty(remainder))
      {
        return "Unknown";
      }

      var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
      var parts = remainder.Split(separators, StringSplitOptions.RemoveEmptyEntries);
      if(parts.Length == 0)
      {
        return "Unknown";
      }

      return parts[0];
    }

    static string NormalizePath(string path)
    {
      try
      {
        return Path.GetFullPath(path);
      }
      catch
      {
        return path ?? string.Empty;
      }
    }

    static string TryGetRoot(string path)
    {
      try
      {
        var root = Path.GetPathRoot(path);
        if(string.IsNullOrEmpty(root))
        {
          return string.Empty;
        }

        return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      }
      catch
      {
        return string.Empty;
      }
    }
  }
}
