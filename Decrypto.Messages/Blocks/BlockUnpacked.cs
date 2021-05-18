using System.ComponentModel.DataAnnotations;
using Totem;
using WaxInterfaces;

namespace Decrypto.Blocks
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
