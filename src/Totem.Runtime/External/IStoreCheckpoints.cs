using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.External
{
    
    public interface IStoreCheckpoints
    {
        ValueTask<Checkpoint> GetLastCheckpoint(Id checkpointId, CancellationToken cancellationToken);

        ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken);
    }
}
