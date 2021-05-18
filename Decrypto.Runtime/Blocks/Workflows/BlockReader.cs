using System;
using System.Collections.Generic;
using Totem;
using WaxInterfaces;

namespace Decrypto.Blocks.Workflows
{
    public class BlockReader : Workflow
    {
        static Id _head = (Id) "38e4a351-ccdc-4fa9-8eb6-af1a95135dfa";
        public static Id Route(BlockCreated e) => e.Id;
        public static Id Route(BlockUnpacked e) => e.Id;

        public static Id DeriveId(string transactionId) =>
            _head?.DeriveId(transactionId) ?? throw new ArgumentNullException(nameof(transactionId));

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
                var transactions = new List<Trx>();
                foreach(var transaction in e.UnpackedBlock.transactions)
                {
                    if(WasParsed(transaction))
                    {
                        transactions.Add(transaction.trx);
                    }
                }
                ThenEnqueue(new NewTransactions(e.Id, transactions));
            }
        }
        private bool WasParsed(Transaction transaction) => transaction._parsedTrx != null || transaction.trx != null;

    }
}
