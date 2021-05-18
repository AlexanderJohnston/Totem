using System.ComponentModel.DataAnnotations;
using Totem;

namespace Decrypto.Blocks
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
