using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Outermind.SmartScanning
{
  public sealed class FolderClassifier
  {
    public ClassifiedFolder Process(SmartScanRecord record)
    {
      if(record == null)
      {
        return null;
      }

      if(string.IsNullOrWhiteSpace(record.FolderPath))
      {
        return null;
      }

      string normalizedPath;
      string parentPath = null;
      try
      {
        normalizedPath = Path.GetFullPath(record.FolderPath);
        parentPath = Directory.GetParent(normalizedPath)?.FullName;
      }
      catch
      {
        return null;
      }

      var folder = CreateFolder(normalizedPath, record);

      TryClassifyFromAncestors(folder, parentPath);

      return CloneFolder(folder);
    }

    static ClassifiedFolder CloneFolder(ClassifiedFolder source)
    {
      lock(source)
      {
        return new ClassifiedFolder
        {
          FolderPath = source.FolderPath,
          Name = source.Name,
          Kind = source.Kind,
          TotalFileCount = source.TotalFileCount,
          FirstSeenUtc = source.FirstSeenUtc,
          LastSeenUtc = source.LastSeenUtc,
          LastModifiedBy = source.LastModifiedBy,
          LastChangeTag = source.LastChangeTag
        };
      }
    }

    static ClassifiedFolder CreateFolder(string normalizedPath, SmartScanRecord record)
    {
      var trimmed = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var name = Path.GetFileName(trimmed);
      if(string.IsNullOrEmpty(name))
      {
        name = normalizedPath;
      }

      var kind = FolderClassificationRules.ClassifyByName(name);

      return new ClassifiedFolder
      {
        FolderPath = normalizedPath,
        Name = name,
        Kind = kind,
        TotalFileCount = record.FileCount,
        FirstSeenUtc = record.StartedAtUtc,
        LastSeenUtc = record.CompletedAtUtc,
        LastModifiedBy = !string.IsNullOrWhiteSpace(record.LastModifiedBy) ? Totem.Id.From(record.LastModifiedBy.ToString().Replace('\\', '_')) : Totem.Id.From("Unknown"),
        LastChangeTag = record.ChangeTag
      };
    }

    static void UpdateExistingFolder(ClassifiedFolder folder, SmartScanRecord record)
    {
      lock(folder)
      {
        folder.TotalFileCount += record.FileCount;

        if(!folder.FirstSeenUtc.HasValue || record.StartedAtUtc < folder.FirstSeenUtc.Value)
        {
          folder.FirstSeenUtc = record.StartedAtUtc;
        }

        if(!folder.LastSeenUtc.HasValue || record.CompletedAtUtc >= folder.LastSeenUtc.Value)
        {
          folder.LastSeenUtc = record.CompletedAtUtc;
          folder.LastModifiedBy = !string.IsNullOrWhiteSpace(record.LastModifiedBy) ? Totem.Id.From(record.LastModifiedBy.ToString().Replace('\\', '_')) : Totem.Id.From("Unknown");
          folder.LastChangeTag = record.ChangeTag;
        }

        if(folder.Kind == FolderKind.Unknown)
        {
          var kind = FolderClassificationRules.ClassifyByName(folder.Name);
          if(kind != FolderKind.Unknown)
          {
            folder.Kind = kind;
          }
        }
      }
    }

    static void TryClassifyFromAncestors(ClassifiedFolder folder, string parentPath)
    {
      if(string.IsNullOrEmpty(parentPath))
      {
        return;
      }

      var current = parentPath;
      while(!string.IsNullOrEmpty(current))
      {
        var trimmed = current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parentName = Path.GetFileName(trimmed);
        if(string.IsNullOrEmpty(parentName))
        {
          parentName = current;
        }

        var inferred = FolderClassificationRules.ClassifyByName(parentName);
        if(inferred != FolderKind.Unknown)
        {
          lock(folder)
          {
            if(folder.Kind == FolderKind.Unknown)
            {
              folder.Kind = inferred;
            }
          }
          break;
        }

        try
        {
          current = Directory.GetParent(current)?.FullName;
        }
        catch
        {
          break;
        }
      }
    }

    static ClassifiedFolder FindFolder(IEnumerable<ClassifiedFolder> folders, string path)
    {
      foreach(var folder in folders)
      {
        if(string.Equals(folder.FolderPath, path, StringComparison.OrdinalIgnoreCase))
        {
          return folder;
        }
      }

      return null;
    }
  }
}