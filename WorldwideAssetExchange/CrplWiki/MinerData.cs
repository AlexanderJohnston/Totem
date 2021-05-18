using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrplWiki
{
    struct MinerData
    {
        public string Description;
        public string LandId;
        public string Time;
        public string Landowner;
        public string Miner;
        public string WaitTimeSeconds;
        public string Transaction;
        public string CommissionRate;
        public string Trilium;
        public string CommissionAmount;
        public AwTool FirstTool;
        public AwTool SecondTool;
        public AwTool ThirdTool;

        public MinerData(string[] args)
        {
            Description = args[1].Trim();
            LandId = args[2].Trim();
            Time = args[3].Trim();
            Landowner = args[4].Trim();
            Miner = args[5].Trim();
            WaitTimeSeconds = args[6].Trim();
            Transaction = args[7].Trim();
            CommissionRate = args[8].Trim();
            Trilium = args[9].Trim();
            CommissionAmount = args[10].Trim();
            FirstTool = new AwTool(args[11].Trim());
            SecondTool = new AwTool(args[12].Trim());
            ThirdTool = new AwTool(args[13].Trim());
        }

        public AwTool[] GetTools() => new[] { FirstTool, SecondTool, ThirdTool };
    }
}
