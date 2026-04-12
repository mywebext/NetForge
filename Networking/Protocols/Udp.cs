// NetForge.Networking.Protocols/Udp.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Nodes;
using NetForge.Networking.Packets;

namespace NetForge.Networking.Protocols;

public sealed class Udp : IProtocol
{
    public ProtocolTypes ProtocolType => ProtocolTypes.Udp;

    public ValueTask ConnectAsync(Node node, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("UDP connect has not been implemented yet.");
    }

    public ValueTask DisconnectAsync(Node node, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("UDP disconnect has not been implemented yet.");
    }

    public ValueTask SendAsync(Node node, Packet packet, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("UDP send has not been implemented yet.");
    }

    public ValueTask<Packet?> ReceiveAsync(Node node, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("UDP receive has not been implemented yet.");
    }
}