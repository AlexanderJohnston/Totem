using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decrypto.Messages.WaxChain;
using Totem;
using WaxInterfaces;

namespace Decrypto.Runtime.Workflows
{
    public class BlockReader : Workflow
    {
        public static Id Route(BlockCreated e) => e.Id;

        Id _blockId;

        public BlockReader()
        {
            Given<BlockCreated>(e => _blockId = e.Id);
        }

        public void When(BlockCreated e)
        {
            if(e.Id != null && !string.IsNullOrWhiteSpace(e.Block))
            {
                ThenEnqueue(new UnpackBlock(e.Id, e.Block));
            }
        }

        public void When(BlockUnpacked e)
        {
            if(e.UnpackedBlock != null && e.Id != null)
            {
                foreach (var transaction in e.UnpackedBlock.transactions)
                {
                    if (WasParsed(transaction))
                    {
                        ThenEnqueue(new NewTransaction(Id.From(transaction._parsedId), e.Id, transaction._parsedTrx));
                    }
                }
            }
        }
        private bool WasParsed(Transaction transaction) => (transaction._parsedTrx != null || transaction.trx != null);

    }
}
