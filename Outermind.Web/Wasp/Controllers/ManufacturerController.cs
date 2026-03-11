using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;
using Quantum.Wasp.Models.Manufacturers;

namespace Quantum.Web.Wasp.Controllers;

public class ManufacturerController : WaspHttpClient
{
    public ManufacturerController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<ManufacturerInfo>>> CreateNewAsync(IReadOnlyList<ManufacturerInfo> manufacturers) =>
        BatchAsync<ManufacturerInfo, ManufacturerInfo>(manufacturers, batch =>
            PostAsync<List<ManufacturerInfo>>("public-api/manufacturers/createNew", batch));

    public Task<WaspResult<List<ManufacturerInfo>>> UpdateExistingAsync(IReadOnlyList<ManufacturerInfo> manufacturers) =>
        BatchAsync<ManufacturerInfo, ManufacturerInfo>(manufacturers, batch =>
            PostAsync<List<ManufacturerInfo>>("public-api/manufacturers/updateExisting", batch));
}
