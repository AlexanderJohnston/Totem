using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class AddressController : WaspHttpClient
{
    public AddressController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<WtResult>>> SaveCustomerAddressesAsync(IReadOnlyList<AddressInfo> addresses) =>
        BatchVoidAsync(addresses, batch =>
            PostAsync<List<WtResult>>("public-api/addresses/customer/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveEmployeeAddressesAsync(IReadOnlyList<AddressInfo> addresses) =>
        BatchVoidAsync(addresses, batch =>
            PostAsync<List<WtResult>>("public-api/addresses/employee/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveManufacturerAddressesAsync(IReadOnlyList<AddressInfo> addresses) =>
        BatchVoidAsync(addresses, batch =>
            PostAsync<List<WtResult>>("public-api/addresses/manufacturers/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveSupplierAddressesAsync(IReadOnlyList<AddressInfo> addresses) =>
        BatchVoidAsync(addresses, batch =>
            PostAsync<List<WtResult>>("public-api/addresses/supplier/save", batch));
}
