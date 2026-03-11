using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Contracts;

namespace Quantum.Web.Wasp.Controllers;

public class ContractController : WaspHttpClient
{
    public ContractController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<ContractInfo>>> CreateAsync(IReadOnlyList<ContractInfo> contracts) =>
        BatchAsync<ContractInfo, ContractInfo>(contracts, batch =>
            PostAsync<List<ContractInfo>>("public-api/contracts/create", batch));

    public Task<WaspResult<List<ContractInfo>>> UpdateAsync(IReadOnlyList<ContractInfo> contracts) =>
        BatchAsync<ContractInfo, ContractInfo>(contracts, batch =>
            PostAsync<List<ContractInfo>>("public-api/contracts/update", batch));

    public Task<WaspResult<List<WtResult>>> DeleteAsync(IReadOnlyList<string> contractNumbers) =>
        BatchVoidAsync(contractNumbers, batch =>
            PostAsync<List<WtResult>>("public-api/contracts/delete", batch));

    public Task<WaspResult<List<WtResult>>> AddAssetsAsync(Dictionary<string, List<string>> contractAssets) =>
        PostAsync<List<WtResult>>("public-api/contracts/addAssets", contractAssets);

    public Task<WaspResult<List<WtResult>>> RemoveAssetsAsync(Dictionary<string, List<string>> contractAssets) =>
        PostAsync<List<WtResult>>("public-api/contracts/removeAssets", contractAssets);

    public Task<WaspResult<ContractInfo>> SearchExactAsync(string contractNumber) =>
        PostAsync<ContractInfo>("public-api/contracts/search/exact", contractNumber);

    public Task<WaspResult<List<ContractInfo>>> GetByNumberAsync(IReadOnlyList<string> contractNumbers) =>
        PostAsync<List<ContractInfo>>("public-api/contracts/getcontractsbynumber", contractNumbers);

    public Task<WaspResult<List<ContractInfo>>> GetByNumberV2Async(IReadOnlyList<string> contractNumbers) =>
        PostAsync<List<ContractInfo>>("public-api/contracts/getcontractsbynumberV2", contractNumbers);

    public Task<WaspResult<List<ContractInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<ContractInfo>>("public-api/contracts/infosearch", CreateSearchPatternRequest(searchText));
}
