//NetForge.Networking.Nodes/Node.cs
using NetForge.Networking;
using NetForge.Networking.Enums;
using NetForge.Networking.Managers;
using NetForge.Networking.Packets;
using NetForge.Networking.Packets.Library;
using NetForge.Networking.Protocols;
using NetForge.Security.Encryption;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetForge.Networking.Nodes;

public abstract class Node
{
    private readonly object _lock = new();
    private readonly Dictionary<Algorithms, byte[]> _encryptionKeys;

    public SessionManager SessionManager { get; }
    public AckManager AckManager { get; }
    public Packetlib PacketLibrary { get; }

    protected Node(NodeTypes nodeType, IProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        NodeType = nodeType;
        Protocol = protocol;
        Signature = PacketSignatures.GetDefaultSignature(nodeType);

        SessionManager = new SessionManager(this);
        AckManager = new AckManager();
        PacketLibrary = new Packetlib001(this);

        _encryptionKeys = new Dictionary<Algorithms, byte[]>();
        CreatedUtc = DateTime.UtcNow;

        GenerateEncryptionKeys();
    }

    public sealed class NodeStateSnapshot
    {
        public required NodeTypes NodeType { get; init; }
        public required uint Signature { get; init; }
        public required ProtocolTypes ProtocolType { get; init; }
        public required bool IsConnected { get; init; }
        public required ulong InstanceId { get; init; }
        public required ulong NextMessageId { get; init; }
        public IPEndPoint? LocalEndPoint { get; init; }
        public IPEndPoint? RemoteEndPoint { get; init; }
        public required DateTime CreatedUtc { get; init; }
        public required DateTime LastSendUtc { get; init; }
        public required DateTime LastReceiveUtc { get; init; }
    }

    public NodeTypes NodeType { get; }
    public uint Signature { get; }
    public IProtocol Protocol { get; private set; }

    public ProtocolTypes ProtocolType
    {
        get
        {
            lock (_lock)
            {
                return Protocol.ProtocolType;
            }
        }
    }

    public bool IsConnected { get; private set; }

    public ulong InstanceId { get; private set; } = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public ulong NextMessageId { get; private set; }

    public IPEndPoint? LocalEndPoint { get; private set; }
    public IPEndPoint? RemoteEndPoint { get; private set; }

    public DateTime CreatedUtc { get; }
    public DateTime LastSendUtc { get; private set; }
    public DateTime LastReceiveUtc { get; private set; }

    protected virtual void GenerateEncryptionKeys()
    {
        lock (_lock)
        {
            _encryptionKeys[Algorithms.None] = Array.Empty<byte>();
            _encryptionKeys[Algorithms.AESGcm] = AESGcmEncryption.GenerateKey(32);
            _encryptionKeys[Algorithms.RC4] = RC4.GenerateKey();
            _encryptionKeys[Algorithms.ChaCha20Poly1305] = ChaCha20Encryption.GenerateKey();
        }
    }

    public NodeStateSnapshot GetStateSnapshot()
    {
        lock (_lock)
        {
            return new NodeStateSnapshot
            {
                NodeType = NodeType,
                Signature = Signature,
                ProtocolType = Protocol.ProtocolType,
                IsConnected = IsConnected,
                InstanceId = InstanceId,
                NextMessageId = NextMessageId,
                LocalEndPoint = LocalEndPoint,
                RemoteEndPoint = RemoteEndPoint,
                CreatedUtc = CreatedUtc,
                LastSendUtc = LastSendUtc,
                LastReceiveUtc = LastReceiveUtc
            };
        }
    }

    public virtual void SetProtocol(IProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        lock (_lock)
        {
            if (IsConnected)
                throw new InvalidOperationException("Cannot change protocol while the node is connected.");

            Protocol = protocol;
        }
    }

    public bool TryGetEncryptionKey(Algorithms type, out byte[]? key)
    {
        lock (_lock)
        {
            if (!_encryptionKeys.TryGetValue(type, out byte[]? existing))
            {
                key = null;
                return false;
            }

            key = [.. existing];
            return true;
        }
    }

    public void SetEncryptionKey(Algorithms type, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            _encryptionKeys[type] = [.. key];
        }
    }

    public bool HasEncryptionKey(Algorithms type)
    {
        lock (_lock)
        {
            return _encryptionKeys.ContainsKey(type);
        }
    }

    public bool RemoveEncryptionKey(Algorithms type)
    {
        lock (_lock)
        {
            return _encryptionKeys.Remove(type);
        }
    }

    internal ulong GetNextMessageId()
    {
        lock (_lock)
        {
            NextMessageId++;
            return NextMessageId;
        }
    }

    public void SetInstanceId(ulong instanceId)
    {
        lock (_lock)
        {
            InstanceId = instanceId;
        }
    }

    public void SetLocalEndPoint(IPEndPoint? endPoint)
    {
        lock (_lock)
        {
            LocalEndPoint = endPoint;
        }
    }

    public void SetRemoteEndPoint(IPEndPoint? endPoint)
    {
        lock (_lock)
        {
            RemoteEndPoint = endPoint;
        }
    }

    protected void MarkConnected(IPEndPoint? localEndPoint = null, IPEndPoint? remoteEndPoint = null)
    {
        lock (_lock)
        {
            IsConnected = true;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }
    }

    protected void MarkDisconnected()
    {
        lock (_lock)
        {
            IsConnected = false;
            LocalEndPoint = null;
            RemoteEndPoint = null;
        }
    }

    protected void MarkSent()
    {
        lock (_lock)
        {
            LastSendUtc = DateTime.UtcNow;
        }
    }

    protected void MarkReceived(IPEndPoint? remoteEndPoint = null)
    {
        lock (_lock)
        {
            LastReceiveUtc = DateTime.UtcNow;

            if (remoteEndPoint is not null)
                RemoteEndPoint = remoteEndPoint;
        }
    }

    public virtual async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        IProtocol protocol;

        lock (_lock)
        {
            protocol = Protocol;
        }

        await protocol.ConnectAsync(this, cancellationToken);
        MarkConnected(LocalEndPoint, RemoteEndPoint);
    }

    public virtual async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IProtocol protocol;

        lock (_lock)
        {
            protocol = Protocol;
        }

        await protocol.DisconnectAsync(this, cancellationToken);
        MarkDisconnected();
    }

    public virtual async ValueTask SendAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        IProtocol protocol;

        lock (_lock)
        {
            protocol = Protocol;
        }

        await protocol.SendAsync(this, packet, cancellationToken);
        MarkSent();
    }

    public virtual async ValueTask<Packet?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        IProtocol protocol;

        lock (_lock)
        {
            protocol = Protocol;
        }

        Packet? packet = await protocol.ReceiveAsync(this, cancellationToken);

        if (packet is not null)
            MarkReceived(packet.RemoteEndPoint);

        return packet;
    }
}