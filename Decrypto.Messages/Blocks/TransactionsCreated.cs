using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Totem;
using WaxInterfaces;

namespace Decrypto.Blocks
{
    public class TransactionsCreated : IEvent
    {
        [Required] public readonly Id TransactionId;
        [Required] public readonly Id BlockId;
        [Required] public readonly IEnumerable<Trx> Transactions;

        public TransactionsCreated(Id blockId, IEnumerable<Trx> transactions)
        {
            BlockId = blockId;
            Transactions = transactions;
        }
    }
}
