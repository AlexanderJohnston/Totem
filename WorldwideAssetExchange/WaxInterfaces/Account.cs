using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaxInterfaces
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class NetLimit
    {
        public int used { get; set; }
        public int available { get; set; }
        public int max { get; set; }
    }

    public class CpuLimit
    {
        public int used { get; set; }
        public int available { get; set; }
        public int max { get; set; }
    }

    public class Key
    {
        public string key { get; set; }
        public int weight { get; set; }
    }

    public class Permission2
    {
        public string actor { get; set; }
        public string permission { get; set; }
    }

    public class Account
    {
        public Permission permission { get; set; }
        public int weight { get; set; }
    }

    public class RequiredAuth
    {
        public int threshold { get; set; }
        public List<Key> keys { get; set; }
        public List<Account> accounts { get; set; }
        public List<object> waits { get; set; }
    }

    public class Permission
    {
        public string perm_name { get; set; }
        public string parent { get; set; }
        public RequiredAuth required_auth { get; set; }
    }

    public class TotalResources
    {
        public string owner { get; set; }
        public string net_weight { get; set; }
        public string cpu_weight { get; set; }
        public int ram_bytes { get; set; }
    }

    public class SelfDelegatedBandwidth
    {
        public string from { get; set; }
        public string to { get; set; }
        public string net_weight { get; set; }
        public string cpu_weight { get; set; }
    }

    public class VoterInfo
    {
        public string owner { get; set; }
        public string proxy { get; set; }
        public List<object> producers { get; set; }
        public object staked { get; set; }
        public string unpaid_voteshare { get; set; }
        public DateTime unpaid_voteshare_last_updated { get; set; }
        public string unpaid_voteshare_change_rate { get; set; }
        public DateTime last_claim_time { get; set; }
        public string last_vote_weight { get; set; }
        public string proxied_vote_weight { get; set; }
        public int is_proxy { get; set; }
        public int flags1 { get; set; }
        public int reserved2 { get; set; }
        public string reserved3 { get; set; }
    }

    public class WaxAccount
    {
        public string account_name { get; set; }
        public int head_block_num { get; set; }
        public DateTime head_block_time { get; set; }
        public bool privileged { get; set; }
        public DateTime last_code_update { get; set; }
        public DateTime created { get; set; }
        public string core_liquid_balance { get; set; }
        public int ram_quota { get; set; }
        public int net_weight { get; set; }
        public object cpu_weight { get; set; }
        public NetLimit net_limit { get; set; }
        public CpuLimit cpu_limit { get; set; }
        public int ram_usage { get; set; }
        public List<Permission> permissions { get; set; }
        public object total_resources { get; set; }
        public object self_delegated_bandwidth { get; set; }
        public object refund_request { get; set; }
        public VoterInfo voter_info { get; set; }
        public object rex_info { get; set; }
    }


}
