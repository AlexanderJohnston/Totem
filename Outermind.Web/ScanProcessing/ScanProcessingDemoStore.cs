using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Outermind.Microfilm;

namespace Quantum.Web.ScanProcessing
{
  /// <summary>
  /// Disposable demo-only state. It deliberately has no database, filesystem, or production-storage dependency.
  /// </summary>
  public sealed class ScanProcessingDemoStore
  {
    readonly object _gate = new();
    readonly ScanProcessingDemoOptions _options;
    readonly IScanProcessingDemoClock _clock;
    readonly Dictionary<string, RowState> _rows = new(StringComparer.Ordinal);
    readonly Dictionary<string, PlanState> _plans = new(StringComparer.Ordinal);
    readonly Dictionary<string, JobState> _jobs = new(StringComparer.Ordinal);
    readonly Dictionary<string, IdempotentValue<ScanProcessingDemoScanResponse>> _scanRequests = new(StringComparer.Ordinal);
    readonly Dictionary<string, IdempotentValue<FinishScanProcessingDemoResponse>> _physicalFinishRequests = new(StringComparer.Ordinal);
    readonly Dictionary<string, IdempotentValue<ScanProcessingDemoJobResponse>> _applyRequests = new(StringComparer.Ordinal);

    public ScanProcessingDemoStore(
      IOptions<ScanProcessingDemoOptions> options,
      IScanProcessingDemoClock clock)
    {
      _options = options.Value;
      _clock = clock;
    }

    public ScanProcessingDemoDiscoveryResponse Discover(string actorId, string rollId, string rowId)
    {
      lock(_gate)
      {
        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);

        return new ScanProcessingDemoDiscoveryResponse
        {
          Context = Context(row),
          Scan = new ScanProcessingDemoScanConfiguration
          {
            Parent = Reference(actorId, row, "scan-parent", "Demo Scan parent"),
            SuggestedFolderName = $"{SafeLabel(rollId)}-{SafeLabel(rowId)}",
            Notes = row.LastNotes
          },
          Processing = new ScanProcessingDemoProcessingConfiguration
          {
            AvailablePreviews = new List<string> { "qpf-settings", "frames-paths" },
            QpfSettings = new Dictionary<string, string>(row.QpfSettings, StringComparer.OrdinalIgnoreCase),
            Qpf = Reference(actorId, row, "qpf", "Demo QPF"),
            GrayscaleFrames = Reference(actorId, row, "frames-grayscale", "Demo grayscale Frames"),
            BitonalFrames = Reference(actorId, row, "frames-bitonal", "Demo bitonal Frames")
          }
        };
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoScanResponse> Start(
      string actorId,
      string rollId,
      string rowId,
      StartScanDemoRequest request)
    {
      lock(_gate)
      {
        if(request == null)
        {
          return Failure<ScanProcessingDemoScanResponse>("A request body is required.", "request");
        }

        if(string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.IdempotencyKeyRequired,
            "An idempotency key is required.",
            "idempotencyKey");
        }

        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);
        var fingerprint = Join(request.ExpectedResourceVersion, request.ParentResourceRefId, request.FolderName, request.Notes);
        var idempotencyKey = Join("scan-start", actorId, request.IdempotencyKey);

        if(_scanRequests.TryGetValue(idempotencyKey, out var previous))
        {
          return previous.Fingerprint == fingerprint
            ? ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(previous.Value, 202)
            : IdempotencyMismatch<ScanProcessingDemoScanResponse>();
        }

        if(request.ExpectedResourceVersion != ResourceVersion(row))
        {
          return Stale<ScanProcessingDemoScanResponse>();
        }

        if(row.ScanState != RollScanState.Idle)
        {
          return Ineligible<ScanProcessingDemoScanResponse>("A scan is already starting, active, or finishing.");
        }

        if(request.ParentResourceRefId != Reference(actorId, row, "scan-parent", "Demo Scan parent").Id)
        {
          return InvalidReference<ScanProcessingDemoScanResponse>("parentResourceRefId");
        }

        if(string.IsNullOrWhiteSpace(request.FolderName))
        {
          return Failure<ScanProcessingDemoScanResponse>("A folder name is required.", "folderName");
        }

        if(request.FolderName.Length > 120
          || request.FolderName.IndexOfAny(new[] { '\\', '/', ':' }) >= 0
          || request.FolderName.Contains("..", StringComparison.Ordinal))
        {
          return Failure<ScanProcessingDemoScanResponse>(
            "folderName must be a single name, not a local path, UNC path, URI, or traversal.",
            "folderName");
        }

        var now = _clock.UtcNow;
        row.Revision++;
        row.ScanState = RollScanState.Starting;
        row.LastNotes = request.Notes?.Trim();
        row.Scan = new ScanProcessingDemoScan(
          $"demo-scan-{Guid.NewGuid():N}",
          "starting",
          request.FolderName.Trim(),
          row.LastNotes,
          now,
          null);
        row.ScanActorId = actorId;
        row.ScanTransitionAt = now + TransitionDelay;

        var response = new ScanProcessingDemoScanResponse(Context(row), row.Scan);
        _scanRequests[idempotencyKey] = new IdempotentValue<ScanProcessingDemoScanResponse>(fingerprint, response);
        return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(response, 202);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoScanResponse> AttachScanFolder(
      string actorId,
      string rollId,
      string rowId,
      string scanId,
      string informationalFullPath)
    {
      lock(_gate)
      {
        var row = GetOrCreateRow(rollId, rowId);
        if(row.Scan == null || row.Scan.ScanId != scanId || row.ScanActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.ScanNotFound,
            "The demo scan was not found while attaching its folder.",
            "scanId",
            404);
        }

        row.Scan = row.Scan with { InformationalFullPath = informationalFullPath };
        row.LatestFolderPath = informationalFullPath;
        row.LatestFolderActorId = actorId;

        foreach(var key in _scanRequests.Keys.ToList())
        {
          var request = _scanRequests[key];
          if(request.Value.Scan.ScanId == scanId)
          {
            _scanRequests[key] = request with
            {
              Value = new ScanProcessingDemoScanResponse(request.Value.Context, row.Scan)
            };
          }
        }

        return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(
          new ScanProcessingDemoScanResponse(Context(row), row.Scan),
          202);
      }
    }

