using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realizer.Messages.Conversations;
public sealed class GetReply : IHttpReportQuery<ThreadRow>
{
    public GetReply(Id versionId)
    {
        //Id.TryFromAny(threadId, out var id);
        //ThreadId = id;
        ThreadId = versionId;
    }
        

    public Id ThreadId { get; }
}
