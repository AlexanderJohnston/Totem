using System;
using System.Collections.Generic;
using System.Linq;
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
    readonly HashSet<string> _importedAssetIds = new(StringComparer.OrdinalIgnoreCase);

    // Pattern: {JobNumber}-Box-{N} or {JobNumber}-Box {N}
    static readonly Regex BoxPattern = new(
      @"^(.+?)-Box[\s-]+(.+)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern: {JobNumber}-Box {N}-{Roll}
    static readonly Regex RollPattern = new(
      @"^(.+?)-Box[\s-]+(.+?)\s*-\s*(.+)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
      var key = MakeBoxKey(e.Box.ClientId, e.Box.BoxName);
      _boxIdsByClientAndBoxName[key] = e.Box.BoxId;
    }

    void Given(WaspBoxIdentified e)
    {
      _importedAssetIds.Add(e.AssetId);
    }

    void Given(WaspRollIdentified e)
    {
      _importedAssetIds.Add(e.AssetId);
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
      await RunImport(waspService);

      if (_importEnabled)
      {
        ThenSchedule.At(
          new HourlyWaspImportEvent(true, "Scheduled recurring import"),
          Clock.Now.AddHours(1));
      }
    }

    async Task When(ManualWaspImportEvent e, IWaspAssetService waspService) =>
      await RunImport(waspService);

    async Task RunImport(IWaspAssetService waspService)
    {
      var importedCount = 0;
      var deferredCount = 0;
      var ignoredCount = 0;
      var importedAssetIds = new List<string>();

      try
      {
        var assetIds = await waspService.GetAssetIdsAsync();

        // Collect box asset IDs from this import for roll resolution
        var boxesInThisImport = new Dictionary<string, Id>(StringComparer.OrdinalIgnoreCase);

        foreach (var assetId in assetIds)
        {
          if (_importedAssetIds.Contains(assetId))
          {
            Then(new WaspAssetAlreadyImported(assetId, $"Asset '{assetId}' has already been processed by a previous import."));
            continue;
          }

          var rollMatch = RollPattern.Match(assetId);
          if (rollMatch.Success)
          {
            // This is a roll: {JobNumber}-Box {N}-{Roll}
            var jobNumber = rollMatch.Groups[1].Value.Trim();
            var boxName = $"Box {rollMatch.Groups[2].Value.Trim()}";
            var rollName = rollMatch.Groups[3].Value.Trim();

            if (!_clientIdsByJobNumber.TryGetValue(jobNumber, out var clientId))
            {
              var asset = new KnownWaspAsset(assetId, jobNumber, boxName, rollName, Id.Unassigned, Id.Unassigned);
              Then(new WaspAssetDeferred(asset, $"Job number '{jobNumber}' does not map to a known client."));
              deferredCount++;
              continue;
            }

            var boxKey = MakeBoxKey(clientId, boxName);
            if (!_boxIdsByClientAndBoxName.TryGetValue(boxKey, out var boxId) &&
                !boxesInThisImport.TryGetValue(boxKey, out boxId))
            {
              var asset = new KnownWaspAsset(assetId, jobNumber, boxName, rollName, clientId, Id.Unassigned);
              Then(new WaspAssetDeferred(asset, $"Box '{boxName}' is not yet recognized for client '{clientId}' and was not created in this import."));
              deferredCount++;
              continue;
            }

            Then(new WaspRollIdentified(assetId, jobNumber, rollName, boxId, clientId));
            importedAssetIds.Add(assetId);
            importedCount++;
            continue;
          }

          var boxMatch = BoxPattern.Match(assetId);
          if (boxMatch.Success)
          {
            // This is a box: {JobNumber}-Box-{N}
            var jobNumber = boxMatch.Groups[1].Value.Trim();
            var boxName = $"Box {boxMatch.Groups[2].Value.Trim()}";

            if (!_clientIdsByJobNumber.TryGetValue(jobNumber, out var clientId))
            {
              var asset = new KnownWaspAsset(assetId, jobNumber, boxName, null, Id.Unassigned, Id.Unassigned);
              Then(new WaspAssetDeferred(asset, $"Job number '{jobNumber}' does not map to a known client."));
              deferredCount++;
              continue;
            }

            Then(new WaspBoxIdentified(assetId, jobNumber, boxName, clientId));

            // Track for roll resolution within this import
            var boxKey = MakeBoxKey(clientId, boxName);
            if (!boxesInThisImport.ContainsKey(boxKey))
            {
              boxesInThisImport[boxKey] = Id.Unassigned;
            }

            importedAssetIds.Add(assetId);
            importedCount++;
            continue;
          }

          // No match — not a supported WASP box/roll asset shape
          Then(new WaspLegacyAssetIgnored(assetId, $"Asset '{assetId}' does not match the job-box or job-box-roll format."));
          ignoredCount++;
        }

        Then(new WaspImportCompleted(importedCount, deferredCount, ignoredCount, importedAssetIds));
      }
      catch (Exception ex)
      {
        Then(new WaspImportFailed(ex.ToString(), "AssetImport"));
      }
    }

    static string MakeBoxKey(Id clientId, string boxName) =>
      $"{clientId}|{boxName?.ToUpperInvariant()}";
  }
}
