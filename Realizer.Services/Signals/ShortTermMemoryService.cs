using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memory.Converse;

namespace Realizer.Services.Signals;
public class ShortTermMemoryService : IShortTermMemory<string>
{
    ShortTermMemory<string> _memory = new ShortTermMemory<string>();

    public Guid Remember(string value, ushort userId, string userName, MemoryType type, ulong channel, string relatedContext = "")
    {
        return _memory.Remember(value, userId, userName, type, channel, relatedContext);
    }

    public List<Memory.Converse.Memory<string>> AllMemories(ulong channel)
    {
        return _memory.AllMemories(channel);
    }

    public string LastMemory(ushort userId)
    {
        return _memory.LastMemory(userId);
    }
}
