using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Feedback
{
    class Test
    {
        private static readonly HttpClient client = new HttpClient();
        private static JsonSerializerOptions _opts;
        public static async Task GetWallet(string Wallet)
        {
            SetupClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@"https://localhost:5001/api/wallet/get/y2bbm.wam"),
                Method = HttpMethod.Get
            };
            var response = await client.SendAsync(request);
            Console.WriteLine(response.ToString());


        }

        static void SetupJsonSerializer()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            _opts = options;
        }

        static void SetupClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.26.8");
            client.DefaultRequestHeaders.Add("Cookie", @"__cfduid=d12d36124612a89fba4d4db67ff7594341620269474; AWSELB=C17BB181189BA932E9703059DB2661260667E9BEB5143BBA6858E03DD386E7B9BC7A1E5DAD6E4ACA2BDC9ED4295E3FB3B831CEFE2FF02B2DDE4752AC70B26B808772CB5D56; AWSELBCORS=C17BB181189BA932E9703059DB2661260667E9BEB5143BBA6858E03DD386E7B9BC7A1E5DAD6E4ACA2BDC9ED4295E3FB3B831CEFE2FF02B2DDE4752AC70B26B808772CB5D56");
            client.DefaultRequestHeaders.Add("Accept", @"*/*");
            client.DefaultRequestHeaders.Add("Postman-Token", Guid.NewGuid().ToString());

        }
    }
}
