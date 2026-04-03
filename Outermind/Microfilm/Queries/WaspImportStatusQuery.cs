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
      LastError = null;
      LastFailureStep = null;
      LastImportedAssetIds = new HashSet<string>();
    }

    void Given(ManualWaspImportEvent e)
    {
      LastError = null;
      LastFailureStep = null;
      LastImportedAssetIds = new HashSet<string>();
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

    void Given(WaspLegacyAssetIgnored e)
    {
      IgnoredLegacyAssetIds.Add(e.AssetId);
    }

    void Given(WaspAssetDeferred e)
    {
      DeferredAssets.Add(e.Asset);
    }

    void Given(WaspAssetAlreadyImported e)
    {
      // ImportedAssetIds unchanged — already recorded
    }

    void Given(WaspImportCompleted e)
    {
      LastImportedAssetCount = e.ImportedAssetCount;
      LastDeferredAssetCount = e.DeferredAssetCount;
      LastIgnoredAssetCount = e.IgnoredAssetCount;
      LastImportedAssetIds = new HashSet<string>(e.ImportedAssetIds);
      LastError = null;
    }

    void Given(WaspImportFailed e)
    {
      LastError = e.Error;
      LastFailureStep = e.Step;
    }
  }
}
