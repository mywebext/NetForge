//NetForge.Networking.Nodes/Seeder.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Protocols;

namespace NetForge.Networking.Nodes;

public sealed class Seeder : Node
{
    public Seeder(IProtocol protocol) : base(NodeTypes.Seeder, protocol) { }
}