    public void FailStart(string actorId, string rollId, string rowId, string scanId)
    {
      lock(_gate)
      {
        var row = GetOrCreateRow(rollId, rowId);
        if(row.Scan?.ScanId != scanId || row.ScanActorId != actorId || row.ScanState != RollScanState.Starting)
        {
          return;
        }

        row.Revision++;
        row.ScanState = RollScanState.Idle;
        row.Scan = null;
        row.ScanActorId = null;
        row.ScanTransitionAt = null;
        row.LatestFolderPath = null;
        row.LatestFolderActorId = null;

        foreach(var key in _scanRequests.Keys
          .Where(key => _scanRequests[key].Value.Scan.ScanId == scanId)
          .ToList())
        {
          _scanRequests.Remove(key);
        }
      }
    }

    public bool TryGetActiveScanFolder(
      string actorId,
      string rollId,
      string rowId,
      string scanId,
      out string folderPath)
    {
      lock(_gate)
      {
        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);
        folderPath = row.LatestFolderPath;
        return row.Scan?.ScanId == scanId
          && row.ScanActorId == actorId
          && row.ScanState == RollScanState.Active
          && !string.IsNullOrWhiteSpace(folderPath);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoScanResponse> Finish(
      string actorId,
      string rollId,
      string rowId,
      string scanId,
      FinishScanDemoRequest request)
    {
      lock(_gate)
      {
        if(request == null)
        {
          return Failure<ScanProcessingDemoScanResponse>("A request body is required.", "request");
        }

        if(string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.IdempotencyKeyRequired,
            "An idempotency key is required.",
            "idempotencyKey");
        }

        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);
        var fingerprint = Join(request.ExpectedResourceVersion, scanId, request.Notes);
        var idempotencyKey = Join("scan-finish", actorId, request.IdempotencyKey);

        if(_scanRequests.TryGetValue(idempotencyKey, out var previous))
        {
          return previous.Fingerprint == fingerprint
            ? ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(previous.Value, 202)
            : IdempotencyMismatch<ScanProcessingDemoScanResponse>();
        }

        if(request.ExpectedResourceVersion != ResourceVersion(row))
        {
          return Stale<ScanProcessingDemoScanResponse>();
        }

        if(row.Scan == null || row.Scan.ScanId != scanId || row.ScanActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.ScanNotFound,
            "The active demo scan was not found for this actor and row.",
            "scanId",
            404);
        }

