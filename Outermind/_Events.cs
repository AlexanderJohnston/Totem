using System;
using System.Collections.Generic;
using Outermind.SmartScanning;
using Quantum.SmartScanning;
using Totem;
using Totem.Timeline;

namespace Outermind
{

  /// <summary>
  /// Type 1: Command operating on new state
  /// </summary>
  public class StartScan : Command
  {
    public Id Roll;
    public Id Operator;
    public DateTime StartTime;

    public StartScan(Id roll, Id @operator, DateTime startTime)
    {
      Roll = roll;
      Operator = @operator;
      StartTime = startTime;
    }
  }

  public class ScanStarted : Event
  {
    public Id Roll;
    public Id Operator;
    public DateTime StartTime;

    public ScanStarted(Id roll, Id @operator, DateTime startTime)
    {
      Roll = roll;
      Operator = @operator;
      StartTime = startTime;
    }
  }

  public class ScanAlreadyInProgress : Event
  {
    public Id Roll;
    public ScanAlreadyInProgress(Id roll)
    {
      Roll = roll;
    }
  }

  public class FinishScan : Command
  {
    public Id Roll;
    public DateTime EndTime;
    public FinishScan(Id roll, DateTime finishTime)
    {
      Roll = roll;
      EndTime = finishTime;
    }
  }

  public class ScanFinished : Event
  {
    public Id Roll;
    public DateTime FinishTime;

    public ScanFinished(Id roll, DateTime finishTime)
    {
      Roll = roll;
      FinishTime = finishTime;
    }
  }

  /// <summary>
  /// Type 2: Command operating on existing state
  /// </summary>
  public class DeleteScan : Command
  {
    public Id Roll;

    public DeleteScan(Id rollId)
    {
      Roll = rollId;
    }
  }

  public class ScanDeleted : Event
  {
    public Id Roll;

    public ScanDeleted(Id rollId)
    {
      Roll = rollId;
    }
  }
  public class NothingToDelete : Event
  {
    public Id Roll;
    public string Reason;

    public NothingToDelete(Id rollId, string reason)
    {
      Roll = rollId;
      Reason = reason;
    }
  }

  public class ScanNotFound : Event
  {
    public Id Roll;
    public string Reason;

    public ScanNotFound(Id rollId, string reason)
    {
      Roll = rollId;
      Reason = reason;
    }
  }

  /// <summary>
  /// Type 2: Command operating on existing state
  /// </summary>
  public class MoveScan : Command
  {
    public Id Roll;
    public Id Operator;
    public string Source;
    public string Destination;

    public MoveScan(Id rollId, Id operatorId, string source, string destination)
    {
      Roll = rollId;
      Operator = operatorId;
      Source = source;
      Destination = destination;
    }
  }

  public class ScanMoved : Event
  {
    public Id Roll;
    public Id Operator;
    public string Source;
    public string Destination;

    public ScanMoved(Id rollId, Id operatorId, string source, string destination)
    {
      Roll = rollId;
      Operator = operatorId;
      Source = source;
      Destination = destination;
    }
  }

  public class CannotMoveScan : Event
  {
    public Id Roll;
    public string Source;
    public string Destination;
    public string Reason;

    public CannotMoveScan(Id rollId, string reason)
    {
      Roll = rollId;
      Reason = reason;
    }
  }


  public class OperatorComment : Command
  {
    public Id Roll;
    public Id Operator;
    public string Notes;

    public OperatorComment(Id rollId, Id operatorId, string notes)
    {
      Roll = rollId;
      Operator = operatorId;
      Notes = notes;
    }
  }

  public class CommentAdded : Event
  {
    public Id Roll;
    public Id Operator;
    public string Notes;

    public CommentAdded(Id rollId, Id operatorId, string notes)
    {
      Roll = rollId;
      Operator = operatorId;
      Notes = notes;
    }
  }

  public class CommentRefused : Event
  {
    public Id Roll;
    public Id Operator;
    public string Notes;
    public string Reason;

    public CommentRefused(Id rollId, Id operatorId, string notes, string reason)
    {
      Roll = rollId;
      Operator = operatorId;
      Notes = notes;
      Reason = reason;
    }
  }

  public class UpdateSettings : Command
  {
    public Id Roll;
    public Dictionary<string, string> Settings;
    public UpdateSettings(Id rollId, Dictionary<string, string> settings)
    {
      Roll = rollId;
      Settings = settings;
    }
  }

  public class SettingsUpdated : Event
  {
    public Id Roll;
    public Dictionary<string, string> Settings;
    public SettingsUpdated(Id rollId, Dictionary<string, string> settings)
    {
      Roll = rollId;
      Settings = settings;
    }
  }

  public class SettingsRefused : Event
  {
    public Id Roll;
    public Dictionary<string, string> Settings;
    public string Reason;

