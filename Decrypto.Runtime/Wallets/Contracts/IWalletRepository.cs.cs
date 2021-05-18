using System.Threading;
using System.Threading.Tasks;
using Decrypto.Records;
using Totem;

namespace Decrypto.Wallets.Contracts
{
    public interface IWalletRepository
    {
        Task<BlockRecord> GetBlockAsync(Id blockId, CancellationToken cancel);
        Task SaveAsync(BlockRecord record, CancellationToken cancel);
    }
}
