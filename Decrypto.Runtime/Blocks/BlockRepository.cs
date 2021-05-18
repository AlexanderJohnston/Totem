using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Blocks.Contracts;
using Decrypto.Records;
using Totem;

namespace Decrypto.Blocks
{
    public class BlockRepository : IBlockRepository
    {
        public Task<BlockRecord> GetBlockAsync(Id blockId, CancellationToken cancel) => _storage.GetValueAsync<BlockRecord>(PartitionKey, blockId, cancel);
        public Task SaveAsync(BlockRecord record, CancellationToken cancel) => _storage.PutAsync(PartitionKey, record.Id, record, cancel);

        const string PartitionKey = "Blocks";

        readonly IStorage _storage;

        public BlockRepository(IStorage storage) => _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }
}
