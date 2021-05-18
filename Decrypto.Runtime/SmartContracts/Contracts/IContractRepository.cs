using System.Threading;
using System.Threading.Tasks;
using Decrypto.Records;
using Totem;

namespace Decrypto.SmartContracts.Contracts
{
    public interface IContractRepository
    {
        Task<BlockRecord> GetBlockAsync(Id blockId, CancellationToken cancel);
        Task SaveAsync(BlockRecord record, CancellationToken cancel);
    }
}
