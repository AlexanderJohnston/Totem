using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
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

    readonly HttpClient _http;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true
    };

    public WaspAssetService(HttpClient http)
    {
      _http = http;
    }

    public async Task<WaspImportClientBatch> GetClientBatchAsync(int clientPosition)
    {
      if (clientPosition < 0)
      {
        return null;
      }

      var assetIds = await GetAssetIdsAsync();
      var unassignedAssetIds = assetIds
        .Where(assetId => !WaspAssetTagParser.TryGetJobNumber(assetId, out _))
        .OrderBy(assetId => assetId, StringComparer.OrdinalIgnoreCase)
        .ToList();

      if (unassignedAssetIds.Count > 0 && clientPosition == 0)
      {
        return new WaspImportClientBatch(null, unassignedAssetIds);
      }

      var jobNumbers = assetIds
        .Select(assetId => WaspAssetTagParser.TryGetJobNumber(assetId, out var jobNumber) ? jobNumber : null)
        .Where(jobNumber => !string.IsNullOrWhiteSpace(jobNumber))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(jobNumber => jobNumber, StringComparer.OrdinalIgnoreCase)
        .ToList();

      var jobNumberPosition = clientPosition - (unassignedAssetIds.Count > 0 ? 1 : 0);

      if (jobNumberPosition < 0 || jobNumberPosition >= jobNumbers.Count)
      {
        return null;
      }

      var jobNumber = jobNumbers[jobNumberPosition];
      var clientAssetIds = assetIds
        .Where(assetId => WaspAssetTagParser.StartsWithJobNumber(assetId, jobNumber))
        .OrderBy(assetId => assetId, StringComparer.OrdinalIgnoreCase)
        .ToList();

      return new WaspImportClientBatch(jobNumber, clientAssetIds);
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
  }
}
