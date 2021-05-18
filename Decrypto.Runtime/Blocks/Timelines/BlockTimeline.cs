using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Totem;
using WaxInterfaces;

namespace Decrypto.Blocks.Timelines
{
    public class BlockTimeline : Timeline
    {
        static Id _head = (Id) "9ccfe1d3-9ec1-483c-a1e0-67dac6ffce44";
        public Id Id { get; set; }
        public Block Block { get; set; }

        public static Id DeriveId(string blockId) =>
            _head?.DeriveId(blockId) ?? throw new ArgumentNullException(nameof(blockId));

        public BlockTimeline(Id id) : base(id)
        {
            Given<BlockUnpacked>(e =>
            {
                Block = e.UnpackedBlock;
                Id = e.Id;
            });
            //Given<TransactionsCreated>(e =>
            //{
            //    Transactions.Add(e.TransactionId, e.Transaction);
            //});
        }

        public async Task WhenAsync(UploadBlock command, CancellationToken cancel)
        {
            Then(new BlockCreated(command.Block, DeriveId(command.Id)));
        }

        public async Task WhenAsync(UnpackBlock command, CancellationToken cancel)
        {
            try
            {
                var block = JsonConvert.DeserializeObject<Block>(command.Block);
                Then(new BlockUnpacked(command.Id, block));
            }
            catch(Exception ex)
            {
                return;
            }
        }

        public async Task WhenAsync(NewTransactions command, CancellationToken cancel)
        {
            Then(new TransactionsCreated(command.BlockId, command.Transactions));
        }
    }
}
