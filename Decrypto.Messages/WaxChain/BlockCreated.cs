using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Totem;

namespace Decrypto.Messages.WaxChain
{
    public class BlockCreated : IEvent
    {
        [Required] public readonly string Block;
        [Required] public readonly Id Id;

        public BlockCreated(string block, Id id)
        {
            Block = block;
            Id = id;
        }
    }
}
