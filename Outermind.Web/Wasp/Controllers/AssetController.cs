using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class AssetController : WaspHttpClient
{
    public AssetController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<AssetInfo>>> CreateFixedAssetsAsync(IReadOnlyList<AssetInfo> assets) =>
        BatchAsync<AssetInfo, AssetInfo>(assets, batch =>
            PostAsync<List<AssetInfo>>("public-api/assets/createFixedAssets", batch));

    public Task<WaspResult<List<AssetInfo>>> UpdateFixedAssetsAsync(IReadOnlyList<AssetInfo> assets) =>
        BatchAsync<AssetInfo, AssetInfo>(assets, batch =>
            PostAsync<List<AssetInfo>>("public-api/assets/updateFixedAssets", batch));

    public Task<WaspResult<List<AssetInfo>>> PartialUpdateFixedAssetsAsync(IReadOnlyList<AssetInfo> assets) =>
        BatchAsync<AssetInfo, AssetInfo>(assets, batch =>
            PostAsync<List<AssetInfo>>("public-api/assets/UpdateSpecificFieldsOfFixedAssets", batch));

    public Task<WaspResult<List<InventoriedAssetInfo>>> CreateMultiQuantityAssetsAsync(IReadOnlyList<InventoriedAssetInfo> assets) =>
        BatchAsync<InventoriedAssetInfo, InventoriedAssetInfo>(assets, batch =>
            PostAsync<List<InventoriedAssetInfo>>("public-api/assets/createMultiQuantityAssets", batch));

    public Task<WaspResult<List<InventoriedAssetInfo>>> UpdateMultiQuantityAssetsAsync(IReadOnlyList<InventoriedAssetInfo> assets) =>
        BatchAsync<InventoriedAssetInfo, InventoriedAssetInfo>(assets, batch =>
            PostAsync<List<InventoriedAssetInfo>>("public-api/assets/updateMultiQuantityAssets", batch));

    public Task<WaspResult<List<InventoriedAssetInfo>>> AddAssetQuantityAsync(IReadOnlyList<InventoriedAssetInfo> assets) =>
        BatchAsync<InventoriedAssetInfo, InventoriedAssetInfo>(assets, batch =>
            PostAsync<List<InventoriedAssetInfo>>("public-api/assets/AddAssetQuantity", batch));

    public Task<WaspResult<List<WaspResult<AssetInfo>>>> GetByTagsAsync(IReadOnlyList<string> assetTags) =>
        PostAsync<List<WaspResult<AssetInfo>>>("public-api/assets/getAssetsByTags", assetTags);

    public Task<WaspResult<List<WaspResult<AssetInfo>>>> GetByTagsLightAsync(IReadOnlyList<string> assetTags) =>
        PostAsync<List<WaspResult<AssetInfo>>>("public-api/assets/getAssetsByTagsLight", assetTags);

    public Task<WaspResult<List<AssetInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<AssetInfo>>("public-api/assets/assetinfosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<AssetInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<AssetInfo>>("public-api/assets/assetadvancedinfosearch", search);

    public Task<WaspResult<AssetCheckOutStatus>> GetCheckoutStatusAsync(string assetTag) =>
        GetAsync<AssetCheckOutStatus>($"public-api/assets/checkout-status/{Uri.EscapeDataString(assetTag)}");

    public Task<Stream> StreamCsvFlatAsync(GridStreamRequestModel request) =>
        PostStreamAsync("public-api/assets/streamgridrequestcsvflat", request);

    public Task<Stream> StreamCsvUniqueAsync(GridStreamRequestModel request) =>
        PostStreamAsync("public-api/assets/streamgridrequestcsvunique", request);

    public Task<Stream> StreamCsvMqaAsync(GridStreamRequestModel request) =>
        PostStreamAsync("public-api/assets/streamgridrequestcsvmqa", request);
}
