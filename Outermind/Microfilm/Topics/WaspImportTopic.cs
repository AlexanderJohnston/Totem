using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Orchestrates WASP asset imports. Manages scheduling and asset parsing.
  /// </summary>
  public class WaspImportTopic : Topic
  {
    bool _importEnabled;
    readonly Dictionary<string, Id> _clientIdsByJobNumber = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Id> _boxIdsByClientAndBoxName = new(StringComparer.OrdinalIgnoreCase);

    bool _importInProgress;
    List<string> _remainingAssetIds = new();
    readonly HashSet<string> _retriedAssetIds = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> _importedAssetIdsInCurrentRun = new();
    int _importedCount;
    int _deferredCount;
    int _ignoredCount;

    ActiveAssetState _activeAssetState;
    string _activeAssetId;
    string _activeJobNumber;
    string _activeBoxName;
    string _activeRollName;
    Id _activeClientId;
    Id _activeBoxId;

    // Pattern: {JobNumber}-Box-{N} or {JobNumber}-Box {N}
    static readonly Regex BoxPattern = new(
      @"^(.+?)-Box[\s-]+(.+)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern: {JobNumber}-Box {N}-{Roll}
    static readonly Regex RollPattern = new(
      @"^(.+?)-Box[\s-]+(.+?)\s*-\s*(.+)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    enum ActiveAssetState
    {
      None,
      AwaitingBoxResult,
      AwaitingRollResult
    }

    //
    // Given
    //

    void Given(WaspImportEnabledSet e)
    {
      _importEnabled = e.ImportEnabled;
    }

    void Given(ClientCreated e)
    {
      _clientIdsByJobNumber[e.Client.JobNumber] = e.Client.ClientId;
    }

    void Given(ClientReassigned e)
    {
      _clientIdsByJobNumber[e.Client.JobNumber] = e.Client.ClientId;
    }

    void Given(BoxCreated e)
    {
      _boxIdsByClientAndBoxName[MakeBoxKey(e.Box.ClientId, e.Box.BoxName)] = e.Box.BoxId;

      if (IsAwaitingBoxResultFor(e.Box.ClientId, e.Box.BoxName))
      {
        RemoveAsset(_activeAssetId);
      }
    }

    void Given(BoxAlreadyExists e)
    {
      if (IsAwaitingBoxResultFor(e.ClientId, e.BoxName))
      {
        RemoveAsset(_activeAssetId);
      }
    }

    void Given(RollCreated e)
    {
      if (IsAwaitingRollResultFor(e.Roll.BoxId, e.Roll.RollName))
      {
        RemoveAsset(_activeAssetId);
      }
    }

    void Given(RollAlreadyExists e)
    {
      if (IsAwaitingRollResultFor(e.BoxId, e.RollName))
      {
        RemoveAsset(_activeAssetId);
      }
    }

    void Given(WaspImportBatchLoaded e)
    {
      ResetRunState();
      _importInProgress = true;
      _remainingAssetIds = new List<string>(e.AssetIds ?? new List<string>());
    }

    void Given(WaspImportAssetRequeued e)
    {
      RemoveAsset(e.AssetId);
      _remainingAssetIds.Add(e.AssetId);
      _retriedAssetIds.Add(e.AssetId);
    }

    void Given(WaspBoxIdentified e)
    {
      _importedCount++;
      _importedAssetIdsInCurrentRun.Add(e.AssetId);

      _activeAssetState = ActiveAssetState.AwaitingBoxResult;
      _activeAssetId = e.AssetId;
      _activeJobNumber = e.JobNumber;
      _activeBoxName = e.BoxName;
      _activeRollName = null;
      _activeClientId = e.ClientId;
      _activeBoxId = Id.Unassigned;
    }

    void Given(WaspRollIdentified e)
    {
      _importedCount++;
      _importedAssetIdsInCurrentRun.Add(e.AssetId);

      _activeAssetState = ActiveAssetState.AwaitingRollResult;
      _activeAssetId = e.AssetId;
      _activeJobNumber = e.JobNumber;
      _activeBoxName = null;
      _activeRollName = e.RollName;
      _activeClientId = e.ClientId;
      _activeBoxId = e.BoxId;
    }

    void Given(WaspAssetDeferred e)
    {
      _deferredCount++;
      RemoveAsset(e.Asset.AssetId);
    }

    void Given(WaspLegacyAssetIgnored e)
    {
      _ignoredCount++;
      RemoveAsset(e.AssetId);
    }

    void Given(WaspImportCompleted e)
    {
      ResetRunState();
    }

    void Given(WaspImportFailed e)
    {
      ResetRunState();
    }

    //
    // When
    //

    void When(SetWaspImportEnabled command)
    {
      if (_importEnabled == command.ImportEnabled)
      {
        Then(new WaspImportAlreadyInRequestedState(
          command.ImportEnabled,
          $"The WASP import scheduler is already {(command.ImportEnabled ? "enabled" : "disabled")}."));
      }
      else
      {
        var reason = command.ImportEnabled
          ? "WASP import scheduler has been enabled."
          : "WASP import scheduler has been disabled.";

        Then(new WaspImportEnabledSet(command.ImportEnabled, reason));

        if (command.ImportEnabled)
        {
          ThenSchedule.At(
            new HourlyWaspImportEvent(true, "Scheduled after enabling import"),
            Clock.Now.AddHours(1));
        }
      }
    }

    void When(ForceWaspImport command)
    {
      var trigger = string.IsNullOrWhiteSpace(command.Trigger)
        ? "Manual import requested"
        : command.Trigger.Trim();

      Then(new ManualWaspImportEvent(trigger));
    }

    async Task When(HourlyWaspImportEvent e, IWaspAssetService waspService)
    {
      await StartImport(waspService);

      if (_importEnabled)
      {
        ThenSchedule.At(
          new HourlyWaspImportEvent(true, "Scheduled recurring import"),
          Clock.Now.AddHours(1));
      }
    }

    async Task When(ManualWaspImportEvent e, IWaspAssetService waspService) =>
      await StartImport(waspService);

    void When(WaspImportBatchLoaded e) =>
      ContinueImport();

    void When(WaspImportAssetRequeued e) =>
      ContinueImport();

    void When(WaspLegacyAssetIgnored e) =>
      ContinueImport();

    void When(WaspAssetDeferred e) =>
      ContinueImport();

    void When(BoxCreated e)
    {
      if (IsAwaitingBoxResultFor(e.Box.ClientId, e.Box.BoxName))
      {
        ClearActiveAsset();
        ContinueImport();
      }
    }

    void When(BoxAlreadyExists e)
    {
      if (IsAwaitingBoxResultFor(e.ClientId, e.BoxName))
      {
        ClearActiveAsset();
        ContinueImport();
      }
    }

    void When(RollCreated e)
    {
      if (IsAwaitingRollResultFor(e.Roll.BoxId, e.Roll.RollName))
      {
        ClearActiveAsset();
        ContinueImport();
      }
    }

    void When(RollAlreadyExists e)
    {
      if (IsAwaitingRollResultFor(e.BoxId, e.RollName))
      {
        ClearActiveAsset();
        ContinueImport();
      }
    }

    async Task StartImport(IWaspAssetService waspService)
    {
      if (_importInProgress)
      {
        Then(new WaspImportFailed("A WASP import is already in progress.", "AssetImport"));
        return;
      }

      try
      {
        var assetIds = await waspService.GetAssetIdsAsync();

        Then(new WaspImportBatchLoaded(assetIds));
      }
      catch (Exception ex)
      {
        Then(new WaspImportFailed(ex.ToString(), "AssetImport"));
      }
    }

    void ContinueImport()
    {
      if (_remainingAssetIds.Count == 0)
      {
        Then(new WaspImportCompleted(
          _importedCount,
          _deferredCount,
          _ignoredCount,
          new List<string>(_importedAssetIdsInCurrentRun)));
        return;
      }

      if (!_importInProgress || _activeAssetState != ActiveAssetState.None)
      {
        return;
      }

      var assetId = _remainingAssetIds[0];

      if (TryParseRoll(assetId, out var rollJobNumber, out var rollBoxName, out var rollName))
      {
        ProcessRollAsset(assetId, rollJobNumber, rollBoxName, rollName);
        return;
      }

      if (TryParseBox(assetId, out var boxJobNumber, out var boxName))
      {
        ProcessBoxAsset(assetId, boxJobNumber, boxName);
        return;
      }

      Then(new WaspLegacyAssetIgnored(assetId, $"Asset '{assetId}' does not match the job-box or job-box-roll format."));
    }

    void ProcessBoxAsset(string assetId, string jobNumber, string boxName)
    {
      if (!_clientIdsByJobNumber.TryGetValue(jobNumber, out var clientId))
      {
        Then(new WaspLegacyAssetIgnored(assetId, $"Job number '{jobNumber}' is not registered to a known client."));
        return;
      }

      Then(new WaspBoxIdentified(assetId, jobNumber, boxName, clientId));
    }

    void ProcessRollAsset(string assetId, string jobNumber, string boxName, string rollName)
    {
      if (!_clientIdsByJobNumber.TryGetValue(jobNumber, out var clientId))
      {
        Then(new WaspLegacyAssetIgnored(assetId, $"Job number '{jobNumber}' is not registered to a known client."));
        return;
      }

      if (TryGetBoxId(clientId, boxName, out var boxId))
      {
        Then(new WaspRollIdentified(assetId, jobNumber, rollName, boxId, clientId));
      }
      else if (!_retriedAssetIds.Contains(assetId))
      {
        Then(new WaspImportAssetRequeued(assetId));
      }
      else
      {
        Then(new WaspAssetDeferred(
          new KnownWaspAsset(assetId, jobNumber, boxName, rollName),
          $"Box '{boxName}' is not yet recognized for job number '{jobNumber}'."));
      }
    }

    static string MakeBoxKey(Id clientId, string boxName) =>
      $"{clientId}|{boxName?.ToUpperInvariant()}";

    static bool TryParseBox(string assetId, out string jobNumber, out string boxName)
    {
      var match = BoxPattern.Match(assetId);

      if (match.Success)
      {
        jobNumber = match.Groups[1].Value.Trim();
        boxName = $"Box {match.Groups[2].Value.Trim()}";
        return true;
      }

      jobNumber = null;
      boxName = null;
      return false;
    }

    static bool TryParseRoll(string assetId, out string jobNumber, out string boxName, out string rollName)
    {
      var match = RollPattern.Match(assetId);

      if (match.Success)
      {
        jobNumber = match.Groups[1].Value.Trim();
        boxName = $"Box {match.Groups[2].Value.Trim()}";
        rollName = match.Groups[3].Value.Trim();
        return true;
      }

      jobNumber = null;
      boxName = null;
      rollName = null;
      return false;
    }

    bool IsAwaitingBoxResultFor(Id clientId, string boxName) =>
      _importInProgress &&
      _activeAssetState == ActiveAssetState.AwaitingBoxResult &&
      _activeClientId == clientId &&
      string.Equals(_activeBoxName, boxName, StringComparison.OrdinalIgnoreCase);

    bool IsAwaitingRollResultFor(Id boxId, string rollName) =>
      _importInProgress &&
      _activeAssetState == ActiveAssetState.AwaitingRollResult &&
      _activeBoxId == boxId &&
      string.Equals(_activeRollName, rollName, StringComparison.OrdinalIgnoreCase);

    bool TryGetBoxId(Id clientId, string boxName, out Id boxId) =>
      _boxIdsByClientAndBoxName.TryGetValue(MakeBoxKey(clientId, boxName), out boxId);

    void RemoveAsset(string assetId)
    {
      if (string.IsNullOrWhiteSpace(assetId))
      {
        return;
      }

      if (_remainingAssetIds.Count > 0 &&
          string.Equals(_remainingAssetIds[0], assetId, StringComparison.OrdinalIgnoreCase))
      {
        _remainingAssetIds.RemoveAt(0);
        return;
      }

      var index = _remainingAssetIds.FindIndex(id => string.Equals(id, assetId, StringComparison.OrdinalIgnoreCase));

      if (index >= 0)
      {
        _remainingAssetIds.RemoveAt(index);
      }
    }

    void ClearActiveAsset()
    {
      _activeAssetState = ActiveAssetState.None;
      _activeAssetId = null;
      _activeJobNumber = null;
      _activeBoxName = null;
      _activeRollName = null;
      _activeClientId = Id.Unassigned;
      _activeBoxId = Id.Unassigned;
    }

    void ResetRunState()
    {
      _importInProgress = false;
      _remainingAssetIds = new List<string>();
      _retriedAssetIds.Clear();
      _importedAssetIdsInCurrentRun.Clear();
      _importedCount = 0;
      _deferredCount = 0;
      _ignoredCount = 0;
      ClearActiveAsset();
    }
  }
}
