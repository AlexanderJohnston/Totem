using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Outermind.Microfilm
{
  public enum RollScanState
  {
    Idle,
    Starting,
    Active,
    Finishing
  }

  public enum OperationAction
  {
    Scan,
    Process
  }

  public enum ProcessingIssueSeverity
  {
    Info,
    Warning,
    Blocker,
    Error
  }

  public static class RollResourceVersion
  {
    public static string FromRevision(long revision)
    {
      if(revision < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(revision), "A roll resource revision must be positive.");
      }

      return $"rv1-{revision.ToString("X16", CultureInfo.InvariantCulture)}";
    }
  }

  public class OperationRowContext
  {
    public string ClientId { get; set; }
    public string RowId { get; set; }
    public string Origin { get; set; }
    public string RollId { get; set; }
    public string RollName { get; set; }
    public bool IsScanning { get; set; }
    public RollScanState ScanState { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ActiveScanId { get; set; }

    public string ResourceVersion { get; set; }
    public List<OperationAction> EligibleActions { get; set; } = new();
  }

  public static class OperationRowContextFactory
  {
    public static OperationRowContext Create(
      KnownClient client,
      KnownRoll roll,
      MicrofilmTableRow row,
      RollScanState scanState,
      string activeScanId,
      string resourceVersion) =>
      new()
      {
        ClientId = client.ClientId.ToString(),
        RowId = row.Id,
        Origin = row.Origin,
        RollId = roll.RollId.ToString(),
        RollName = roll.RollName,
        IsScanning = scanState != RollScanState.Idle,
        ScanState = scanState,
        ActiveScanId = activeScanId,
        ResourceVersion = resourceVersion,
        EligibleActions = EligibleActions(scanState)
      };

    static List<OperationAction> EligibleActions(RollScanState scanState)
    {
      var actions = new List<OperationAction> { OperationAction.Scan };

      if(scanState == RollScanState.Idle)
      {
        actions.Add(OperationAction.Process);
      }

      return actions;
    }
  }

  public class ProcessingIssue
  {
    public string Code { get; set; }
    public ProcessingIssueSeverity Severity { get; set; }
    public string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Field { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ResourceRefId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RequiresAcknowledgement { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Retryable { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CorrelationId { get; set; }
  }

  public class ProcessingIssueEnvelope
  {
    public List<ProcessingIssue> Issues { get; set; } = new();

    public ProcessingIssueEnvelope()
    {
    }

    public ProcessingIssueEnvelope(ProcessingIssue issue)
    {
      Issues.Add(issue);
    }
  }

  public static class OperationContextApiErrors
  {
    public static ProcessingIssueEnvelope RollNotFound(string rollId, string correlationId = null) =>
      One("ROLL_NOT_FOUND", $"Roll '{rollId}' was not found.", "rollId", correlationId);

    public static ProcessingIssueEnvelope RowNotFound(string rollId, string rowId, string correlationId = null) =>
      One("ROW_NOT_FOUND", $"Row '{rowId}' was not found for roll '{rollId}'.", "rowId", correlationId);

    public static ProcessingIssueEnvelope RollMappingInvalid(string rollId, string correlationId = null) =>
      One("ROLL_MAPPING_INVALID", $"Roll '{rollId}' does not have a complete canonical client and box mapping.", "rollId", correlationId);

    public static ProcessingIssueEnvelope RowContextInvalid(string rollId, string rowId, string correlationId = null) =>
      One("ROW_CONTEXT_INVALID", $"Row '{rowId}' does not have a valid canonical context for roll '{rollId}'.", "rowId", correlationId);

    public static ProcessingIssueEnvelope ResourceVersionStale(string correlationId = null) =>
      One("RESOURCE_VERSION_STALE", "The roll changed after this operation context was read. Refresh the context and try again.", "resourceVersion", correlationId);

    public static ProcessingIssueEnvelope ActionIneligible(OperationAction action, string correlationId = null) =>
      One("ACTION_INELIGIBLE", $"The {action.ToString().ToLowerInvariant()} action is not eligible in the current roll state.", "action", correlationId);

    static ProcessingIssueEnvelope One(string code, string message, string field, string correlationId) =>
      new(new ProcessingIssue
      {
        Code = code,
        Severity = ProcessingIssueSeverity.Error,
        Message = message,
        Field = field,
        CorrelationId = correlationId
      });
  }
}
