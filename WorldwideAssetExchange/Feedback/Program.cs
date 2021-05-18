using Launchpad;
using RestClient.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WaxInterfaces;

namespace Feedback
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static JsonSerializerOptions _opts;
        private static Timer _blockTimer;
        private static WaxChainClient _launch;
        private static int _lastBlock;
        private static int _currentBlock;
        private static int _lastReadBlock;
        private static bool _halted = true;

        static async Task Main(string[] args)
        {
            _launch = new WaxChainClient();
            SetupJsonSerializer();
            var chain = await ReadChain();
            _currentBlock = chain.head_block_num;
            _blockTimer = new Timer(e => UpdateHeadBlock(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2));
            Console.WriteLine("Type exit to quit.");
            while (true)
            {
                var command = Console.ReadLine();
                if (command.ToLower() == "exit")
                {
                    return;
                }
                else Thread.Sleep(100);
            }
            
        }

        public static async Task UpdateHeadBlock()
        {
            var chain = await ReadChain();
            _lastBlock = chain.head_block_num;
            if (_halted)
            {
                _halted = false;
                ReadBlockChain();
            }
        }

        public static async Task ReadBlockChain()
        {
            if (_currentBlock == 0)
            {
                _currentBlock = _lastBlock;
            }
            else if (_currentBlock < _lastBlock)
            {
                _currentBlock += 1;
            }
            else if (_lastReadBlock == _lastBlock)
            {
                _halted = true;
                return;
            }
            Console.WriteLine($"Reading block {_currentBlock} from the wax chain on {DateTime.Now.ToLongTimeString()}.");
            var block = await GetRawBlock(_currentBlock.ToString());
            _launch.SendRawBlock(block, _currentBlock.ToString());
            _lastReadBlock = _currentBlock;
            Console.WriteLine("Completed.");
            ReadBlockChain();
        }

        static void SaveWalletsToDisk(List<WaxAccount> wallets)
        {
            var jsonOut = JsonSerializer.Serialize(wallets);
            File.WriteAllText($"{Environment.CurrentDirectory}\\wallets_out.json", jsonOut);
        }

        static async Task<List<WaxAccount>> ReadMinerWallets(Block block)
        {
            var accounts = new List<WaxAccount>();
            var miners = block.transactions
                .Where(act => act._parsedTrx != null && act._parsedTrx.transaction.actions[0].name == "mine")
                .Select(trans => trans._parsedTrx);
            foreach (var miner in miners)
            {
                var wallet = await GetAccount(miner.transaction.actions[0].data.miner);
                accounts.Add(wallet);
            }
            return accounts;
        }

        static async Task<WaxAccount> GetAccount(string wallet)
        {
            // https://chain.wax.io/v1/chain/get_account
            SetupClient();
            var body = new { account_name = wallet };
            var requestJson = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(@"https://chain.wax.io/v1/chain/get_account"),
                Method = HttpMethod.Get
            };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WaxAccount>(json, _opts);
        }

        static void SetupJsonSerializer()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            options.Converters.Add(new MinerDataConverter());
            _opts = options;
        }

        static async Task<Block> ReadBlock(Block block)
        {
            if (false && File.Exists(Environment.CurrentDirectory + $"\\block_{block.id}.json"))
            {
                block = JsonSerializer.Deserialize<Block>(File.ReadAllText(Environment.CurrentDirectory + $"\\block_{block.id}.json"));
            }
            else
            {
                block = await GetBlock(block.id);
            }
            return block;
        }

        static async Task<Chain> ReadChain()
        {
            Chain chain;
            if (false && File.Exists(Environment.CurrentDirectory + "\\chain_info.json"))
            {
                chain = JsonSerializer.Deserialize<Chain>(File.ReadAllText(Environment.CurrentDirectory + "\\chain_info.json"));
            }
            else
            {
                chain = await GetChain();
            }
            return chain;
        }

        static void SaveMinerDataToDisk(Block block, Chain chain)
        {
            var miners = block.transactions
                .Where(act => act._parsedTrx != null && act._parsedTrx.transaction.actions[0].name == "mine")
                .Select(trans => trans._parsedTrx);
            var jsonOut = JsonSerializer.Serialize(miners);
            File.WriteAllText($"{Environment.CurrentDirectory}\\miners_on_block_{chain.head_block_id}.json", jsonOut);
        }


        //static void ParseTransactions(Block block)
        //{
        //    var actions = (Transaction[]) block.transactions.ToArray().Clone();
        //    for (var x = 0; x < actions.Length; x++)
        //    {
        //        var action = actions[x];
        //        var trx = (JsonElement)action.trx;
                
        //        if (trx.ValueKind == JsonValueKind.String)
        //        {
        //            actions[x]._parsedId = trx.GetString();
        //        }
        //        else if (trx.ValueKind == JsonValueKind.Object) 
        //        {
        //            var element = trx.GetRawText();
        //            var copy = JsonSerializer.Deserialize<Trx>(element, _opts);
        //            actions[x]._parsedTrx = copy;
        //        }
        //        else
        //        {
        //            var error = JsonSerializer.Serialize(action);
        //            Console.WriteLine($"Unknown Transaction: {error}");
        //            File.WriteAllText(Environment.CurrentDirectory + $"\\{DateTime.Now}.fail-transaction", error);
        //        }
        //     }
        //    block.transactions = new List<Transaction>(actions);
        //}

        static async Task<Block> GetBlock(string blockId)
        {
            SetupClient();
            var body = new { block_num_or_id = blockId };
            var requestJson = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(@"https://chain.wax.io/v1/chain/get_block"),
                Method = HttpMethod.Get
            };
            //var json = await client.GetStringAsync(@"https://chain.wax.io/v1/chain/get_block");
            var location = Environment.CurrentDirectory + $"\\block_{blockId}.json";
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            File.WriteAllText(location, json);
            return JsonSerializer.Deserialize<Block>(json);
        }

        static async Task<string> GetRawBlock(string blockId)
        {
            SetupClient();
            var body = new { block_num_or_id = blockId };
            var requestJson = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(@"https://chain.wax.io/v1/chain/get_block"),
                Method = HttpMethod.Get
            };
            //var json = await client.GetStringAsync(@"https://chain.wax.io/v1/chain/get_block");
            var location = Environment.CurrentDirectory + $"\\block_{blockId}.json";
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return json;
        }

        static async Task<Chain> GetChain()
        {
            SetupClient();
            var json = await client.GetStringAsync(@"https://chain.wax.io/v1/chain/get_info");
            var location = Environment.CurrentDirectory + "\\chain_info.json";
            File.WriteAllText(location, json);
            return JsonSerializer.Deserialize<Chain>(json);
        }

        static void SetupClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.26.8");
            client.DefaultRequestHeaders.Add("Cookie", @"__cfduid=d12d36124612a89fba4d4db67ff7594341620269474; AWSELB=C17BB181189BA932E9703059DB2661260667E9BEB5143BBA6858E03DD386E7B9BC7A1E5DAD6E4ACA2BDC9ED4295E3FB3B831CEFE2FF02B2DDE4752AC70B26B808772CB5D56; AWSELBCORS=C17BB181189BA932E9703059DB2661260667E9BEB5143BBA6858E03DD386E7B9BC7A1E5DAD6E4ACA2BDC9ED4295E3FB3B831CEFE2FF02B2DDE4752AC70B26B808772CB5D56");
            client.DefaultRequestHeaders.Add("Accept", @"*/*");
            client.DefaultRequestHeaders.Add("Host", @"chain.wax.io");
            client.DefaultRequestHeaders.Add("Postman-Token", Guid.NewGuid().ToString());
        }
    }
}
