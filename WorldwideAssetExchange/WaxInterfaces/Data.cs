using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaxInterfaces
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Authorization
    {
        public string actor { get; set; }
        public string permission { get; set; }
    }

    public class Data
    {
        public string miner { get; set; }
        public string nonce { get; set; }
    }

    public class Action
    {
        public string account { get; set; }
        public string name { get; set; }
        public List<Authorization> authorization { get; set; }
        public Data data { get; set; }
        public string hex_data { get; set; }
    }

    public class Transact
    {
        public DateTime expiration { get; set; }
        public int ref_block_num { get; set; }
        public long ref_block_prefix { get; set; }
        public int max_net_usage_words { get; set; }
        public int max_cpu_usage_ms { get; set; }
        public int delay_sec { get; set; }
        public List<object> context_free_actions { get; set; }
        public List<Action> actions { get; set; }
    }

    public class Trx
    {
        public string id { get; set; }
        public List<string> signatures { get; set; }
        public string compression { get; set; }
        public string packed_context_free_data { get; set; }
        public List<object> context_free_data { get; set; }
        public string packed_trx { get; set; }
        public Transact transaction { get; set; }
    }
}
