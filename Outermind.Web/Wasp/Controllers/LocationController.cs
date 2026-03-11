using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Locations;

namespace Quantum.Web.Wasp.Controllers;

public class LocationController : WaspHttpClient
{
    public LocationController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<LocationModelInfo>>> CreateAsync(IReadOnlyList<LocationModelInfo> locations) =>
        BatchAsync<LocationModelInfo, LocationModelInfo>(locations, batch =>
            PostAsync<List<LocationModelInfo>>("public-api/locations/create", batch));

    public Task<WaspResult<List<LocationModelInfo>>> UpdateAsync(IReadOnlyList<LocationModelInfo> locations) =>
        BatchAsync<LocationModelInfo, LocationModelInfo>(locations, batch =>
            PostAsync<List<LocationModelInfo>>("public-api/locations/update", batch));

    public Task<WaspResult<LocationModelInfo>> SearchExactAsync(string searchText) =>
        PostAsync<LocationModelInfo>("public-api/locations/search/exact", searchText);

    public Task<WaspResult<List<LocationModelInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<LocationModelInfo>>("public-api/locations/advancedinfosearch", search);

    public Task<WaspResult<List<LocationModelInfo>>> GetByCodeAsync(IReadOnlyList<string> codes) =>
        PostAsync<List<LocationModelInfo>>("public-api/locations/getlocationsbycode", codes);

    public Task<WaspResult<List<LocationModelInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<LocationModelInfo>>("public-api/locations/infosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<WtResult>>> DeleteByCodeAsync(IReadOnlyList<string> codes) =>
        PostAsync<List<WtResult>>("public-api/locations/deletelocationsbycode", codes);
}
