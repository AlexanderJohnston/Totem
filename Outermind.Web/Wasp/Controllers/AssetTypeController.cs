using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.AssetTypes;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class AssetTypeController : WaspHttpClient
{
    public AssetTypeController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<AssetTypeInfo>>> CreateAsync(IReadOnlyList<AssetTypeInfo> assetTypes) =>
        BatchAsync<AssetTypeInfo, AssetTypeInfo>(assetTypes, batch =>
            PostAsync<List<AssetTypeInfo>>("public-api/asset-types/create", batch));

    public Task<WaspResult<List<AssetTypeInfo>>> UpdateAsync(IReadOnlyList<AssetTypeInfo> assetTypes) =>
        BatchAsync<AssetTypeInfo, AssetTypeInfo>(assetTypes, batch =>
            PostAsync<List<AssetTypeInfo>>("public-api/asset-types/update", batch));

    public Task<WaspResult<List<AssetTypeInfo>>> GetByNumberAsync(IReadOnlyList<string> numbers) =>
        PostAsync<List<AssetTypeInfo>>("public-api/asset-types/getassettypesbynumber", numbers);

    public Task<WaspResult<List<AssetTypeInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<AssetTypeInfo>>("public-api/asset-types/infosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<AssetTypeInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<AssetTypeInfo>>("public-api/asset-types/advancedinfosearch", search);

    public Task<WaspResult<List<WtResult>>> DeleteByNumberAsync(IReadOnlyList<string> numbers) =>
        PostAsync<List<WtResult>>("public-api/asset-types/deleteassettypesbynumber", numbers);

    public Task<WaspResult<List<AssetTypeInfo>>> GetAllSimpleAsync() =>
        PostAsync<List<AssetTypeInfo>>("public-api/asset-types/simple/all", new { });
}
