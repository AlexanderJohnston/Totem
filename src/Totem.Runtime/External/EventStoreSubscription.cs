using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace Totem.External
{
    public abstract record EventStoreSubscription : SubscriptionLink
    {
        public string Uri { get; set; }
        public UserCredentials? Credentials { get; set; }
    }
}