        if(row.ScanState != RollScanState.Active)
        {
          return Ineligible<ScanProcessingDemoScanResponse>("Only an active scan can be finished.");
        }

        var now = _clock.UtcNow;
        row.Revision++;
        row.ScanState = RollScanState.Finishing;
        row.LastNotes = request.Notes?.Trim() ?? row.LastNotes;
        row.Scan = row.Scan with { Status = "finishing", Notes = row.LastNotes };
        row.ScanTransitionAt = now + TransitionDelay;

        var response = new ScanProcessingDemoScanResponse(Context(row), row.Scan);
        _scanRequests[idempotencyKey] = new IdempotentValue<ScanProcessingDemoScanResponse>(fingerprint, response);
        return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(response, 202);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoScanResponse> ValidateFinish(
      string actorId,
      string rollId,
      string rowId,
      string scanId,
      FinishScanDemoRequest request)
    {
      lock(_gate)
      {
        if(request == null)
        {
          return Failure<ScanProcessingDemoScanResponse>("A request body is required.", "request");
        }

        if(string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.IdempotencyKeyRequired,
            "An idempotency key is required.",
            "idempotencyKey");
        }

        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);
        var fingerprint = Join(request.ExpectedResourceVersion, scanId, request.Notes);
        var idempotencyKey = Join("scan-finish", actorId, request.IdempotencyKey);
        if(_scanRequests.TryGetValue(idempotencyKey, out var previous))
        {
          return previous.Fingerprint == fingerprint
            ? ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(previous.Value, 202)
            : IdempotencyMismatch<ScanProcessingDemoScanResponse>();
        }

        if(request.ExpectedResourceVersion != ResourceVersion(row))
        {
          return Stale<ScanProcessingDemoScanResponse>();
        }

        if(row.Scan == null || row.Scan.ScanId != scanId || row.ScanActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Failure(
            ScanProcessingDemoIssueCodes.ScanNotFound,
            "The active demo scan was not found for this actor and row.",
            "scanId",
            404);
        }

        if(row.ScanState != RollScanState.Active)
        {
          return Ineligible<ScanProcessingDemoScanResponse>("Only an active scan can be finished.");
        }

