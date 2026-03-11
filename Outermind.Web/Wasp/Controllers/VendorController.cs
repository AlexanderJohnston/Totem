using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Vendors;

namespace Quantum.Web.Wasp.Controllers;

public class VendorController : WaspHttpClient
{
    public VendorController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<AssetCheckOutStatus>>> GetCheckoutStatusAsync(string vendorNumber) =>
        GetAsync<List<AssetCheckOutStatus>>($"public-api/vendors/checkout-status/{Uri.EscapeDataString(vendorNumber)}");

    public Task<WaspResult<List<VendorInfo>>> CreateNewAsync(IReadOnlyList<VendorInfo> vendors) =>
        BatchAsync<VendorInfo, VendorInfo>(vendors, batch =>
            PostAsync<List<VendorInfo>>("public-api/vendors/createNew", batch));

    public Task<WaspResult<List<VendorInfo>>> UpdateExistingAsync(IReadOnlyList<VendorInfo> vendors) =>
        BatchAsync<VendorInfo, VendorInfo>(vendors, batch =>
            PostAsync<List<VendorInfo>>("public-api/vendors/updateExisting", batch));

    public Task<WaspResult<List<VendorInfo>>> AdvancedSearchAsync(AdvancedSearchParameters search) =>
        PostAsync<List<VendorInfo>>("public-api/vendors/advancedinfosearch", search);

    public Task<WaspResult<List<VendorInfo>>> GetByNumberAsync(IReadOnlyList<string> vendorNumbers) =>
        PostAsync<List<VendorInfo>>("public-api/vendors/getvendorbynumber", vendorNumbers);
}
