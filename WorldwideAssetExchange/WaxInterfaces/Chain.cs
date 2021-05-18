using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaxInterfaces
{
    public class Chain
    {
        public string server_version { get; set; }
        public string chain_id { get; set; }
        public int head_block_num { get; set; }
        public int last_irreversible_block_num { get; set; }
        public string last_irreversible_block_id { get; set; }
        public string head_block_id { get; set; }
        public DateTime head_block_time { get; set; }
        public string head_block_producer { get; set; }
        public int virtual_block_cpu_limit { get; set; }
        public int virtual_block_net_limit { get; set; }
        public int block_cpu_limit { get; set; }
        public int block_net_limit { get; set; }
        public string server_version_string { get; set; }
        public int fork_db_head_block_num { get; set; }
        public string fork_db_head_block_id { get; set; }
        public string server_full_version_string { get; set; }
    }
}
