//NetForge.Networking.Enums/TypeScopes.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Enums;

public enum TypeScopes :byte
{
    None,
    Session,
    Status,
    File,
    Path,
    Window,
    Client,
    Host,
    Server,
    System,
    Network,
    Game,
    Login,
    Character,
    Player,
    Manifest,
    Key
}
