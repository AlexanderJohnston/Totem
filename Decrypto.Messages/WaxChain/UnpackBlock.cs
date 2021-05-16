using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Totem;

namespace Decrypto.Messages.WaxChain
{
    [QueueName("wax-chain")]
    public class UnpackBlock : IQueueCommand
    {
        public UnpackBlock(Id id, string block)
        {
            Id = id;
            Block = block;
        }

        [Required] public Id Id { get; set; } = null;
        [Required] public string Block { get; set; } = string.Empty;
    }
}
