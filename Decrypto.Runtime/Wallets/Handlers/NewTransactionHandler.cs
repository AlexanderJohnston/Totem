using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Blocks.Timelines;
using Totem;

namespace Decrypto.Wallets.Handlers
{
    //public class NftSeen : IQueueHandler<NewTransactions>
    //{
    //    readonly ITimelineRepository<SmartContractTracker> _blocks;
    //    public NftSeen(ITimelineRepository<SmartContractTracker> blocks)
    //    {
    //        _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
    //    }

    //    public async Task HandleAsync(IQueueContext<NewTransactions> context, CancellationToken cancel)
    //    {
    //        var timelineId = SmartContractTracker.DeriveId(context.Command.BlockId);
    //        var timeline = await _blocks.LoadAsync(timelineId, cancel);
    //        await timeline.WhenAsync(context.Command, cancel);
    //        if(timeline.HasErrors)
    //        {
    //            context.AddErrors(timeline.Errors);
    //            return;
    //        }
    //        await _blocks.SaveAsync(timeline, context.CorrelationId, context.Principal, cancel);
    //    }
    //}
}
