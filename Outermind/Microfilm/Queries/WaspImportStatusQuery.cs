using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks the full WASP import state. Single instance.
  /// </summary>
  public class WaspImportStatusQuery : Query
  {
    public bool ImportEnabled { get; set; }
    public bool IsRunning { get; set; }
    public HashSet<string> ImportedAssetIds { get; set; } = new();
    public HashSet<KnownWaspAsset> DeferredAssets { get; set; } = new();
    public HashSet<string> IgnoredLegacyAssetIds { get; set; } = new();
    public HashSet<string> LastImportedAssetIds { get; set; } = new();
    public int LastImportedAssetCount { get; set; }
    public int LastDeferredAssetCount { get; set; }
    public int LastIgnoredAssetCount { get; set; }
    public string LastError { get; set; }
    public string LastFailureStep { get; set; }

    void Given(WaspImportEnabledSet e)
    {
      ImportEnabled = e.ImportEnabled;
    }

    void Given(WaspImportAlreadyInRequestedState e)
    {
      // ImportEnabled unchanged
    }

    void Given(HourlyWaspImportEvent e)
    {
      ResetLastRun();
    }

    void Given(ManualWaspImportEvent e)
    {
      ResetLastRun();
    }

    void Given(WaspImportStarted e)
    {
      IsRunning = true;
      ResetLastRun();
    }

    void Given(WaspBoxIdentified e)
    {
      ImportedAssetIds.Add(e.AssetId);
      LastImportedAssetIds.Add(e.AssetId);
    }

    void Given(WaspRollIdentified e)
    {
      ImportedAssetIds.Add(e.AssetId);
      LastImportedAssetIds.Add(e.AssetId);
    }

    void Given(WaspClientAssetsAccepted e)
    {
      foreach (var box in e.Boxes)
      {
        ImportedAssetIds.Add(box.AssetId);
        LastImportedAssetIds.Add(box.AssetId);
      }

      foreach (var roll in e.Rolls)
      {
        ImportedAssetIds.Add(roll.AssetId);
        LastImportedAssetIds.Add(roll.AssetId);
      }

      LastImportedAssetCount += e.Boxes.Count + e.Rolls.Count;
    }

    void Given(WaspLegacyAssetIgnored e)
    {
      IgnoredLegacyAssetIds.Add(e.AssetId);
      LastIgnoredAssetCount++;
    }

    void Given(WaspLegacyAssetsIgnored e)
    {
      foreach (var asset in e.Assets)
      {
        IgnoredLegacyAssetIds.Add(asset.AssetId);
      }

      LastIgnoredAssetCount += e.Assets.Count;
    }

    void Given(WaspAssetDeferred e)
    {
      DeferredAssets.Add(e.Asset);
      LastDeferredAssetCount++;
    }

    void Given(WaspAssetAlreadyImported e)
    {
      // ImportedAssetIds unchanged — already recorded
    }

    void Given(WaspImportCompleted e)
    {
      IsRunning = false;

      if (HasLegacyCompletionSummary(e))
      {
        LastImportedAssetCount = e.ImportedAssetCount;
        LastDeferredAssetCount = e.DeferredAssetCount;
        LastIgnoredAssetCount = e.IgnoredAssetCount;
        LastImportedAssetIds = new HashSet<string>(e.ImportedAssetIds);
        LastError = null;
      }
    }

    void Given(WaspImportFailed e)
    {
      IsRunning = false;
      LastError = e.Error;
      LastFailureStep = e.Step;
    }

    void Given(WaspClientImportFailed e)
    {
      LastError = e.Error;
      LastFailureStep = "ClientImport";
    }

    static bool HasLegacyCompletionSummary(WaspImportCompleted e) =>
      e.ImportedAssetCount > 0
      || e.DeferredAssetCount > 0
      || e.IgnoredAssetCount > 0
      || (e.ImportedAssetIds?.Count ?? 0) > 0;

    void ResetLastRun()
    {
      LastError = null;
      LastFailureStep = null;
      LastImportedAssetIds = new HashSet<string>();
      LastImportedAssetCount = 0;
      LastDeferredAssetCount = 0;
      LastIgnoredAssetCount = 0;
    }
  }
}
