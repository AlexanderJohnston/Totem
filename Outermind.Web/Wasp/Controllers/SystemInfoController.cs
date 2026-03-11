using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class SystemInfoController : WaspHttpClient
{
    public SystemInfoController(HttpClient http) : base(http) { }

    public Task<WaspResult<string>> GetServerTypeAsync() =>
        PostAsync<string>("public-api/systeminfo/servertype", new { });
}
