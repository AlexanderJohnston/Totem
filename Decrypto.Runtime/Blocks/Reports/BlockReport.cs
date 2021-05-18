using System;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Blocks.Contracts;
using Decrypto.Records;
using Totem;

namespace Decrypto.Blocks.Reports
{
    public class BlockReport : Report
    {
        public static Id Route(BlockCreated e) => e.Id;
        public static Id Route(BlockUnpacked e) => e.Id;
        public static Id Route(TransactionsCreated e) => e.BlockId;

        readonly IBlockRepository _blocks;

        public BlockReport(IBlockRepository blocks) => _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));

        public async Task WhenAsync(BlockCreated e, CancellationToken cancel)
        {
            await _blocks.SaveAsync(new BlockRecord
            {
                Id = e.Id,
                BlockBeforeParsing = e.Block
            }, cancel);
        }

        public async Task WhenAsync(BlockUnpacked e, CancellationToken cancel)
        {
            var block = await _blocks.GetBlockAsync(e.Id, cancel);
            if(block == null)
            {
                ThenError(new(nameof(BlockReport), ErrorLevel.NotFound));
                return;
            }
            block.BlockAfterParsing = e.UnpackedBlock;
            await _blocks.SaveAsync(block, cancel);
        }

        public async Task WhenAsync(TransactionsCreated e, CancellationToken cancel)
        {
            var block = await _blocks.GetBlockAsync(e.BlockId, cancel);
            if(block == null)
            {
                ThenError(new(nameof(BlockReport), ErrorLevel.NotFound));
                return;
            }
            block.Transactions.AddRange(e.Transactions);
            await _blocks.SaveAsync(block, cancel);
        }
    }
}
