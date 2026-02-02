using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dream.Test;
using Totem.Core;

namespace Dream.Versions.Topics; 
public sealed class EchoTopic : Topic
{
    public async Task When(EchoBounce command, CancellationToken cancellationToken)
    {
        var id = TimelineId.DeriveId(command.Test);
        Then(new EchoBounced(id, command.Test));
    }
}
