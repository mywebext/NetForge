// NetForge.Networking.Protocols/ProtocolBase.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Nodes;
using NetForge.Networking.Packets;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetForge.Networking.Protocols;

public abstract class ProtocolBase : IProtocol
{
    public abstract ProtocolTypes ProtocolType { get; }

    public string HostNameOrAddress { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; }
    public int RemotePort { get; set; }
    public int ConnectTimeoutMs { get; set; } = 10000;
    public int SendTimeoutMs { get; set; } = 30000;
    public int ReceiveTimeoutMs { get; set; } = 30000;
    public int MaxPacketSize { get; set; } = 1024 * 1024;

    public virtual EndPoint? BuildRemoteEndPoint()
    {
        if (RemotePort <= 0)
            return null;

        if (!IPAddress.TryParse(HostNameOrAddress, out IPAddress? ip))
            return null;

        return new IPEndPoint(ip, RemotePort);
    }

    public abstract ValueTask ConnectAsync(Node node, CancellationToken cancellationToken = default);
    public abstract ValueTask DisconnectAsync(Node node, CancellationToken cancellationToken = default);
    public abstract ValueTask SendAsync(Node node, Packet packet, CancellationToken cancellationToken = default);
    public abstract ValueTask<Packet?> ReceiveAsync(Node node, CancellationToken cancellationToken = default);

    protected static void ValidateNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
    }

    protected static void ValidatePacket(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
    }

    protected static void ThrowIfWrongProtocol(Node node, ProtocolTypes expectedProtocol)
    {
        ValidateNode(node);

        if (node.ProtocolType != expectedProtocol)
            throw new InvalidOperationException(
                $"Node protocol mismatch. Expected {expectedProtocol}, but node is using {node.ProtocolType}.");
    }

    protected static void ThrowIfNotConnected(Node node)
    {
        ValidateNode(node);

        if (!node.IsConnected)
            throw new InvalidOperationException("Node is not connected.");
    }

    protected static void ThrowIfRemoteEndPointMissing(Node node)
    {
        ValidateNode(node);

        if (node.RemoteEndPoint is null)
            throw new InvalidOperationException("RemoteEndPoint is not set on the node.");
    }

    protected static void ThrowIfPacketRemoteEndPointMissing(Packet packet)
    {
        ValidatePacket(packet);

        if (packet.RemoteEndPoint is null)
            throw new InvalidOperationException("RemoteEndPoint is not set on the packet.");
    }

    protected static IPEndPoint ResolveRemoteEndPoint(Node node, Packet? packet = null)
    {
        ValidateNode(node);

        if (packet is not null && packet.RemoteEndPoint is not null)
            return packet.RemoteEndPoint;

        if (node.RemoteEndPoint is not null)
            return node.RemoteEndPoint;

        throw new InvalidOperationException("No remote endpoint is available on the packet or node.");
    }

    protected static byte[] GetPayloadBytes(Packet packet)
    {
        ValidatePacket(packet);

        if (packet.Payload.Array is null || packet.Payload.Count == 0)
            return Array.Empty<byte>();

        return packet.Payload.AsSpan().ToArray();
    }

    protected static void SetPayload(Packet packet, byte[] payload)
    {
        ValidatePacket(packet);
        ArgumentNullException.ThrowIfNull(payload);

        packet.Payload = new ArraySegment<byte>(payload);
    }

    protected static void UpdatePacketIdentity(Node node, Packet packet)
    {
        ValidateNode(node);
        ValidatePacket(packet);

        packet.Header.SourceNodeType = node.NodeType;
        packet.Header.Protocol = node.ProtocolType;
        packet.Header.SenderInstanceId = node.InstanceId;
        packet.Header.Signature = node.Signature;

        if (packet.Header.MessageId == 0)
            packet.Header.MessageId = node.GetNextMessageId();
    }

    protected static void StampPacketRemoteEndPoint(Node node, Packet packet)
    {
        ValidateNode(node);
        ValidatePacket(packet);

        if (packet.RemoteEndPoint is null && node.RemoteEndPoint is not null)
            packet.RemoteEndPoint = node.RemoteEndPoint;
    }

    protected static void StampPacketTargetNodeType(Node node, Packet packet)
    {
        ValidateNode(node);
        ValidatePacket(packet);

        if (!PacketSignatures.IsDefinedNodeType(packet.Header.TargetNodeType))
            throw new InvalidOperationException("Packet TargetNodeType must be explicitly set before sending.");
    }

    protected static bool ShouldDiscardUnknownSignature(Packet packet)
    {
        ValidatePacket(packet);
        return !PacketSignatures.IsFileEngineSignature(packet.Header.Signature);
    }

    protected static bool ShouldDiscardUndefinedTargetNodeType(Packet packet)
    {
        ValidatePacket(packet);
        return !PacketSignatures.IsDefinedNodeType(packet.Header.TargetNodeType);
    }

    protected static bool ShouldDiscardWrongTargetNodeType(Node node, Packet packet)
    {
        ValidateNode(node);
        ValidatePacket(packet);

        return packet.Header.TargetNodeType != node.NodeType;
    }

    protected static bool ShouldDiscardWrongProtocol(Packet packet, ProtocolTypes expectedProtocol)
    {
        ValidatePacket(packet);
        return packet.Header.Protocol != expectedProtocol;
    }

    protected static bool ShouldDiscardPacket(Node node, Packet packet)
    {
        ValidateNode(node);
        ValidatePacket(packet);

        return ShouldDiscardUndefinedTargetNodeType(packet) ||
               ShouldDiscardUnknownSignature(packet) ||
               ShouldDiscardWrongTargetNodeType(node, packet) ||
               ShouldDiscardWrongProtocol(packet, node.ProtocolType);
    }
}