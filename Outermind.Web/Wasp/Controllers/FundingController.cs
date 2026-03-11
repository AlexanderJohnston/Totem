using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Funding;

namespace Quantum.Web.Wasp.Controllers;

public class FundingController : WaspHttpClient
{
    public FundingController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<FundingInfo>>> CreateAsync(IReadOnlyList<FundingInfo> fundings) =>
        BatchAsync<FundingInfo, FundingInfo>(fundings, batch =>
            PostAsync<List<FundingInfo>>("public-api/funding/createFunding", batch));

    public Task<WaspResult<List<FundingInfo>>> UpdateAsync(IReadOnlyList<FundingInfo> fundings) =>
        BatchAsync<FundingInfo, FundingInfo>(fundings, batch =>
            PostAsync<List<FundingInfo>>("public-api/funding/updateFunding", batch));

    public Task<WaspResult<List<WtResult>>> DeleteAsync(IReadOnlyList<FundingInfo> fundings) =>
        BatchVoidAsync(fundings, batch =>
            PostAsync<List<WtResult>>("public-api/funding/deleteFunding", batch));

    public Task<WaspResult<List<WtResult>>> AddAssetsAsync(object fundingAssets) =>
        PostAsync<List<WtResult>>("public-api/funding/addFundingAssets", fundingAssets);

    public Task<WaspResult<List<WtResult>>> RemoveAssetsAsync(object fundingAssets) =>
        PostAsync<List<WtResult>>("public-api/funding/removeFundingAssets", fundingAssets);

    public Task<WaspResult<List<WtResult>>> AddRestrictedSitesAsync(object sites) =>
        PostAsync<List<WtResult>>("public-api/funding/addFundingRestrictedSites", sites);

    public Task<WaspResult<List<WtResult>>> RemoveRestrictedSitesAsync(object sites) =>
        PostAsync<List<WtResult>>("public-api/funding/removeFundingRestrictedSites", sites);

    public Task<WaspResult<FundingInfo>> SearchExactAsync(string fundingName) =>
        PostAsync<FundingInfo>("public-api/funding/search/exactname", fundingName);

    public Task<WaspResult<List<FundingInfo>>> GetByNameAsync(IReadOnlyList<string> names) =>
        PostAsync<List<FundingInfo>>("public-api/funding/getfundingsbyname", names);

    public Task<WaspResult<List<FundingInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<FundingInfo>>("public-api/funding/fundinginfosearch", CreateSearchPatternRequest(searchText));
}
