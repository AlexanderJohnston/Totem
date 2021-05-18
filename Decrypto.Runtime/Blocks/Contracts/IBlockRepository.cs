using System.Threading;
using System.Threading.Tasks;
using Decrypto.Records;
using Totem;

namespace Decrypto.Blocks.Contracts
{
    public interface IBlockRepository
    {
        Task<BlockRecord> GetBlockAsync(Id blockId, CancellationToken cancel);
        Task SaveAsync(BlockRecord record, CancellationToken cancel);
    }
}
