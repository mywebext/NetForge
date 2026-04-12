// NetForge.Networking.Protocols/TcpIP.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Nodes;
using NetForge.Networking.Packets;
using System.Net;
using System.Net.Sockets;

namespace NetForge.Networking.Protocols;

public sealed class TcpIP : ProtocolBase
{
    private readonly object _lock = new();
    private TcpClient? _client;
    private NetworkStream? _stream;

    public override ProtocolTypes ProtocolType => ProtocolTypes.TcpIP;

    public bool NoDelay { get; set; } = true;

    public override async ValueTask ConnectAsync(Node node, CancellationToken cancellationToken = default)
    {
        ValidateNode(node);
        ThrowIfWrongProtocol(node, ProtocolType);

        lock (_lock)
        {
            if (_client is not null || _stream is not null)
                throw new InvalidOperationException("TCP/IP client is already initialized.");
        }

        if (RemotePort <= 0)
            throw new InvalidOperationException("RemotePort must be greater than 0 for TCP/IP.");

        TcpClient client = new();
        client.NoDelay = NoDelay;

        using CancellationTokenSource timeoutCts = new(ConnectTimeoutMs);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        await client.ConnectAsync(HostNameOrAddress, RemotePort, linkedCts.Token);

        NetworkStream stream = client.GetStream();
        stream.ReadTimeout = ReceiveTimeoutMs;
        stream.WriteTimeout = SendTimeoutMs;

        lock (_lock)
        {
            _client = client;
            _stream = stream;
        }

        if (client.Client.LocalEndPoint is IPEndPoint localEp)
            node.SetLocalEndPoint(localEp);

        if (client.Client.RemoteEndPoint is IPEndPoint remoteEp)
            node.SetRemoteEndPoint(remoteEp);
    }

    public override ValueTask DisconnectAsync(Node node, CancellationToken cancellationToken = default)
    {
        ValidateNode(node);
        ThrowIfWrongProtocol(node, ProtocolType);

        lock (_lock)
        {
            try
            {
                _stream?.Dispose();
            }
            finally
            {
                _stream = null;
            }

            try
            {
                _client?.Close();
                _client?.Dispose();
            }
            finally
            {
                _client = null;
            }
        }

        node.SetLocalEndPoint(null);
        node.SetRemoteEndPoint(null);

        return ValueTask.CompletedTask;
    }

    public override async ValueTask SendAsync(Node node, Packet packet, CancellationToken cancellationToken = default)
    {
        ValidateNode(node);
        ValidatePacket(packet);
        ThrowIfWrongProtocol(node, ProtocolType);
        ThrowIfNotConnected(node);

        NetworkStream stream = GetStream();

        UpdatePacketIdentity(node, packet);
        StampPacketTargetNodeType(node, packet);
        StampPacketRemoteEndPoint(node, packet);

        // TODO:
        // 1. Set packet.Header.PayloadLength from packet.Payload.Count
        // 2. Serialize PacketHeader into the fixed 64-byte wire format
        // 3. Compute CRC32 over header-with-zeroed-crc + payload
        // 4. Write final CRC32 into the header
        // 5. Build full outbound byte[] = packed header + payload
        byte[] outbound = PackPacket(packet);

        await stream.WriteAsync(outbound, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public override async ValueTask<Packet?> ReceiveAsync(Node node, CancellationToken cancellationToken = default)
    {
        ValidateNode(node);
        ThrowIfWrongProtocol(node, ProtocolType);
        ThrowIfNotConnected(node);

        NetworkStream stream = GetStream();

        // TODO:
        // 1. Read exact fixed-size 64-byte header bytes
        // 2. Parse early header fields
        // 3. Discard immediately on invalid target/signature/protocol
        // 4. Validate payload length sanity
        // 5. Read exact payload bytes
        // 6. Recompute and validate CRC32
        // 7. Create Packet instance from parsed data
        Packet? packet = await UnpackPacketAsync(stream, cancellationToken);

        if (packet is null)
            return null;

        if (ShouldDiscardPacket(node, packet))
            return null;

        if (node.RemoteEndPoint is not null && packet.RemoteEndPoint is null)
            packet.RemoteEndPoint = node.RemoteEndPoint;

        return packet;
    }

    private NetworkStream GetStream()
    {
        lock (_lock)
        {
            return _stream ?? throw new InvalidOperationException("TCP/IP stream is not available.");
        }
    }

    private byte[] PackPacket(Packet packet)
    {
        throw new NotImplementedException("TCP/IP packet packing has not been implemented yet.");
    }

    private ValueTask<Packet?> UnpackPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("TCP/IP packet unpacking has not been implemented yet.");
    }

    private static async ValueTask<byte[]> ReadExactAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length == 0)
            return Array.Empty<byte>();

        byte[] buffer = new byte[length];
        int offset = 0;

        while (offset < length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
                throw new IOException("Remote TCP/IP endpoint closed the connection.");

            offset += read;
        }

        return buffer;
    }
}