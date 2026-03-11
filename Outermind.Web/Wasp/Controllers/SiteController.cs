using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Sites;

namespace Quantum.Web.Wasp.Controllers;

public class SiteController : WaspHttpClient
{
    public SiteController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<SiteInfo>>> CreateAsync(IReadOnlyList<SiteInfo> sites) =>
        BatchAsync<SiteInfo, SiteInfo>(sites, batch =>
            PostAsync<List<SiteInfo>>("public-api/sites/create", batch));

    public Task<WaspResult<List<SiteInfo>>> UpdateAsync(IReadOnlyList<SiteInfo> sites) =>
        BatchAsync<SiteInfo, SiteInfo>(sites, batch =>
            PostAsync<List<SiteInfo>>("public-api/sites/update", batch));

    public Task<WaspResult<SiteInfo>> SearchExactAsync(string searchText) =>
        PostAsync<SiteInfo>("public-api/sites/search/exact", searchText);

    public Task<WaspResult<List<SiteInfo>>> GetByNameAsync(IReadOnlyList<string> names) =>
        PostAsync<List<SiteInfo>>("public-api/sites/getsitesbyname", names);

    public Task<WaspResult<List<SiteInfo>>> InfoSearchAsync(string searchText) =>
        PostAsync<List<SiteInfo>>("public-api/sites/infosearch", CreateSearchPatternRequest(searchText));

    public Task<WaspResult<List<SiteInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<SiteInfo>>("public-api/sites/advancedinfosearch", search);

    public Task<WaspResult<List<WtResult>>> DeleteByNameAsync(IReadOnlyList<string> names) =>
        BatchVoidAsync(names, batch =>
            PostAsync<List<WtResult>>("public-api/sites/deletesitesbyname", batch));
}