    public SettingsRefused(Id rollId, Dictionary<string, string> settings, string reason)
    {
      Roll = rollId;
      Settings = settings;
      Reason = reason;
    }
  }

  public class UpdateScanRegistry : Command
  {
    public DateTime RequestedAtUtc;
    public List<SmartScanRecord> Scans;

    public UpdateScanRegistry(IEnumerable<SmartScanRecord> scans)
    {
      RequestedAtUtc = DateTime.UtcNow;
      Scans = scans == null ? new List<SmartScanRecord>() : new List<SmartScanRecord>(scans);
    }
  }



  public class ScanDetected : Event
  {
    public SmartScanRecord Scan;
    public Id UserId;
    public DateTime UpdatedAtUtc;

    public ScanDetected(SmartScanRecord scan, Id userId, DateTime updatedAtUtc)
    {
      Scan = scan;
      UserId = userId;
      UpdatedAtUtc = updatedAtUtc;
    }
  }

  public class ScanRejected : Event
  {
    public SmartScanRecord Scan;
    public Id UserId;
    public DateTime UpdatedAtUtc;

    public ScanRejected(SmartScanRecord scan, Id userId, DateTime updatedAtUtc)
    {
      Scan = scan;
      UserId = userId;
      UpdatedAtUtc = updatedAtUtc;
    }
  }

  public class RegisterScans : Event
  {
    public DateTime RequestedAtUtc;
    public List<SmartScanRecord> Scans;

    public RegisterScans(IEnumerable<SmartScanRecord> scans, DateTime requestedAtUtc)
    {
      RequestedAtUtc = requestedAtUtc;
      Scans = new List<SmartScanRecord>(scans);
    }
  }

  public enum OffTaskKind
  {
    Break,
    ShiftGap
  }

  public class TimeOffTask : Event
  {
    public Id UserId;
    public DateTime OffTaskBeganAtUtc;
    public DateTime ResumedAtUtc;
    public string LastFolderPath;
    public string NextFolderPath;
    public OffTaskKind Kind;

    public TimeOffTask(Id userId, DateTime offTaskBeganAtUtc, DateTime resumedAtUtc, string lastFolderPath, string nextFolderPath, OffTaskKind kind)
    {
      UserId = userId;
      OffTaskBeganAtUtc = offTaskBeganAtUtc;
      ResumedAtUtc = resumedAtUtc;
      LastFolderPath = lastFolderPath ?? string.Empty;
      NextFolderPath = nextFolderPath ?? string.Empty;
      Kind = kind;
    }
  }

  public class FolderClassified : Event
  {
    public ClassifiedFolder Folder;
    public DateTime UpdatedAtUtc;
    public SmartScanRecord Scan;
    public Id UserId;

    public FolderClassified(ClassifiedFolder folder, DateTime updatedAtUtc, SmartScanRecord scan, Id userId)
    {
      Folder = folder;
      UpdatedAtUtc = updatedAtUtc;
      Scan = scan;
      UserId = userId;
    }
  }

  public class ShareClassified : Event
  {
    public string Share;
    public string FolderPath;
    public DateTime UpdatedAtUtc;
    public SmartScanRecord Scan;
    public Id UserId;

    public ShareClassified(string share, string folderPath, DateTime updatedAtUtc, SmartScanRecord scan, Id userId)
    {
      Share = string.IsNullOrWhiteSpace(share) ? "Unknown" : share;
      FolderPath = folderPath ?? string.Empty;
      UpdatedAtUtc = updatedAtUtc;
      Scan = scan;
      UserId = userId;
    }
  }

  public class ProjectClassified : Event
  {
    public Id Project;
    public Id UserId;
    public string FolderPath;
    public SmartScanRecord Scan;

    public ProjectClassified(string project, string folderPath, SmartScanRecord scan, Id user)
    {
      Project = string.IsNullOrWhiteSpace(project) ? Id.From("Unknown") : Id.From(project);
      FolderPath = folderPath ?? string.Empty;
      Scan = scan;
      UserId = user;
    }
  }

  public class QuantumScanDetected : Event
  {
    public string FolderPath;
    public Id Owner;
    public string ChangeType;

    public QuantumScanDetected(string folderPath, Id owner, string changeType)
    {
      FolderPath = folderPath ?? string.Empty;
      Owner = owner;
      ChangeType = changeType;
    }
  }

  public class UsersLogParsed : Event
  {
    public string FolderPath;
    public List<UserEvent> UserEvents;

    public UsersLogParsed(string folderPath, List<UserEvent> userEvents)
    {
      FolderPath = folderPath ?? string.Empty;
      UserEvents = userEvents ?? new List<UserEvent>();
    }
  }
  public class ScanForNARA : Event
  {
    public string FolderPath;
    public Id Owner;
    public string ChangeType;

    public ScanForNARA(string folderPath, Id owner, string changeType)
    {
      FolderPath = folderPath;
      Owner = owner;
      ChangeType = changeType;
    }
  }
}
