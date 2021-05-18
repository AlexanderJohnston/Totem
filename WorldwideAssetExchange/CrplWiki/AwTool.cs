using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrplWiki
{
    class AwTool
    {
        private static readonly HttpClient client = new HttpClient();

        public AssetRoot Asset;
        public readonly string Id;
        const string URI = @"https://wax.api.atomicassets.io/atomicassets/v1/assets/";
        public AwTool(string id)
        {
            Id = id;
        }

        public async Task CreateFromBoksViewer()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var address = URI + Id + '/';
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var serializedAsset = await client.GetStringAsync(address);
            var asset = JsonSerializer.Deserialize<AssetRoot>(serializedAsset);
            Asset = asset;
        }
    }
}
