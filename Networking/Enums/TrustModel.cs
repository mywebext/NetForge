//NetForge.Networking.Enums/TrustModel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Enums;

public enum TrustModel
{
    None = 0,
    HandShaking = 10,
    Basic = 20,
    Validated = 50,

    Trusted = 70,
    Elevated = 90,
    Authenticated = 100
}
