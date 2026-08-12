using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// One durably ordered logical-storage authority per Version 1 client/workspace.
  /// This topic never resolves a physical location and never uses the client's Server assignment.
  /// </summary>
  public sealed class ScanProcessingStorageBindingTopic : Topic
  {
    readonly ScanProcessingStorageBindingState _state = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id RouteFirst(CreateScanProcessingStorageBinding command) => command.ClientId;
    static Id RouteFirst(ActivateScanProcessingStorageBinding command) => command.ClientId;
    static Id Route(CreateScanProcessingStorageBinding command) => command.ClientId;
    static Id Route(ActivateScanProcessingStorageBinding command) => command.ClientId;
    static Id Route(ScanProcessingStorageBindingCreated e) => Id.From(e.ClientId);
    static Id Route(ScanProcessingStorageBindingActivated e) => Id.From(e.ClientId);

    void Given(ClientCreated e) => _state.Apply(e);
    void Given(ScanProcessingStorageBindingCreated e) => _state.Apply(e);
    void Given(ScanProcessingStorageBindingActivated e) => _state.Apply(e);

    void When(CreateScanProcessingStorageBinding command)
    {
      if(!TryValidateCommon(
        command.RequestId,
        command.ActingActor,
        command.ClientId,
        ScanProcessingStorageBindingActions.Create,
        out var requestId,
        out var clientId))
      {
        return;
      }

      var bindingId = Normalize(command.BindingId);

      if(string.IsNullOrWhiteSpace(bindingId) || bindingId.Length > 100)
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Create,
          ScanProcessingStorageBindingIssueCodes.InvalidRequest,
          "The server-issued binding ID was invalid.",
          "bindingId",
          command.ActingActor,
          bindingId);
        return;
      }

      if(_state.TryGetBinding(bindingId, out _))
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Create,
          ScanProcessingStorageBindingIssueCodes.BindingAlreadyExists,
          "The logical storage binding already exists.",
          "bindingId",
          command.ActingActor,
          bindingId);
        return;
      }

      if(!TryNormalizeLabel(command.Label, out var label))
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Create,
          ScanProcessingStorageBindingIssueCodes.InvalidLabel,
          "A path-free label of at most 120 characters is required.",
          "label",
          command.ActingActor,
          bindingId);
        return;
      }

      if(!TryNormalizeCapabilities(command.Capabilities, out var capabilities))
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Create,
          ScanProcessingStorageBindingIssueCodes.InvalidCapabilities,
          "Exactly one path-free label is required for each Version 1 storage capability.",
          "capabilities",
          command.ActingActor,
          bindingId);
        return;
      }

      var revision = NextRevision;
      var binding = new ScanProcessingStorageBindingDefinition(
        bindingId,
        clientId,
        clientId,
        label,
        capabilities,
        1,
        revision);

      Then(new ScanProcessingStorageBindingCreated(
        requestId,
        command.ActingActor.Clone(),
        clientId,
        clientId,
        binding,
        revision));
      Audit(
        requestId,
        ScanProcessingStorageBindingActions.Create,
        "accepted",
        null,
        command.ActingActor,
        clientId,
        bindingId,
        null,
        revision);
    }

    void When(ActivateScanProcessingStorageBinding command)
    {
      if(!TryValidateCommon(
        command.RequestId,
        command.ActingActor,
        command.ClientId,
        ScanProcessingStorageBindingActions.Activate,
        out var requestId,
        out var clientId))
      {
        return;
      }

      var bindingId = Normalize(command.BindingId);

      if(string.IsNullOrWhiteSpace(bindingId) || !_state.TryGetBinding(bindingId, out _))
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Activate,
          ScanProcessingStorageBindingIssueCodes.BindingNotFound,
          "The logical storage binding was not found for this workspace.",
          "bindingId",
          command.ActingActor,
          bindingId,
          command.ConfigurationGeneration);
        return;
      }

      if(command.ExpectedStorageRevision != _state.StorageRevision)
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Activate,
          ScanProcessingStorageBindingIssueCodes.StorageRevisionStale,
          "The workspace storage configuration changed after it was read. Refresh it and try again.",
          "expectedStorageRevision",
          command.ActingActor,
          bindingId,
          command.ConfigurationGeneration);
        return;
      }

      if(_state.ActiveBinding != null
        && string.Equals(_state.ActiveBinding.BindingId, bindingId, StringComparison.Ordinal)
        && _state.ActiveBinding.ConfigurationGeneration == command.ConfigurationGeneration)
      {
        Then(new ScanProcessingStorageBindingActivationUnchanged(
          requestId,
          command.ActingActor.Clone(),
          clientId,
          clientId,
          bindingId,
          command.ConfigurationGeneration,
          _state.StorageRevision));
        Audit(
          requestId,
          ScanProcessingStorageBindingActions.Activate,
          "unchanged",
          null,
          command.ActingActor,
          clientId,
          bindingId,
          command.ConfigurationGeneration,
          _state.StorageRevision);
        return;
      }

      var requiredGeneration = checked(_state.LatestGeneration(bindingId) + 1);

      if(command.ConfigurationGeneration != requiredGeneration)
      {
        Reject(
          requestId,
          ScanProcessingStorageBindingActions.Activate,
          ScanProcessingStorageBindingIssueCodes.ConfigurationGenerationStale,
          "The configuration generation must be the next generation for this logical binding.",
          "configurationGeneration",
          command.ActingActor,
          bindingId,
          command.ConfigurationGeneration);
        return;
      }

      var revision = NextRevision;
      var previous = _state.ActiveBinding;

      Then(new ScanProcessingStorageBindingActivated(
        requestId,
        command.ActingActor.Clone(),
        clientId,
        clientId,
        bindingId,
        command.ConfigurationGeneration,
        previous?.BindingId,
        previous?.ConfigurationGeneration,
        revision));
      Audit(
        requestId,
        ScanProcessingStorageBindingActions.Activate,
        "accepted",
        null,
        command.ActingActor,
        clientId,
        bindingId,
        command.ConfigurationGeneration,
        revision);
    }

    bool TryValidateCommon(
      string rawRequestId,
      ScanProcessingActorIdentity actor,
      Id commandClientId,
      string action,
      out string requestId,
      out string clientId)
    {
      requestId = Normalize(rawRequestId);
      clientId = commandClientId.ToString();

      if(string.IsNullOrWhiteSpace(requestId))
      {
        Reject(
          requestId,
          action,
          ScanProcessingStorageBindingIssueCodes.InvalidRequest,
          "A request ID is required.",
          "requestId",
          actor,
          null);
        return false;
      }

      if(actor?.IsValid != true)
      {
        Reject(
          requestId,
          action,
          ScanProcessingStorageBindingIssueCodes.AuthenticatedActorRequired,
          "A registered authenticated actor is required.",
          "actor",
          actor,
          null);
        return false;
      }

      if(!_state.ClientRecognized
        || !string.Equals(_state.ClientId, clientId, StringComparison.Ordinal)
        || !string.Equals(_state.WorkspaceId, clientId, StringComparison.Ordinal))
      {
        Reject(
          requestId,
          action,
          ScanProcessingStorageBindingIssueCodes.ClientNotFound,
          "The Version 1 client/workspace was not found.",
          "clientId",
          actor,
          null);
        return false;
      }

      return true;
    }

    void Reject(
      string requestId,
      string action,
      string code,
      string message,
      string field,
      ScanProcessingActorIdentity actor,
      string bindingId,
      long? configurationGeneration = null)
    {
      Then(new ScanProcessingStorageBindingRequestRejected(
        requestId,
        action,
        _state.ClientId,
        _state.WorkspaceId,
        code,
        message,
        field,
        _state.StorageRevision));
      Audit(
        requestId,
        action,
        "rejected",
        code,
        actor,
        _state.ClientId,
        bindingId,
        configurationGeneration,
        _state.StorageRevision);
    }

    void Audit(
      string requestId,
      string action,
      string outcome,
      string code,
      ScanProcessingActorIdentity actor,
      string clientId,
      string bindingId,
      long? configurationGeneration,
      long revision) =>
      Then(new ScanProcessingStorageBindingAuditRecorded(
        ScanProcessingStorageBindingAuditRecorded.NewAuditId().ToString(),
        requestId,
        action,
        outcome,
        code,
        actor?.Clone(),
        clientId,
        clientId,
        bindingId,
        configurationGeneration,
        revision));

    static bool TryNormalizeCapabilities(
      IEnumerable<ScanProcessingStorageCapability> rawCapabilities,
      out List<ScanProcessingStorageCapability> capabilities)
    {
      capabilities = new List<ScanProcessingStorageCapability>();
      var seen = new HashSet<string>(StringComparer.Ordinal);

      foreach(var raw in rawCapabilities ?? Enumerable.Empty<ScanProcessingStorageCapability>())
      {
        if(!ScanProcessingStorageCapabilities.TryNormalize(raw?.Kind, out var kind)
          || !seen.Add(kind)
          || !TryNormalizeLabel(raw.Label, out var label))
        {
          capabilities = null;
          return false;
        }

        capabilities.Add(new ScanProcessingStorageCapability(kind, label));
      }

      capabilities.Sort((left, right) => StringComparer.Ordinal.Compare(left.Kind, right.Kind));
      return seen.SetEquals(ScanProcessingStorageCapabilities.All);
    }

    static bool TryNormalizeLabel(string raw, out string label)
    {
      label = Normalize(raw);

      return !string.IsNullOrWhiteSpace(label)
        && label.Length <= 120
        && !label.Contains('\\')
        && !label.Contains('/')
        && !label.Contains(':');
    }

    long NextRevision => checked(_state.StorageRevision + 1);

    static string Normalize(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
