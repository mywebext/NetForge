//NetForge.Networking.Enums/NodeTypes.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Enums;

public enum NodeTypes : byte
{
    Client = 1,
    Host = 2,
    System = 3,
    Seeder = 4
}