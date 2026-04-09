using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Outermind.Microfilm;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;

namespace Outermind.Service
{
  /// <summary>
  /// Fetches WASP asset IDs for import processing using the WASP public API
  /// </summary>
  public class WaspAssetService : IWaspAssetService
  {
    const int AssetPageSize = 500;
    static readonly TimeSpan AssetCacheDuration = TimeSpan.FromMinutes(30);
    const string AssetSnapshotCacheKey = "WaspAssetService.AssetSnapshot";

    readonly HttpClient _http;
    readonly IMemoryCache _cache;
    readonly TimeProvider _timeProvider;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true
    };

    public WaspAssetService(HttpClient http, IMemoryCache cache, TimeProvider timeProvider = null)
    {
      _http = http;
      _cache = cache;
      _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<WaspImportClientBatch> GetClientBatchAsync(int clientPosition)
    {
      if (clientPosition < 0)
      {
        return null;
      }

      var snapshot = await GetAssetSnapshotAsync(clientPosition);

      if (snapshot.HasUnassignedAssets && clientPosition == 0)
      {
        return new WaspImportClientBatch(null, snapshot.GetUnassignedAssetIds());
      }

      var jobNumberPosition = clientPosition - (snapshot.HasUnassignedAssets ? 1 : 0);

      if (jobNumberPosition < 0 || jobNumberPosition >= snapshot.JobNumbers.Count)
      {
        return null;
      }

      var jobNumber = snapshot.JobNumbers[jobNumberPosition];

      return new WaspImportClientBatch(jobNumber, snapshot.GetAssetIds(jobNumber));
    }

    async Task<CachedAssetSnapshot> GetAssetSnapshotAsync(int clientPosition)
    {
      if (_cache.TryGetValue<CachedAssetSnapshot>(AssetSnapshotCacheKey, out var snapshot)
        && !ShouldRefreshSnapshot(snapshot, clientPosition))
      {
        return snapshot;
      }

      var assetIds = await GetAssetIdsAsync();
      snapshot = BuildAssetSnapshot(assetIds);
      _cache.Set(AssetSnapshotCacheKey, snapshot);
      return snapshot;
    }

    async Task<List<string>> GetAssetIdsAsync()
    {
      var assetIds = new List<string>();
      var seenAssetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      long? totalCount = null;
      var pageNumber = 1;

      while (true)
      {
        var result = await FetchAssetPageAsync(new AdvancedSearchParameters
        {
          PageSize = AssetPageSize,
          PageNumber = pageNumber,
          TotalCountFromPriorFetch = totalCount,
          IgnoreAttachments = true,
          IgnoreGeoLocation = true
        });

        foreach (var assetTag in result.Data?
          .Where(a => !string.IsNullOrWhiteSpace(a.AssetTag))
          .Select(a => a.AssetTag)
          ?? Enumerable.Empty<string>())
        {
          if (seenAssetIds.Add(assetTag))
          {
            assetIds.Add(assetTag);
          }
        }

        totalCount = result.TotalRecordsLongCount;

        if (!ShouldContinuePaging(pageNumber, AssetPageSize, result.Data?.Count ?? 0, totalCount ?? 0))
        {
          return assetIds;
        }

        pageNumber++;
      }
    }

    async Task<WaspResult<List<AssetInfo>>> FetchAssetPageAsync(AdvancedSearchParameters request)
    {
      var response = await _http.PostAsJsonAsync("public-api/assets/assetadvancedinfosearch", request, JsonOptions);
      response.EnsureSuccessStatusCode();

      var result = await response.Content.ReadFromJsonAsync<WaspResult<List<AssetInfo>>>(JsonOptions)
        ?? throw new InvalidOperationException("WASP returned an empty asset search response.");

      if (result.HasError || result.HasHttpError)
      {
        var message = result.Messages
          .Where(m => !string.IsNullOrWhiteSpace(m.Message))
          .Select(m => m.Message)
          .DefaultIfEmpty("WASP asset search reported an error.")
          .Aggregate((current, next) => $"{current}; {next}");

        throw new InvalidOperationException(message);
      }

      return result;
    }

    static bool ShouldContinuePaging(int pageNumber, int pageSize, int fetchedCount, long totalCount)
    {
      if (fetchedCount == 0)
      {
        return false;
      }

      if (totalCount > 0)
      {
        return (long)pageNumber * pageSize < totalCount;
      }

      return fetchedCount >= pageSize;
    }

    bool ShouldRefreshSnapshot(CachedAssetSnapshot snapshot, int clientPosition)
    {
      if (snapshot is null)
      {
        return true;
      }

      if (clientPosition > 0)
      {
        return false;
      }

      return _timeProvider.GetUtcNow() - snapshot.CachedAt >= AssetCacheDuration;
    }

    CachedAssetSnapshot BuildAssetSnapshot(List<string> assetIds)
    {
      var unassignedAssetIds = new List<string>();
      var assetIdsByJobNumber = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

      foreach (var assetId in assetIds ?? Enumerable.Empty<string>())
      {
        if (!WaspAssetTagParser.TryGetJobNumber(assetId, out var jobNumber))
        {
          unassignedAssetIds.Add(assetId);
          continue;
        }

        if (!assetIdsByJobNumber.TryGetValue(jobNumber, out var groupedAssetIds))
        {
          groupedAssetIds = new List<string>();
          assetIdsByJobNumber[jobNumber] = groupedAssetIds;
        }

        groupedAssetIds.Add(assetId);
      }

      unassignedAssetIds.Sort(StringComparer.OrdinalIgnoreCase);

      foreach (var groupedAssetIds in assetIdsByJobNumber.Values)
      {
        groupedAssetIds.Sort(StringComparer.OrdinalIgnoreCase);
      }

      var jobNumbers = assetIdsByJobNumber.Keys
        .OrderBy(jobNumber => jobNumber, StringComparer.OrdinalIgnoreCase)
        .ToList();

      return new CachedAssetSnapshot(
        _timeProvider.GetUtcNow(),
        unassignedAssetIds,
        jobNumbers,
        assetIdsByJobNumber);
    }

    sealed class CachedAssetSnapshot
    {
      readonly Dictionary<string, List<string>> _assetIdsByJobNumber;

      public DateTimeOffset CachedAt { get; }
      public List<string> UnassignedAssetIds { get; }
      public List<string> JobNumbers { get; }
      public bool HasUnassignedAssets => UnassignedAssetIds.Count > 0;

      public CachedAssetSnapshot(
        DateTimeOffset cachedAt,
        List<string> unassignedAssetIds,
        List<string> jobNumbers,
        Dictionary<string, List<string>> assetIdsByJobNumber)
      {
        CachedAt = cachedAt;
        UnassignedAssetIds = unassignedAssetIds ?? new List<string>();
        JobNumbers = jobNumbers ?? new List<string>();
        _assetIdsByJobNumber = assetIdsByJobNumber ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
      }

      public List<string> GetUnassignedAssetIds() => new(UnassignedAssetIds);

      public List<string> GetAssetIds(string jobNumber)
      {
        if (_assetIdsByJobNumber.TryGetValue(jobNumber, out var assetIds))
        {
          return new List<string>(assetIds);
        }

        return new List<string>();
      }
    }
  }
}
