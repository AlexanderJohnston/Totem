using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Memory.Converse;

namespace Realizer.Services.Signals;
public interface IShortTermMemory<T>
{
    public Guid Remember(T value, ushort userId, string userName, MemoryType type, ulong channel, string relatedContext = "");
    public List<Memory.Converse.Memory<T>> AllMemories(ulong channel);
    public string LastMemory(ushort userId);
}
