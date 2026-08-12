using System;
using System.Collections.Generic;
using Outermind.Microfilm;

namespace Quantum.Web.ScanProcessing
{
  public static class ScanProcessingDemoIssueCodes
  {
    public const string Disabled = "DEMO_DISABLED";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string RegisteredActorRequired = "REGISTERED_ACTOR_REQUIRED";
    public const string ResourceVersionStale = "RESOURCE_VERSION_STALE";
    public const string ResourceReferenceInvalid = "RESOURCE_REFERENCE_INVALID";
    public const string ActionIneligible = "ACTION_INELIGIBLE";
    public const string ScanNotFound = "SCAN_NOT_FOUND";
    public const string PlanNotFound = "PLAN_NOT_FOUND";
    public const string PlanStale = "PLAN_STALE";
    public const string PlanExpired = "PLAN_EXPIRED";
    public const string JobNotFound = "JOB_NOT_FOUND";
    public const string JobTerminal = "JOB_TERMINAL";
    public const string IdempotencyKeyRequired = "IDEMPOTENCY_KEY_REQUIRED";
    public const string IdempotencyKeyMismatch = "IDEMPOTENCY_KEY_MISMATCH";
    public const string PhysicalConfigurationRequired = "DEMO_PHYSICAL_CONFIGURATION_REQUIRED";
    public const string ScanFolderCollision = "SCAN_FOLDER_COLLISION";
    public const string ScanFolderUnavailable = "SCAN_FOLDER_UNAVAILABLE";
    public const string IdfNotFound = "IDF_NOT_FOUND";
    public const string IdfAmbiguous = "IDF_AMBIGUOUS";
    public const string IdfInvalid = "IDF_INVALID";
    public const string QpfNotFound = "QPF_NOT_FOUND";
    public const string QpfAmbiguous = "QPF_AMBIGUOUS";
    public const string QpfInvalid = "QPF_INVALID";
    public const string QpfMutationFailed = "QPF_MUTATION_FAILED";
    public const string DurableImageCountFailed = "DURABLE_IMAGE_COUNT_FAILED";
  }

  public sealed class ScanProcessingDemoDiscoveryResponse
  {
    public bool Demo { get; set; } = true;
    public OperationRowContext Context { get; set; }
    public ScanProcessingDemoScanConfiguration Scan { get; set; }
    public ScanProcessingDemoProcessingConfiguration Processing { get; set; }
  }

  public sealed class ScanProcessingDemoScanConfiguration
  {
    public ScanProcessingDemoResourceReference Parent { get; set; }
    public string SuggestedFolderName { get; set; }
    public string Notes { get; set; }
  }

  public sealed class ScanProcessingDemoProcessingConfiguration
  {
    public List<string> AvailablePreviews { get; set; } = new();
    public Dictionary<string, string> QpfSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ScanProcessingDemoResourceReference Qpf { get; set; }
    public ScanProcessingDemoResourceReference GrayscaleFrames { get; set; }
    public ScanProcessingDemoResourceReference BitonalFrames { get; set; }
  }

  public sealed record ScanProcessingDemoResourceReference(
    string Id,
    string Kind,
    string Label,
    string InformationalFullPath);

  public sealed class StartScanDemoRequest
  {
    public string ExpectedResourceVersion { get; set; }
    public string ParentResourceRefId { get; set; }
    public string FolderName { get; set; }
    public string Notes { get; set; }
    public string IdempotencyKey { get; set; }
  }

  public sealed class FinishScanDemoRequest
  {
    public string ExpectedResourceVersion { get; set; }
    public string Notes { get; set; }
    public string IdempotencyKey { get; set; }
  }

  public sealed record ScanProcessingDemoScanResponse(
    OperationRowContext Context,
    ScanProcessingDemoScan Scan);

  public sealed record ScanProcessingDemoScan(
    string ScanId,
    string Status,
    string FolderName,
    string Notes,
    DateTimeOffset AcceptedAt,
    string InformationalFullPath);

  public sealed record FinishScanProcessingDemoResponse(
    OperationRowContext Context,
    ScanProcessingDemoScan Scan,
    int ImageCount,
    string IdfFileName);

  public sealed class PreviewQpfSettingsDemoRequest
  {
    public string ExpectedResourceVersion { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
  }

  public sealed class PreviewFramesPathsDemoRequest
  {
    public string ExpectedResourceVersion { get; set; }
    public string GrayscaleResourceRefId { get; set; }
    public string BitonalResourceRefId { get; set; }
  }

  public sealed record ScanProcessingDemoPreviewResponse(
    string PlanId,
    string PlanVersion,
    string Purpose,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string> Effects,
    IReadOnlyList<ProcessingIssue> Issues);

  public sealed class ApplyScanProcessingDemoPlanRequest
  {
    public string ExpectedPlanVersion { get; set; }
    public List<string> AcknowledgedIssueCodes { get; set; } = new();
    public string IdempotencyKey { get; set; }
  }

  public enum ScanProcessingDemoJobStatus
  {
    Queued,
    Running,
    Completed,
    Cancelled
  }

  public sealed record ScanProcessingDemoJobResponse(
    string JobId,
    string PlanId,
    string Purpose,
    ScanProcessingDemoJobStatus Status,
    int PercentComplete,
    DateTimeOffset AcceptedAt,
    DateTimeOffset? CompletedAt,
    ScanProcessingDemoQpfApplyResult QpfApply);

  public sealed record ScanProcessingDemoQpfApplyResult(
    string QpfFileName,
    string BackupFileName,
    int UpdatedDetectionSettings);

  public sealed record ScanProcessingDemoPlanExecution(
    string Purpose,
    string RollId,
    string RowId,
    string FolderPath,
    IReadOnlyDictionary<string, string> Settings);

  public sealed record ScanProcessingDemoIdfInspection(
    int ImageCount,
    string IdfFileName);

  public sealed record ScanProcessingDemoResetResponse(
    bool Demo,
    DateTimeOffset ResetAt);

  public sealed class ScanProcessingDemoResult<T>
  {
    public bool Succeeded { get; private set; }
    public int StatusCode { get; private set; }
    public T Value { get; private set; }
    public ProcessingIssue Issue { get; private set; }

    public static ScanProcessingDemoResult<T> Success(T value, int statusCode = 200) =>
      new() { Succeeded = true, StatusCode = statusCode, Value = value };

    public static ScanProcessingDemoResult<T> Failure(
      string code,
      string message,
      string field,
      int statusCode = 400) =>
      new()
      {
        StatusCode = statusCode,
        Issue = new ProcessingIssue
        {
          Code = code,
          Severity = ProcessingIssueSeverity.Error,
          Message = message,
          Field = field
        }
      };
  }
}
