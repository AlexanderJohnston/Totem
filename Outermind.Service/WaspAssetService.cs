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

    public async Task<List<string>> GetAssetIdsAsync()
    {
      var assetIds = new List<string>();
      var seenAssetIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
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

        if (!result.HasSuccessWithMoreDataRemaining)
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
  }
}
