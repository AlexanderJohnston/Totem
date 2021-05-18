using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Totem;
using WaxInterfaces;

namespace Decrypto.Wallets.Timelines
{
    public class SmartContractTracker : Timeline
    {
        static Id _head = (Id) "9ccfe1d3-9ec1-483c-a1e0-67dac6ffce44";
        public static Id DeriveId(string blockId) =>
            _head?.DeriveId(blockId) ?? throw new ArgumentNullException(nameof(blockId));

        public SmartContractTracker(Id id) : base(id)
        {
            //Given<BlockUnpacked>(e =>
            //{
            //    Id = e.Id;
            //});
        }

        //public async Task WhenAsync(UploadBlock command, CancellationToken cancel)
        //{
        //    Then(new BlockCreated(command.Block, DeriveId(command.Id)));
        //}
    }
}
