using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Orchestrates WASP asset imports with minimal durable state.
  /// </summary>
  public class WaspImportTopic : Topic
  {
    bool _importEnabled;
    bool _importInProgress;
    int _clientPosition;
    readonly HashSet<string> _knownJobNumbers = new(StringComparer.OrdinalIgnoreCase);

    void Given(ClientCreated e)
    {
      AddKnownJobNumber(e.Client?.JobNumber);
    }

    void Given(ClientReassigned e)
    {
      AddKnownJobNumber(e.Client?.JobNumber);
    }

    void Given(WaspImportEnabledSet e)
    {
      _importEnabled = e.ImportEnabled;
    }

    void Given(WaspImportStarted e)
    {
      _importInProgress = true;
      _clientPosition = e.ClientPosition;
    }

    void Given(WaspImportClientHandled e)
    {
      if (_importInProgress)
      {
        _clientPosition++;
      }
    }

    void Given(WaspImportCompleted e)
    {
      ResetRunState();
    }

    void Given(WaspImportFailed e)
    {
      ResetRunState();
    }

    void When(SetWaspImportEnabled command)
    {
      if (_importEnabled == command.ImportEnabled)
      {
        Then(new WaspImportAlreadyInRequestedState(
          command.ImportEnabled,
          $"The WASP import scheduler is already {(command.ImportEnabled ? "enabled" : "disabled")}"));
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
      await StartImport();

      if (_importEnabled)
      {
        ThenSchedule.At(
          new HourlyWaspImportEvent(true, "Scheduled recurring import"),
          Clock.Now.AddHours(1));
      }
    }

    async Task When(ManualWaspImportEvent e, IWaspAssetService waspService) =>
      await StartImport();

    async Task When(WaspImportStarted e, IWaspAssetService waspService) =>
      await ContinueImport(waspService);

    async Task When(WaspImportClientHandled e, IWaspAssetService waspService) =>
      await ContinueImport(waspService);

    Task StartImport()
    {
      if (_importInProgress)
      {
        Then(new WaspImportFailed("A WASP import is already in progress.", "AssetImport"));
      }
      else
      {
        Then(new WaspImportStarted(0));
      }

      return Task.CompletedTask;
    }

    async Task ContinueImport(IWaspAssetService waspService)
    {
      if (!_importInProgress)
      {
        return;
      }

      try
      {
        var knownJobNumbers = _knownJobNumbers.ToArray();
        var batch = await waspService.GetClientBatchAsync(_clientPosition, knownJobNumbers);

        if (batch == null)
        {
          Then(new WaspImportCompleted());
          return;
        }

        if ((batch.AssetIds?.Count ?? 0) == 0)
        {
          Then(new WaspImportClientHandled(batch.JobNumber));
          return;
        }

        var ignoredAssets = new List<IgnoredWaspLegacyAsset>();
        var acceptedBoxes = new List<WaspAcceptedBoxAsset>();
        var acceptedRolls = new List<WaspAcceptedRollAsset>();

        foreach (var assetId in batch.AssetIds.OrderBy(assetId => assetId, StringComparer.OrdinalIgnoreCase))
        {
          if (TryAcceptAsset(batch.JobNumber, assetId, acceptedBoxes, acceptedRolls))
          {
            continue;
          }

          ignoredAssets.Add(new IgnoredWaspLegacyAsset(assetId, DescribeIgnoredAsset(batch.JobNumber, assetId)));
        }

        if (ignoredAssets.Count > 0)
        {
          Then(new WaspLegacyAssetsIgnored(ignoredAssets));
        }

        if (acceptedBoxes.Count > 0 || acceptedRolls.Count > 0)
        {
          Then(new WaspClientAssetsImported(batch.JobNumber, acceptedBoxes, acceptedRolls));
        }
        else
        {
          Then(new WaspImportClientHandled(batch.JobNumber));
        }
      }
      catch (Exception ex)
      {
        Then(new WaspImportFailed(ex.ToString(), "AssetImport"));
      }
    }

    static string DescribeIgnoredAsset(string jobNumber, string assetId)
    {
      return string.IsNullOrWhiteSpace(jobNumber)
        ? $"Asset '{assetId}' does not contain a recognized client/job number prefix."
        : $"Asset '{assetId}' does not match a supported WASP box or roll format for job number '{jobNumber}'.";
    }

    static bool TryAcceptAsset(
      string jobNumber,
      string assetId,
      ICollection<WaspAcceptedBoxAsset> acceptedBoxes,
      ICollection<WaspAcceptedRollAsset> acceptedRolls)
    {
      if (!string.IsNullOrWhiteSpace(jobNumber)
        && WaspAssetTagParser.TryParseRoll(assetId, out var rollJobNumber, out var boxName, out var rollName)
        && string.Equals(rollJobNumber, jobNumber, StringComparison.OrdinalIgnoreCase))
      {
        acceptedRolls.Add(new WaspAcceptedRollAsset(assetId, boxName, rollName));
        return true;
      }

      if (!string.IsNullOrWhiteSpace(jobNumber)
        && WaspAssetTagParser.TryParseBox(assetId, out var boxJobNumber, out boxName)
        && string.Equals(boxJobNumber, jobNumber, StringComparison.OrdinalIgnoreCase))
      {
        acceptedBoxes.Add(new WaspAcceptedBoxAsset(assetId, boxName));
        return true;
      }

      return false;
    }

    void ResetRunState()
    {
      _importInProgress = false;
      _clientPosition = 0;
    }

    void AddKnownJobNumber(string jobNumber)
    {
      if (!string.IsNullOrWhiteSpace(jobNumber))
      {
        _knownJobNumbers.Add(jobNumber);
      }
    }
  }
}
