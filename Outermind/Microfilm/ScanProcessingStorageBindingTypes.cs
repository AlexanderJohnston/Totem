using System;
using System.Collections.Generic;
using System.Linq;

namespace Outermind.Microfilm
{
  /// <summary>
  /// Fixed logical capability identities. Operations configuration may later map two or more
  /// capabilities to the same physical root without collapsing these durable identities.
  /// </summary>
  public static class ScanProcessingStorageCapabilities
  {
    public const string ScanParent = "scan-parent";
    public const string Qpf = "qpf";
    public const string GrayscaleFrames = "frames-grayscale";
    public const string BitonalFrames = "frames-bitonal";

    static readonly string[] Catalog =
    {
      BitonalFrames,
      GrayscaleFrames,
      Qpf,
      ScanParent
    };

    static readonly Dictionary<string, string> CanonicalByValue = Catalog.ToDictionary(
      capability => capability,
      capability => capability,
      StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> All => Catalog;

    public static bool TryNormalize(string capability, out string canonical)
    {
      var candidate = string.IsNullOrWhiteSpace(capability) ? null : capability.Trim();
      return CanonicalByValue.TryGetValue(candidate ?? string.Empty, out canonical);
    }
  }

  public static class ScanProcessingStorageBindingIssueCodes
  {
    public const string AuthenticatedActorRequired = "AUTHENTICATED_ACTOR_REQUIRED";
    public const string ClientNotFound = "STORAGE_BINDING_CLIENT_NOT_FOUND";
    public const string InvalidRequest = "STORAGE_BINDING_INVALID_REQUEST";
    public const string InvalidLabel = "STORAGE_BINDING_INVALID_LABEL";
    public const string InvalidCapabilities = "STORAGE_BINDING_INVALID_CAPABILITIES";
    public const string BindingAlreadyExists = "STORAGE_BINDING_ALREADY_EXISTS";
    public const string BindingNotFound = "STORAGE_BINDING_NOT_FOUND";
    public const string StorageRevisionStale = "STORAGE_BINDING_REVISION_STALE";
    public const string ConfigurationGenerationStale = "STORAGE_CONFIGURATION_GENERATION_STALE";
    public const string ResourceReferenceInvalid = "RESOURCE_REFERENCE_INVALID";
    public const string ResourceReferenceRevoked = "RESOURCE_REFERENCE_REVOKED";
    public const string ResourceReferenceScopeMismatch = "RESOURCE_REFERENCE_SCOPE_MISMATCH";
    public const string ResourceReferencePurposeMismatch = "RESOURCE_REFERENCE_PURPOSE_MISMATCH";
    public const string ResourceReferenceGenerationStale = "RESOURCE_REFERENCE_GENERATION_STALE";
    public const string ResourceReferencePermissionStale = "RESOURCE_REFERENCE_PERMISSION_STALE";
  }

  public static class ScanProcessingStorageBindingActions
  {
    public const string Create = "storage-binding.create";
    public const string Activate = "storage-binding.activate";
  }

  public sealed class ScanProcessingStorageCapability
  {
    public string Kind { get; set; }
    public string Label { get; set; }

    public ScanProcessingStorageCapability()
    {
    }

    public ScanProcessingStorageCapability(string kind, string label)
    {
      Kind = kind;
      Label = label;
    }

    public ScanProcessingStorageCapability Clone() => new(Kind, Label);
  }

  public sealed class ScanProcessingStorageBindingDefinition
  {
    public string BindingId { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string Label { get; set; }
    public List<ScanProcessingStorageCapability> Capabilities { get; set; } = new();
    public int DefinitionVersion { get; set; }
    public long CreatedStorageRevision { get; set; }

    public ScanProcessingStorageBindingDefinition()
    {
    }

    public ScanProcessingStorageBindingDefinition(
      string bindingId,
      string clientId,
      string workspaceId,
      string label,
      IEnumerable<ScanProcessingStorageCapability> capabilities,
      int definitionVersion,
      long createdStorageRevision)
    {
      BindingId = bindingId;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      Label = label;
      Capabilities = capabilities?
        .Select(capability => capability.Clone())
        .OrderBy(capability => capability.Kind, StringComparer.Ordinal)
        .ToList() ?? new List<ScanProcessingStorageCapability>();
      DefinitionVersion = definitionVersion;
      CreatedStorageRevision = createdStorageRevision;
    }

    public ScanProcessingStorageBindingDefinition Clone() =>
      new(BindingId, ClientId, WorkspaceId, Label, Capabilities, DefinitionVersion, CreatedStorageRevision);

    public bool TryGetCapability(string kind, out ScanProcessingStorageCapability capability)
    {
      capability = Capabilities.FirstOrDefault(candidate =>
        string.Equals(candidate.Kind, kind, StringComparison.Ordinal));
      return capability != null;
    }
  }

