using System.ComponentModel.DataAnnotations;
using Totem;

namespace Decrypto.Blocks
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
