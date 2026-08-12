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
    DateTimeOffset AcceptedAt);

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
    DateTimeOffset? CompletedAt);

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
