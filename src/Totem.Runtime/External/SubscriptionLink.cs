using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.External
{
    public record SubscriptionLink
    {
        public string SubscriptionId { get; set; } = null!;
        public bool ThrowOnError { get; set; }
    }
}
