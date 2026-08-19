using System.Collections.Generic;
using System.Threading.Channels;

namespace Ticketfy.Services.Implementations;

public class CoOccurrenceQueue
{
    public Channel<List<string>> Queue { get; } = Channel.CreateUnbounded<List<string>>();
}
