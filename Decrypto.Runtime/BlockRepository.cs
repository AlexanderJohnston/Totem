using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Messages.WaxChain.Records;
using Decrypto.Runtime.Contracts;
using Totem;

namespace Decrypto.Runtime
{
    public class BlockRepository : IBlockRepository
    {
        public Task<BlockRecord> GetBlockAsync(Id blockId, CancellationToken cancel) => _storage.GetValueAsync<BlockRecord>(PartitionKey, blockId, cancel);
        public Task SaveAsync(BlockRecord record, CancellationToken cancel) => _storage.PutAsync(PartitionKey, record.Id, record, cancel);

        const string PartitionKey = "Blocks";

        readonly IStorage _storage;

        public BlockRepository(IStorage storage) => _storage = _storage ?? throw new ArgumentNullException(nameof(storage));
    }
}
