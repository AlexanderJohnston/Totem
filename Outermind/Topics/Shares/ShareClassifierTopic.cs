using System;
using System.IO;
using Totem.Timeline;

namespace Outermind.Topics
{
  public class ShareClassifierTopic : Topic
  {
    const string SbsrFilmShare = @"\\sbsr-film\Film";
    const string SanShare = @"Y:";

    void When(ScanDetected e)
    {
      var folderPath = NormalizePath(e.Scan.FolderPath);
      var share = ClassifyShare(folderPath);

      Then(new ShareClassified(share, folderPath, e.UpdatedAtUtc, e.Scan, e.UserId));
    }

    static string ClassifyShare(string fullPath)
    {
      var root = TryGetRoot(fullPath);
      if(root.Length == 0)
      {
        return "Unknown";
      }

      if(string.Equals(root, SbsrFilmShare, StringComparison.OrdinalIgnoreCase))
      {
        return "SBSR-FILM";
      }

      if(string.Equals(root, SanShare, StringComparison.OrdinalIgnoreCase))
      {
        return "SAN";
      }

      return "Unknown";
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
