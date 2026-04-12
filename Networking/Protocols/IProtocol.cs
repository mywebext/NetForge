// NetForge.Networking.Protocols/IProtocol.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Nodes;
using NetForge.Networking.Packets;

namespace NetForge.Networking.Protocols;

public interface IProtocol
{
    ProtocolTypes ProtocolType { get; }

    ValueTask ConnectAsync(Node node, CancellationToken cancellationToken = default);
    ValueTask DisconnectAsync(Node node, CancellationToken cancellationToken = default);
    ValueTask SendAsync(Node node, Packet packet, CancellationToken cancellationToken = default);
    ValueTask<Packet?> ReceiveAsync(Node node, CancellationToken cancellationToken = default);
}
