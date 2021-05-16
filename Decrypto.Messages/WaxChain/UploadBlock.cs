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
    public class UploadBlock : IQueueCommand
    {
        [Required] public Id Id { get; set; } = null;
        [Required] public string Block { get; set; } = string.Empty;
    }
}
