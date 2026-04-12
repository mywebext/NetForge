//NetForge.Networking.Enums/ProtocolTypes.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Enums;

public enum ProtocolTypes : byte
{
    None,
    TcpIP,
    Udp,
    HTTP,
    FTP,
    ICMP
}
