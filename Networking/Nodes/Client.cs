// NetForge.Networking.Nodes/Client.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Protocols;

namespace NetForge.Networking.Nodes;

public sealed class Client : Node
{
    public Client(IProtocol protocol) : base(NodeTypes.Client, protocol) { }
}