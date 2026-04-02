using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
      var request = new SearchPatternRequest { SearchPattern = "" };
      var response = await _http.PostAsJsonAsync("public-api/assets/assetinfosearch", request, JsonOptions);
      response.EnsureSuccessStatusCode();

      var result = await response.Content.ReadFromJsonAsync<WaspResult<List<AssetInfo>>>(JsonOptions);

      return result?.Data?
        .Where(a => !string.IsNullOrWhiteSpace(a.AssetTag))
        .Select(a => a.AssetTag)
        .ToList() ?? new List<string>();
    }
  }
}
