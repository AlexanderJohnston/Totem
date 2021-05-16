using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Totem;
using WaxInterfaces;

namespace Decrypto.Messages.WaxChain
{
    public class TransactionCreated : IEvent
    {
        [Required] public readonly Id TransactionId;
        [Required] public readonly Id BlockId;
        [Required] public readonly Trx Transaction;

        public TransactionCreated(Id transactionId, Id blockId, Trx transaction)
        {
            TransactionId = transactionId;
            BlockId = blockId;
            Transaction = transaction;
        }
    }
}
