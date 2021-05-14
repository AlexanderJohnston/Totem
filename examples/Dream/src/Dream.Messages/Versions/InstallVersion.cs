using System;
using System.ComponentModel.DataAnnotations;
using Totem;

namespace Dream.Versions
{
    [PostRequest("/versions")]
    public class InstallVersion : ICommand
    {
        [Required] public Uri ZipUrl { get; set; } = null!;
    }
}