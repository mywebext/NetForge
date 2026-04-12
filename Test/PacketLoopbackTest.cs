// Network.Tests/PacketLoopbackTest.cs
using NetForge.Networking.Enums;
using NetForge.Networking.Nodes;
using NetForge.Networking.Packets;
using NetForge.Networking.Protocols;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetForge.Test;

public static class PacketLoopbackTest
{
    public static ValueTask RunAsync()
    {
        Console.WriteLine("=== PacketLoopbackTest ===");

        TestHeaderRoundTrip();
        TestWrongTargetDiscard();

        Console.WriteLine("PacketLoopbackTest completed successfully.");
        return ValueTask.CompletedTask;
    }

    private static void TestHeaderRoundTrip()
    {
        var protocol = new TestProtocol();
        var sender = new TestNode(NodeTypes.Client, protocol);
        var receiver = new TestNode(NodeTypes.Host, protocol);

        byte[] payload = [1, 2, 3, 4, 5];

        var packet = new Packet
        {
            Header = new PacketHeader
            {
                TargetNodeType = NodeTypes.Host,
                Flags = PacketFlags.AckRequired,
                Encryption = Algorithms.None,
                TypeScope = TypeScopes.Session,
                TypeCommand = 0,
                SessionId = 100,
                AckForMessageId = 0,
                RoutingKey = 200
            },
            Payload = new ArraySegment<byte>(payload)
        };

        protocol.Stamp(sender, packet);
        packet.Header.PayloadLength = (ulong)packet.Payload.Count;

        byte[] wireHeader = PacketHeaderWriter.WriteHeader(packet.Header);

        Require(PacketHeaderReader.HeaderSize == 64, "HeaderSize should be 64.");
        Require(wireHeader.Length == 64, "Written header length should be 64.");

        bool ok = PacketHeaderReader.TryParseHeader(wireHeader, out PacketHeader? parsed);
        Require(ok, "Header parse should succeed.");
        Require(parsed is not null, "Parsed header should not be null.");

        Require(parsed!.TargetNodeType == packet.Header.TargetNodeType, "TargetNodeType round-trip failed.");
        Require(parsed.SourceNodeType == packet.Header.SourceNodeType, "SourceNodeType round-trip failed.");
        Require(parsed.Signature == packet.Header.Signature, "Signature round-trip failed.");
        Require(parsed.Protocol == packet.Header.Protocol, "Protocol round-trip failed.");
        Require(parsed.Flags == packet.Header.Flags, "Flags round-trip failed.");
        Require(parsed.Encryption == packet.Header.Encryption, "Encryption round-trip failed.");
        Require(parsed.TypeScope == packet.Header.TypeScope, "TypeScope round-trip failed.");
        Require(parsed.TypeCommand == packet.Header.TypeCommand, "TypeCommand round-trip failed.");
        Require(parsed.SenderInstanceId == packet.Header.SenderInstanceId, "SenderInstanceId round-trip failed.");
        Require(parsed.MessageId == packet.Header.MessageId, "MessageId round-trip failed.");
        Require(parsed.SessionId == packet.Header.SessionId, "SessionId round-trip failed.");
        Require(parsed.AckForMessageId == packet.Header.AckForMessageId, "AckForMessageId round-trip failed.");
        Require(parsed.RoutingKey == packet.Header.RoutingKey, "RoutingKey round-trip failed.");
        Require(parsed.PayloadLength == packet.Header.PayloadLength, "PayloadLength round-trip failed.");

        Require(!protocol.Discard(receiver, packet), "Packet should not be discarded for correct target.");

        Console.WriteLine("PASS: TestHeaderRoundTrip");
    }

    private static void TestWrongTargetDiscard()
    {
        var protocol = new TestProtocol();
        var sender = new TestNode(NodeTypes.Client, protocol);
        var receiver = new TestNode(NodeTypes.Host, protocol);

        var packet = new Packet
        {
            Header = new PacketHeader
            {
                TargetNodeType = NodeTypes.System
            }
        };

        protocol.Stamp(sender, packet);

        Require(protocol.Discard(receiver, packet), "Packet should be discarded for wrong target node type.");

        Console.WriteLine("PASS: TestWrongTargetDiscard");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class TestProtocol : ProtocolBase
    {
        public override ProtocolTypes ProtocolType => ProtocolTypes.TcpIP;

        public override ValueTask ConnectAsync(Node node, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public override ValueTask DisconnectAsync(Node node, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public override ValueTask SendAsync(Node node, Packet packet, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public override ValueTask<Packet?> ReceiveAsync(Node node, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Packet?>(null);

        public void Stamp(Node node, Packet packet) => UpdatePacketIdentity(node, packet);

        public bool Discard(Node node, Packet packet) => ShouldDiscardPacket(node, packet);
    }

    private sealed class TestNode : Node
    {
        public TestNode(NodeTypes nodeType, IProtocol protocol) : base(nodeType, protocol)
        {
        }
    }
}