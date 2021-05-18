using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Totem;
using WaxInterfaces;

namespace Decrypto.Blocks
{
    public class NewTransactions : IQueueCommand
    {
        [Required] public readonly Id BlockId;
        [Required] public readonly IEnumerable<Trx> Transactions;

        public NewTransactions(Id blockId, IEnumerable<Trx> transactions)
        {
            BlockId = blockId;
            Transactions = transactions;
        }
    }
}
