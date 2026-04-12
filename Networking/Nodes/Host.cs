// NetForge.Networking.Nodes/Host.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Protocols;

namespace NetForge.Networking.Nodes;

public sealed class Host : Node
{
    public Host(IProtocol protocol) : base(NodeTypes.Host, protocol) { }
}