  public sealed class ScanProcessingActiveStorageBinding
  {
    public string BindingId { get; set; }
    public long ConfigurationGeneration { get; set; }
    public long ActivatedStorageRevision { get; set; }

    public ScanProcessingActiveStorageBinding()
    {
    }

    public ScanProcessingActiveStorageBinding(
      string bindingId,
      long configurationGeneration,
      long activatedStorageRevision)
    {
      BindingId = bindingId;
      ConfigurationGeneration = configurationGeneration;
      ActivatedStorageRevision = activatedStorageRevision;
    }

    public ScanProcessingActiveStorageBinding Clone() =>
      new(BindingId, ConfigurationGeneration, ActivatedStorageRevision);
  }

  /// <summary>
  /// Shared deterministic reducer used by the client-routed authority topic and read projection.
  /// Physical roots and Server identifiers are deliberately absent.
  /// </summary>
  public sealed class ScanProcessingStorageBindingState
  {
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public bool ClientRecognized { get; set; }
    public long StorageRevision { get; set; }
    public Dictionary<string, ScanProcessingStorageBindingDefinition> BindingsById { get; set; } = new();
    public Dictionary<string, long> LatestActivatedGenerationByBindingId { get; set; } = new();
    public ScanProcessingActiveStorageBinding ActiveBinding { get; set; }

    public void Apply(ClientCreated e)
    {
      ClientId = e.Client.ClientId.ToString();
      WorkspaceId = ClientId;
      ClientRecognized = true;
    }

    public void Apply(ScanProcessingStorageBindingCreated e)
    {
      if(e.Binding == null || string.IsNullOrWhiteSpace(e.Binding.BindingId))
      {
        return;
      }

      ClientId = e.ClientId;
      WorkspaceId = e.WorkspaceId;
      ClientRecognized = true;
      BindingsById[e.Binding.BindingId] = e.Binding.Clone();
      StorageRevision = Math.Max(StorageRevision, e.StorageRevision);
    }

    public void Apply(ScanProcessingStorageBindingActivated e)
    {
      ClientId = e.ClientId;
      WorkspaceId = e.WorkspaceId;
      ClientRecognized = true;
      LatestActivatedGenerationByBindingId[e.BindingId] = e.ConfigurationGeneration;
      ActiveBinding = new ScanProcessingActiveStorageBinding(
        e.BindingId,
        e.ConfigurationGeneration,
        e.StorageRevision);
      StorageRevision = Math.Max(StorageRevision, e.StorageRevision);
    }

    public bool TryGetBinding(string bindingId, out ScanProcessingStorageBindingDefinition binding) =>
      BindingsById.TryGetValue(bindingId ?? string.Empty, out binding);

    public long LatestGeneration(string bindingId) =>
      LatestActivatedGenerationByBindingId.TryGetValue(bindingId ?? string.Empty, out var generation)
        ? generation
        : 0;
  }

  /// <summary>
  /// Server-side record behind an opaque resource-reference ID. Future HTTP inputs accept only
  /// the ID; they must never bind this record or any informational path from a client request.
  /// </summary>
  public sealed class ScanProcessingResourceReference
  {
    public string Id { get; set; }
    public string ActorId { get; set; }
    public string Purpose { get; set; }
    public ScanProcessingResolvedScope Scope { get; set; }
    public string Permission { get; set; }
    public long AuthorizationRevision { get; set; }
    public string BindingId { get; set; }
    public long ConfigurationGeneration { get; set; }
    public string Capability { get; set; }
    public string DisplayName { get; set; }
    public string Version { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool Revoked { get; set; }

    public ScanProcessingResourceReference Clone() => new()
    {
      Id = Id,
      ActorId = ActorId,
      Purpose = Purpose,
      Scope = Scope?.Clone(),
      Permission = Permission,
      AuthorizationRevision = AuthorizationRevision,
      BindingId = BindingId,
      ConfigurationGeneration = ConfigurationGeneration,
      Capability = Capability,
      DisplayName = DisplayName,
      Version = Version,
      ExpiresAt = ExpiresAt,
      Revoked = Revoked
    };
  }

  public sealed record ScanProcessingResourceReferenceValidation(bool IsValid, string Code)
  {
    public static ScanProcessingResourceReferenceValidation Valid() => new(true, null);
    public static ScanProcessingResourceReferenceValidation Invalid(string code) => new(false, code);
  }

