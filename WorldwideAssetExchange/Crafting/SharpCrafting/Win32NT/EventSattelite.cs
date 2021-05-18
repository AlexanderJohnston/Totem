using JetBrains.Annotations;
using Newtonsoft.Json;
using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Model;
using PostSharp.Patterns.Threading;
using SharpCrafting.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WaxInterfaces;
using static PostSharp.Patterns.Diagnostics.FormattedMessageBuilder;

namespace SharpCrafting.Win32NT
{
    [PrivateThreadAware]
    class EventSattelite : INativeClass
    {
        [Reference]
        private readonly LogSource _log = LogSource.Get ().WithLevels ( LogLevel.Trace, LogLevel.Warning );
        [Parent, CanBeNull]
        private EventSatteliteService _parent { get; set; }
        [Child]
        private LaunchpadReceiver _receiver { get; set; }
        [Reference]
        private static readonly HttpClient client = new HttpClient();
        private readonly IConsoleArea _console;

        public delegate Task Del(string block, string id);

        public EventSattelite()
        {
            //_console = console;
        }

        [EntryPoint]
        public async Task Initialize<T>([CanBeNull] T parent)
        {
            Type parentType = typeof(T);
            if (parentType == typeof(EventSatteliteService))
                _parent = (EventSatteliteService)(object)parent;
            Del handler = StoreBlock;
            _receiver = new LaunchpadReceiver(handler);
        }

        //[Reentrant]
        //private async Task StoreWallets(string block)
        //{
        //    SetupClient();
        //    var requestJson = JsonSerializer.Serialize(wallets);
        //    File.WriteAllText($"{Environment.CurrentDirectory}-wallets-api.json", requestJson);
        //    var request = new HttpRequestMessage
        //    {
        //        Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
        //        RequestUri = new Uri(@"https://localhost:5001/api/wallet/add"),
        //        Method = HttpMethod.Put
        //    };
        //    var response = await client.SendAsync(request);
        //    response.EnsureSuccessStatusCode();
        //    var json = await response.Content.ReadAsStringAsync();
        //}

        private async Task StoreBlock(string block, string id)
        {
            SetupClient();
            var requestJson = JsonConvert.SerializeObject(new
            {
                Block = block,
                Id = id
            });
            var blockJson = $"{{\"Block\":{block}}}";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(@"http://localhost:5000/block/"),
                Method = HttpMethod.Put
            };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return;
        }

        private void SetupClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.26.8");
            client.DefaultRequestHeaders.Add("Accept", @"*/*");
            client.DefaultRequestHeaders.Add("Host", @"localhost");
            client.DefaultRequestHeaders.Add("Postman-Token", Guid.NewGuid().ToString());
        }

        public async Task Terminate(string reason)
        {
            _receiver.Dispose();
            _log.Trace.Write(Formatted("[Native Event Sattelite]: Disposed of the event sattelite after receiving reason: {reason}",
                                           reason));
        }
    }
}