        return ScanProcessingDemoResult<ScanProcessingDemoScanResponse>.Success(
          new ScanProcessingDemoScanResponse(Context(row), row.Scan));
      }
    }

    public bool TryReplayPhysicalFinish(
      string actorId,
      string scanId,
      FinishScanDemoRequest request,
      out FinishScanProcessingDemoResponse response)
    {
      lock(_gate)
      {
        response = null;
        if(request == null || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
          return false;
        }

        var key = Join("physical-scan-finish", actorId, request.IdempotencyKey);
        var fingerprint = Join(request.ExpectedResourceVersion, scanId, request.Notes);
        if(!_physicalFinishRequests.TryGetValue(key, out var previous) || previous.Fingerprint != fingerprint)
        {
          return false;
        }

        response = previous.Value;
        return true;
      }
    }

    public void RememberPhysicalFinish(
      string actorId,
      string scanId,
      FinishScanDemoRequest request,
      FinishScanProcessingDemoResponse response)
    {
      lock(_gate)
      {
        var key = Join("physical-scan-finish", actorId, request.IdempotencyKey);
        var fingerprint = Join(request.ExpectedResourceVersion, scanId, request.Notes);
        _physicalFinishRequests[key] = new IdempotentValue<FinishScanProcessingDemoResponse>(fingerprint, response);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoPreviewResponse> PreviewQpf(
      string actorId,
      string rollId,
      string rowId,
      PreviewQpfSettingsDemoRequest request)
    {
      var allowedSettings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        "contrast",
        "brightness",
        "gamma",
        "sharpen",
        "auto-crop",
        "auto-deskew",
        "rotate",
        "flip",
        "save-grayscale",
        "save-bitonal",
        "grayscale-format",
        "bitonal-format",
        "crop-border",
        "crop-threshold",
        "deskew-quality"
      };
      var invalidSetting = request?.Settings?.Keys.FirstOrDefault(key => !allowedSettings.Contains(key));
      if(invalidSetting != null)
      {
        return Failure<ScanProcessingDemoPreviewResponse>(
          $"'{invalidSetting}' is not a supported Version 1 demo QPF setting.",
          $"settings.{invalidSetting}");
      }

      return Preview(
        actorId,
        rollId,
        rowId,
        request?.ExpectedResourceVersion,
        "qpf-settings",
        request == null
          ? null
          : new Dictionary<string, string>(request.Settings ?? new(), StringComparer.OrdinalIgnoreCase),
        request == null ? null : SettingsFingerprint(request.Settings),
        new[] { "Update the demo QPF settings shown for this row." });
    }

    public ScanProcessingDemoResult<ScanProcessingDemoPreviewResponse> PreviewFrames(
      string actorId,
      string rollId,
      string rowId,
      PreviewFramesPathsDemoRequest request)
    {
      lock(_gate)
      {
        if(request == null)
        {
          return Failure<ScanProcessingDemoPreviewResponse>("A request body is required.", "request");
        }

        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);

        if(request.GrayscaleResourceRefId != Reference(actorId, row, "frames-grayscale", "Demo grayscale Frames").Id)
        {
          return InvalidReference<ScanProcessingDemoPreviewResponse>("grayscaleResourceRefId");
        }

        if(request.BitonalResourceRefId != Reference(actorId, row, "frames-bitonal", "Demo bitonal Frames").Id)
        {
          return InvalidReference<ScanProcessingDemoPreviewResponse>("bitonalResourceRefId");
        }

        return PreviewLocked(
          actorId,
          row,
          request.ExpectedResourceVersion,
          "frames-paths",
          null,
          Join(request.GrayscaleResourceRefId, request.BitonalResourceRefId),
          new[]
          {
            "Use the configured demo grayscale Frames destination.",
            "Use the configured demo bitonal Frames destination."
          });
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoJobResponse> Apply(
      string actorId,
      string planId,
      ApplyScanProcessingDemoPlanRequest request,
      Func<ScanProcessingDemoPlanExecution, ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>> physicalApply = null)
    {
      lock(_gate)
      {
        if(request == null)
        {
          return Failure<ScanProcessingDemoJobResponse>("A request body is required.", "request");
        }

        if(string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.IdempotencyKeyRequired,
            "An idempotency key is required.",
            "idempotencyKey");
        }

        var fingerprint = Join(planId, request.ExpectedPlanVersion, string.Join(",", request.AcknowledgedIssueCodes ?? new()));
        var idempotencyKey = Join("plan-apply", actorId, request.IdempotencyKey);

        if(_applyRequests.TryGetValue(idempotencyKey, out var previous))
        {
          return previous.Fingerprint == fingerprint
            ? ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Success(previous.Value, 202)
            : IdempotencyMismatch<ScanProcessingDemoJobResponse>();
        }

        if(!_plans.TryGetValue(planId ?? string.Empty, out var plan) || plan.ActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.PlanNotFound,
            "The demo plan was not found for this actor.",
            "planId",
            404);
        }

        if(request.ExpectedPlanVersion != plan.PlanVersion)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.PlanStale,
            "The demo plan version is stale.",
            "expectedPlanVersion",
            409);
        }

        if(_clock.UtcNow >= plan.ExpiresAt)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.PlanExpired,
            "The demo preview plan expired. Preview again.",
            "planId",
            409);
        }

        var row = GetOrCreateRow(plan.RollId, plan.RowId);
        Advance(row);
        if(ResourceVersion(row) != plan.ResourceVersion || row.ScanState != RollScanState.Idle)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.PlanStale,
            "The row changed after preview. Refresh and preview again.",
            "planId",
            409);
        }

        ScanProcessingDemoQpfApplyResult qpfApply = null;
        if(physicalApply != null && plan.Purpose == "qpf-settings")
        {
          if(row.LatestFolderActorId != actorId || string.IsNullOrWhiteSpace(row.LatestFolderPath))
          {
            return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
              ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
              "No completed physical demo scan folder is available for this actor and row.",
              "scanFolder",
              409);
          }

          var physical = physicalApply(new ScanProcessingDemoPlanExecution(
            plan.Purpose,
            plan.RollId,
            plan.RowId,
            row.LatestFolderPath,
            plan.QpfSettings));
          if(!physical.Succeeded)
          {
            return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
              physical.Issue.Code,
              physical.Issue.Message,
              physical.Issue.Field,
              physical.StatusCode);
          }

          qpfApply = physical.Value;
        }

        var now = _clock.UtcNow;
        var job = new JobState
        {
          JobId = $"demo-job-{Guid.NewGuid():N}",
          Plan = plan,
          Status = qpfApply == null ? ScanProcessingDemoJobStatus.Queued : ScanProcessingDemoJobStatus.Completed,
          AcceptedAt = now,
          NextTransitionAt = now + TransitionDelay,
          CompletedAt = qpfApply == null ? null : now,
          QpfApply = qpfApply
        };
        _jobs[job.JobId] = job;

        if(qpfApply != null)
        {
          ApplyCompletedSettings(job);
        }

        var response = JobResponse(job);
        _applyRequests[idempotencyKey] = new IdempotentValue<ScanProcessingDemoJobResponse>(fingerprint, response);
        return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Success(response, 202);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoJobResponse> GetJob(string actorId, string jobId)
    {
      lock(_gate)
      {
        if(!_jobs.TryGetValue(jobId ?? string.Empty, out var job) || job.Plan.ActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.JobNotFound,
            "The demo job was not found for this actor.",
            "jobId",
            404);
        }

        Advance(job);
        return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Success(JobResponse(job));
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoJobResponse> CancelJob(string actorId, string jobId)
    {
      lock(_gate)
      {
        if(!_jobs.TryGetValue(jobId ?? string.Empty, out var job) || job.Plan.ActorId != actorId)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.JobNotFound,
            "The demo job was not found for this actor.",
            "jobId",
            404);
        }

        Advance(job);
        if(job.Status is ScanProcessingDemoJobStatus.Completed or ScanProcessingDemoJobStatus.Cancelled)
        {
          return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Failure(
            ScanProcessingDemoIssueCodes.JobTerminal,
            "The demo job is already terminal.",
            "jobId",
            409);
        }

        job.Status = ScanProcessingDemoJobStatus.Cancelled;
        job.CompletedAt = _clock.UtcNow;
        return ScanProcessingDemoResult<ScanProcessingDemoJobResponse>.Success(JobResponse(job));
      }
    }

    public ScanProcessingDemoResetResponse Reset()
    {
      lock(_gate)
      {
        _rows.Clear();
        _plans.Clear();
        _jobs.Clear();
        _scanRequests.Clear();
        _physicalFinishRequests.Clear();
        _applyRequests.Clear();
        return new ScanProcessingDemoResetResponse(true, _clock.UtcNow);
      }
    }

    ScanProcessingDemoResult<ScanProcessingDemoPreviewResponse> Preview(
      string actorId,
      string rollId,
      string rowId,
      string expectedResourceVersion,
      string purpose,
      Dictionary<string, string> settings,
      string fingerprint,
      IReadOnlyList<string> effects)
    {
      lock(_gate)
      {
        if(expectedResourceVersion == null)
        {
          return Failure<ScanProcessingDemoPreviewResponse>("A request body and resource version are required.", "request");
        }

        var row = GetOrCreateRow(rollId, rowId);
        Advance(row);
        return PreviewLocked(actorId, row, expectedResourceVersion, purpose, settings, fingerprint, effects);
      }
    }

    ScanProcessingDemoResult<ScanProcessingDemoPreviewResponse> PreviewLocked(
      string actorId,
      RowState row,
      string expectedResourceVersion,
      string purpose,
      Dictionary<string, string> settings,
      string fingerprint,
      IReadOnlyList<string> effects)
    {
      if(expectedResourceVersion != ResourceVersion(row))
      {
        return Stale<ScanProcessingDemoPreviewResponse>();
      }

      if(row.ScanState != RollScanState.Idle)
      {
        return Ineligible<ScanProcessingDemoPreviewResponse>("Processing previews are available only while the row is idle.");
      }

      var now = _clock.UtcNow;
      var plan = new PlanState
      {
        PlanId = $"demo-plan-{Guid.NewGuid():N}",
        PlanVersion = $"pv1-{Opaque(Join(actorId, row.RollId, row.RowId, purpose, fingerprint, now.ToUnixTimeMilliseconds().ToString()))}",
        ActorId = actorId,
        RollId = row.RollId,
        RowId = row.RowId,
        Purpose = purpose,
        ResourceVersion = ResourceVersion(row),
        ExpiresAt = now.AddMinutes(Math.Max(1, _options.PlanLifetimeMinutes)),
        QpfSettings = settings
      };
      _plans[plan.PlanId] = plan;

      return ScanProcessingDemoResult<ScanProcessingDemoPreviewResponse>.Success(
        new ScanProcessingDemoPreviewResponse(
          plan.PlanId,
          plan.PlanVersion,
          plan.Purpose,
          plan.ExpiresAt,
          effects,
          Array.Empty<ProcessingIssue>()));
    }

    void Advance(RowState row)
    {
      if(row.ScanTransitionAt == null || _clock.UtcNow < row.ScanTransitionAt)
      {
        return;
      }

      row.Revision++;
      row.ScanTransitionAt = null;

      if(row.ScanState == RollScanState.Starting)
      {
        row.ScanState = RollScanState.Active;
        row.Scan = row.Scan with { Status = "active" };
      }
      else if(row.ScanState == RollScanState.Finishing)
      {
        row.ScanState = RollScanState.Idle;
        row.Scan = null;
        row.ScanActorId = null;
      }
    }

    void Advance(JobState job)
    {
      var now = _clock.UtcNow;
      if(now < job.NextTransitionAt || job.Status is ScanProcessingDemoJobStatus.Completed or ScanProcessingDemoJobStatus.Cancelled)
      {
        return;
      }

      if(job.Status == ScanProcessingDemoJobStatus.Queued)
      {
        job.Status = ScanProcessingDemoJobStatus.Running;
        job.NextTransitionAt = now + TransitionDelay;
        return;
      }

      job.Status = ScanProcessingDemoJobStatus.Completed;
      job.CompletedAt = now;
      ApplyCompletedSettings(job);
    }

    void ApplyCompletedSettings(JobState job)
    {
      if(job.Plan.Purpose != "qpf-settings" || job.Plan.QpfSettings == null)
      {
        return;
      }

      var row = GetOrCreateRow(job.Plan.RollId, job.Plan.RowId);
      row.QpfSettings = new Dictionary<string, string>(job.Plan.QpfSettings, StringComparer.OrdinalIgnoreCase);
      row.Revision++;
    }

    RowState GetOrCreateRow(string rollId, string rowId)
    {
      var key = Join(rollId?.Trim(), rowId?.Trim());
      if(_rows.TryGetValue(key, out var row))
      {
        return row;
      }

      row = new RowState
      {
        RollId = rollId?.Trim() ?? string.Empty,
        RowId = rowId?.Trim() ?? string.Empty,
        Revision = 1,
        QpfSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
          ["contrast"] = "64",
          ["brightness"] = "0",
          ["gamma"] = "0",
          ["sharpen"] = "0",
          ["auto-crop"] = "false",
          ["auto-deskew"] = "false",
          ["rotate"] = "0",
          ["flip"] = "0",
          ["save-grayscale"] = "true",
          ["save-bitonal"] = "false"
        }
      };
      _rows[key] = row;
      return row;
    }

    OperationRowContext Context(RowState row) =>
      new()
      {
        ClientId = "demo-client",
        RowId = row.RowId,
        Origin = "regular",
        RollId = row.RollId,
        RollName = $"Demo roll {row.RollId}",
        IsScanning = row.ScanState != RollScanState.Idle,
        ScanState = row.ScanState,
        ActiveScanId = row.Scan?.ScanId,
        ResourceVersion = ResourceVersion(row),
        EligibleActions = row.ScanState == RollScanState.Idle
          ? new List<OperationAction> { OperationAction.Scan, OperationAction.Process }
          : new List<OperationAction> { OperationAction.Scan }
      };

    ScanProcessingDemoResourceReference Reference(string actorId, RowState row, string kind, string label) =>
      new(
        $"demo-ref-{Opaque(Join(actorId, row.RollId, row.RowId, kind))}",
        kind,
        label,
        $"demo://configured-storage/{Opaque(Join(row.RollId, row.RowId))}/{kind}");

    ScanProcessingDemoJobResponse JobResponse(JobState job) =>
      new(
        job.JobId,
        job.Plan.PlanId,
        job.Plan.Purpose,
        job.Status,
        job.Status switch
        {
          ScanProcessingDemoJobStatus.Queued => 0,
          ScanProcessingDemoJobStatus.Running => 50,
          _ => 100
        },
        job.AcceptedAt,
        job.CompletedAt,
        job.QpfApply);

    TimeSpan TransitionDelay => TimeSpan.FromMilliseconds(Math.Max(1, _options.TransitionDelayMilliseconds));

    static string ResourceVersion(RowState row) => RollResourceVersion.FromRevision(row.Revision);

    static string SettingsFingerprint(Dictionary<string, string> settings) =>
      settings == null
        ? string.Empty
        : string.Join("|", settings.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));

    static string Opaque(string value)
    {
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
      return Convert.ToHexString(hash).Substring(0, 24).ToLowerInvariant();
    }

    static string SafeLabel(string value) =>
      string.IsNullOrWhiteSpace(value)
        ? "unknown"
        : new string(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').Take(40).ToArray());

    static string Join(params string[] values) => string.Join("\u001f", values.Select(value => value ?? string.Empty));

    static ScanProcessingDemoResult<T> Failure<T>(string message, string field) =>
      ScanProcessingDemoResult<T>.Failure(ScanProcessingDemoIssueCodes.InvalidRequest, message, field);

    static ScanProcessingDemoResult<T> Stale<T>() =>
      ScanProcessingDemoResult<T>.Failure(
        ScanProcessingDemoIssueCodes.ResourceVersionStale,
        "The demo row changed. Refresh discovery and try again.",
        "expectedResourceVersion",
        409);

    static ScanProcessingDemoResult<T> Ineligible<T>(string message) =>
      ScanProcessingDemoResult<T>.Failure(
        ScanProcessingDemoIssueCodes.ActionIneligible,
        message,
        "action",
        409);

    static ScanProcessingDemoResult<T> InvalidReference<T>(string field) =>
      ScanProcessingDemoResult<T>.Failure(
        ScanProcessingDemoIssueCodes.ResourceReferenceInvalid,
        "The opaque demo resource reference is not valid for this actor, row, and purpose.",
        field,
        403);

    static ScanProcessingDemoResult<T> IdempotencyMismatch<T>() =>
      ScanProcessingDemoResult<T>.Failure(
        ScanProcessingDemoIssueCodes.IdempotencyKeyMismatch,
        "The idempotency key was already used with different input.",
        "idempotencyKey",
        409);

    sealed class RowState
    {
      public string RollId { get; set; }
      public string RowId { get; set; }
      public long Revision { get; set; }
      public RollScanState ScanState { get; set; }
      public ScanProcessingDemoScan Scan { get; set; }
      public string ScanActorId { get; set; }
      public DateTimeOffset? ScanTransitionAt { get; set; }
      public string LastNotes { get; set; }
      public Dictionary<string, string> QpfSettings { get; set; }
      public string LatestFolderPath { get; set; }
      public string LatestFolderActorId { get; set; }
    }

    sealed class PlanState
    {
      public string PlanId { get; set; }
      public string PlanVersion { get; set; }
      public string ActorId { get; set; }
      public string RollId { get; set; }
      public string RowId { get; set; }
      public string Purpose { get; set; }
      public string ResourceVersion { get; set; }
      public DateTimeOffset ExpiresAt { get; set; }
      public Dictionary<string, string> QpfSettings { get; set; }
    }

    sealed class JobState
    {
      public string JobId { get; set; }
      public PlanState Plan { get; set; }
      public ScanProcessingDemoJobStatus Status { get; set; }
      public DateTimeOffset AcceptedAt { get; set; }
      public DateTimeOffset NextTransitionAt { get; set; }
      public DateTimeOffset? CompletedAt { get; set; }
      public ScanProcessingDemoQpfApplyResult QpfApply { get; set; }
    }

    sealed record IdempotentValue<T>(string Fingerprint, T Value);
  }
}
