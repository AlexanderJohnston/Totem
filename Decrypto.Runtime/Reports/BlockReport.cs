using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Messages.WaxChain;
using Decrypto.Messages.WaxChain.Records;
using Decrypto.Runtime.Contracts;
using Totem;

namespace Decrypto.Runtime.Reports
{
    public class BlockReport : Report
    {
        public static Id Route(BlockCreated e) => e.Id;
        public static Id Route(BlockUnpacked e) => e.Id;
        public static Id Route(TransactionCreated e) => e.BlockId;

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
            if (block == null)
            {
                ThenError(new(nameof(BlockReport), ErrorLevel.NotFound));
                return;
            }
            block.BlockAfterParsing = e.UnpackedBlock;
            await _blocks.SaveAsync(block, cancel);
        }

        public async Task WhenAsync(TransactionCreated e, CancellationToken cancel)
        {
            var block = await _blocks.GetBlockAsync(e.BlockId, cancel);
            if(block == null)
            {
                ThenError(new(nameof(BlockReport), ErrorLevel.NotFound));
                return;
            }
            block.Transactions.Add(e.Transaction);
            await _blocks.SaveAsync(block, cancel);
        }
    }
}
