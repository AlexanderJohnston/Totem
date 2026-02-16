using System;
using Totem;

namespace Outermind.SmartScanning
{
  public class SmartScanRecord
  {
    public SmartScanRecord()
    {
    }

    public SmartScanRecord(string folderPath, DateTime startedAtUtc, DateTime completedAtUtc, int fileCount, string lastModifiedBy, string changeTag = null)
    {
      FolderPath = folderPath ?? string.Empty;
      StartedAtUtc = startedAtUtc;
      CompletedAtUtc = completedAtUtc;
      FileCount = fileCount;
      LastModifiedBy = lastModifiedBy;
      ChangeTag = changeTag;
    }

    public string FolderPath { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public int FileCount { get; set; }

    public string LastModifiedBy { get; set; }

    public string ChangeTag { get; set; }
  }
}
