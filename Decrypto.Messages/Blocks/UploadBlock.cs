using System.ComponentModel.DataAnnotations;
using Totem;

namespace Decrypto.Blocks
{
    [PutRequest("/block")]
    public class UploadBlock : ICommand
    {
        [Required] public string Id { get; set; } = string.Empty;
        [Required] public string Block { get; set; } = string.Empty;
    }
}
