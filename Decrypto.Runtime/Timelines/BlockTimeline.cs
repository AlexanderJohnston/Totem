using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Messages.WaxChain;
using Totem;
using WaxInterfaces;

namespace Decrypto.Runtime.Timelines
{
    public class BlockTimeline : Timeline
    {
        public Id Id {get; set;}
        public Block Block {get; set; }
        public Dictionary<Id, Trx> Transactions { get; set; } = new Dictionary<Id, Trx>();

        public static Id DeriveId(Id versionId) =>
            versionId?.DeriveId(nameof(BlockTimeline)) ?? throw new ArgumentNullException(nameof(versionId));

        public BlockTimeline(Id id) : base(id)
        {
            Given<BlockUnpacked>(e =>
            {
                Block = e.UnpackedBlock;
                Id = e.Id;
            });
            Given<TransactionCreated>(e =>
            {
                Transactions.Add(e.TransactionId, e.Transaction);
            });
        }

        public async Task WhenAsync(UploadBlock command, CancellationToken cancel)
        {
            Then(new BlockCreated(command.Block, command.Id));
        }

        public async Task WhenAsync(UnpackBlock command, CancellationToken cancel)
        {
            var block = JsonSerializer.Deserialize<Block>(command.Block);
            Then(new BlockUnpacked(Id, block));
        }

        public async Task WhenAsync(NewTransaction command, CancellationToken cancel)
        {
            Then(new TransactionCreated(command.TransactionId, command.BlockId, command.Transaction));
        }

    }
}
