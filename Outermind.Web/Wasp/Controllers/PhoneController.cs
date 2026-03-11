using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class PhoneController : WaspHttpClient
{
    public PhoneController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<WtResult>>> SaveCustomerPhonesAsync(IReadOnlyList<PhoneInfo> phones) =>
        BatchVoidAsync(phones, batch =>
            PostAsync<List<WtResult>>("public-api/phones/customer/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveEmployeePhonesAsync(IReadOnlyList<PhoneInfo> phones) =>
        BatchVoidAsync(phones, batch =>
            PostAsync<List<WtResult>>("public-api/phones/employee/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveManufacturerPhonesAsync(IReadOnlyList<PhoneInfo> phones) =>
        BatchVoidAsync(phones, batch =>
            PostAsync<List<WtResult>>("public-api/phones/manufacturers/save", batch));

    public Task<WaspResult<List<WtResult>>> SaveSupplierPhonesAsync(IReadOnlyList<PhoneInfo> phones) =>
        BatchVoidAsync(phones, batch =>
            PostAsync<List<WtResult>>("public-api/phones/supplier/save", batch));
}
