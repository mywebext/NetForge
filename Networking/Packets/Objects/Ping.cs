using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Packets.Objects;

public class Ping
{
    public ulong Nonce { get; set; }
    public int PayloadBytes { get; set; }
    public long SentUtcTicks { get; set; }
}
