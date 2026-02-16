using System;
using Totem;

namespace Outermind.SmartScanning
{
  public enum FolderKind
  {
    Unknown = 0,
    Scanning,
    Processing,
    Derivative,
    Delivery,
    Indexing,
    QualityCheck
  }

  public sealed class ClassifiedFolder
  {
    public string FolderPath { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FolderKind Kind { get; set; } = FolderKind.Unknown;

    public int TotalFileCount { get; set; }

    public DateTime? FirstSeenUtc { get; set; }

    public DateTime? LastSeenUtc { get; set; }

    public Id LastModifiedBy { get; set; }

    public string LastChangeTag { get; set; }
  }

  public static class FolderClassificationRules
  {
    public static FolderKind ClassifyByName(string folderName)
    {
      if(string.IsNullOrWhiteSpace(folderName))
      {
        return FolderKind.Unknown;
      }

      var name = folderName.ToLowerInvariant();

      if(name.Contains("original"))
      {
        return FolderKind.Scanning;
      }

      if(name.Contains("frame"))
      {
        return FolderKind.Processing;
      }

      if(name.Contains("derivative") || name.Contains("derivitiv"))
      {
        return FolderKind.Derivative;
      }

      if(name.Contains("deliverable") || name.Contains("deliver"))
      {
        return FolderKind.Delivery;
      }

      if(name.Contains("qc"))
      {
        return FolderKind.QualityCheck;
      }

      if(name.Contains("indexing") || name.Contains("index") || name.Contains("split"))
      {
        return FolderKind.Indexing;
      }

      return FolderKind.Unknown;
    }
  }
}
