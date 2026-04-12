//NetForge.Networking.Nodes/System.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Protocols;

namespace NetForge.Networking.Nodes;

public sealed class System : Node
{
    public System(IProtocol protocol) : base(NodeTypes.System, protocol) { }
}