  /// <summary>
  /// Creates and validates server-side references without resolving a physical location.
  /// Current access and storage state must be authoritative replayed state, never client claims.
  /// </summary>
  public static class ScanProcessingResourceReferences
  {
    public static bool TryIssue(
      string referenceId,
      ScanProcessingActorIdentity actor,
      ScanProcessingResolvedScope scope,
      string purpose,
      string permission,
      string capability,
      ScanProcessingAccessState access,
      ScanProcessingStorageBindingState storage,
      out ScanProcessingResourceReference reference)
    {
      reference = null;

      if(string.IsNullOrWhiteSpace(referenceId)
        || actor?.IsValid != true
        || scope?.IsValid != true
        || string.IsNullOrWhiteSpace(purpose)
        || !ScanProcessingPermissions.TryNormalize(permission, out var canonicalPermission)
        || !ScanProcessingStorageCapabilities.TryNormalize(capability, out var canonicalCapability)
        || storage?.ActiveBinding == null
        || !string.Equals(scope.ClientId, storage.ClientId, StringComparison.Ordinal)
        || !string.Equals(scope.WorkspaceId, storage.WorkspaceId, StringComparison.Ordinal)
        || !storage.TryGetBinding(storage.ActiveBinding.BindingId, out var binding)
        || !binding.TryGetCapability(canonicalCapability, out var configuredCapability)
        || access == null
        || access.ResolveEffectiveRoles(actor.UserId, canonicalPermission).Count == 0)
      {
        return false;
      }

      reference = new ScanProcessingResourceReference
      {
        Id = referenceId.Trim(),
        ActorId = actor.UserId,
        Purpose = purpose.Trim(),
        Scope = scope.Clone(),
        Permission = canonicalPermission,
        AuthorizationRevision = access.AuthorizationRevision,
        BindingId = storage.ActiveBinding.BindingId,
        ConfigurationGeneration = storage.ActiveBinding.ConfigurationGeneration,
        Capability = canonicalCapability,
        DisplayName = configuredCapability.Label,
        Version = $"storage-{storage.StorageRevision}",
        ExpiresAt = null,
        Revoked = false
      };

      return true;
    }

    public static ScanProcessingResourceReferenceValidation ValidateUse(
      ScanProcessingResourceReference reference,
      ScanProcessingActorIdentity actor,
      ScanProcessingResolvedScope scope,
      string purpose,
      string permission,
      string capability,
      ScanProcessingAccessState access,
      ScanProcessingStorageBindingState storage)
    {
      if(reference == null || string.IsNullOrWhiteSpace(reference.Id))
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferenceInvalid);
      }

      if(reference.Revoked)
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferenceRevoked);
      }

      if(actor?.IsValid != true
        || scope?.IsValid != true
        || !string.Equals(reference.ActorId, actor.UserId, StringComparison.Ordinal)
        || !reference.Scope.Equals(scope))
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferenceScopeMismatch);
      }

      if(string.IsNullOrWhiteSpace(purpose)
        || !string.Equals(reference.Purpose, purpose.Trim(), StringComparison.Ordinal))
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferencePurposeMismatch);
      }

      if(!ScanProcessingPermissions.TryNormalize(permission, out var canonicalPermission)
        || !ScanProcessingStorageCapabilities.TryNormalize(capability, out var canonicalCapability)
        || !string.Equals(reference.Permission, canonicalPermission, StringComparison.Ordinal)
        || !string.Equals(reference.Capability, canonicalCapability, StringComparison.Ordinal))
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferenceInvalid);
      }

      if(storage?.ActiveBinding == null
        || !string.Equals(reference.Scope.ClientId, storage.ClientId, StringComparison.Ordinal)
        || !string.Equals(reference.Scope.WorkspaceId, storage.WorkspaceId, StringComparison.Ordinal)
        || !string.Equals(reference.BindingId, storage.ActiveBinding.BindingId, StringComparison.Ordinal)
        || reference.ConfigurationGeneration != storage.ActiveBinding.ConfigurationGeneration
        || !storage.TryGetBinding(reference.BindingId, out var binding)
        || !binding.TryGetCapability(canonicalCapability, out _))
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferenceGenerationStale);
      }

      if(access == null
        || reference.AuthorizationRevision != access.AuthorizationRevision
        || access.ResolveEffectiveRoles(actor.UserId, canonicalPermission).Count == 0)
      {
        return ScanProcessingResourceReferenceValidation.Invalid(
          ScanProcessingStorageBindingIssueCodes.ResourceReferencePermissionStale);
      }

      return ScanProcessingResourceReferenceValidation.Valid();
    }
  }
}
