using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaxInterfaces
{
    public class Transaction
    {
        public string status { get; set; }
        public int cpu_usage_us { get; set; }
        public int net_usage_words { get; set; }
        public Trx trx { get; set; }
        public Trx _parsedTrx { get; set; }
        public string _parsedId { get; set; }
    }
    
    public class Block
    {
        public DateTime timestamp { get; set; }
        public string producer { get; set; }
        public int confirmed { get; set; }
        public string previous { get; set; }
        public string transaction_mroot { get; set; }
        public string action_mroot { get; set; }
        public int schedule_version { get; set; }
        public object new_producers { get; set; }
        public string producer_signature { get; set; }
        public List<Transaction> transactions { get; set; }
        public string id { get; set; }
        public int block_num { get; set; }
        public long ref_block_prefix { get; set; }
    }
}
