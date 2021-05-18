using System;
using System.Threading;
using System.Threading.Tasks;
using Decrypto.Blocks.Timelines;
using Totem;

namespace Decrypto.Blocks.Handlers
{
    public class NewTransactionHandler : IQueueHandler<NewTransactions>
    {
        readonly ITimelineRepository<BlockTimeline> _blocks;
        public NewTransactionHandler(ITimelineRepository<BlockTimeline> blocks)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }

        public async Task HandleAsync(IQueueContext<NewTransactions> context, CancellationToken cancel)
        {
            var timelineId = BlockTimeline.DeriveId(context.Command.BlockId);
            var timeline = await _blocks.LoadAsync(timelineId, cancel);
            await timeline.WhenAsync(context.Command, cancel);
            if(timeline.HasErrors)
            {
                context.AddErrors(timeline.Errors);
                return;
            }
            await _blocks.SaveAsync(timeline, context.CorrelationId, context.Principal, cancel);
        }
    }
}
