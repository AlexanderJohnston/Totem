using System;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Assets.Contracts;
using Totem;

namespace Decrypto.Assets.Reports
{
    public class NftReport : Report
    {
        //public static Id Route(BlockCreated e) => e.Id;

        readonly IStoreNft _blocks;

        public NftReport(IStoreNft blocks) => _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));

        //public async Task WhenAsync(BlockCreated e, CancellationToken cancel)
        //{
        //    await _blocks.SaveAsync(new BlockRecord
        //    {
        //        Id = e.Id,
        //        BlockBeforeParsing = e.Block
        //    }, cancel);
        //}
    }
}
