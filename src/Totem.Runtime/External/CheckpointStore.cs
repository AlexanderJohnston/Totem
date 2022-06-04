using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.External
{
    public class CheckpointStore : IStoreCheckpoints
    {
        public ValueTask<Checkpoint> GetLastCheckpoint(Id checkpointId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
