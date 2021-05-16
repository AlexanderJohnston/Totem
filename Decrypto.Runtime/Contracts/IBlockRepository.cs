using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Messages.WaxChain.Records;
using Totem;

namespace Decrypto.Runtime.Contracts
{
    public interface IBlockRepository
    {
        Task<BlockRecord?> GetBlockAsync(Id blockId, CancellationToken cancel);
        Task SaveAsync(BlockRecord record, CancellationToken cancel);
    }
}
