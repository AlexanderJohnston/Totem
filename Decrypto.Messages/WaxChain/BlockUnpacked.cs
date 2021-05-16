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
    public class BlockUnpacked : IEvent
    {
        public BlockUnpacked(Id id, Block unpackedBlock)
        {
            Id = id;
            UnpackedBlock = unpackedBlock;
        }

        [Required] public Id Id { get; set; } = null;
        [Required] public Block UnpackedBlock { get; set; } = null;

    }
}
