//NetForge.Networking.Packets/PacketHeaderReader.cs
using NetForge.Networking.Enums;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace NetForge.Networking.Packets;

public static class PacketHeaderReader
{
    public const int HeaderSize =
        4 +  // CRC32
        1 +  // TargetNodeType
        1 +  // SourceNodeType
        4 +  // Signature
        1 +  // Protocol
        2 +  // Flags
        1 +  // Encryption
        1 +  // TypeScope
        1 +  // TypeCommand
        8 +  // SenderInstanceId
        8 +  // MessageId
        8 +  // SessionId
        8 +  // AckForMessageId
        8 +  // RoutingKey
        8;   // PayloadLength

    public static bool TryParseHeader(ReadOnlySpan<byte> data, out PacketHeader? header)
    {
        header = null;

        if (data.Length < HeaderSize)
            return false;

        int offset = 0;

        uint crc32 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        NodeTypes targetNodeType = (NodeTypes)data[offset++];
        NodeTypes sourceNodeType = (NodeTypes)data[offset++];

        uint signature = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        ProtocolTypes protocol = (ProtocolTypes)data[offset++];

        PacketFlags flags = (PacketFlags)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Algorithms encryption = (Algorithms)data[offset++];
        TypeScopes typeScope = (TypeScopes)data[offset++];
        CommandTypes typeCommand = (CommandTypes)data[offset++];

        ulong senderInstanceId = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        ulong messageId = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        ulong sessionId = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        ulong ackForMessageId = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        ulong routingKey = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        ulong payloadLength = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        offset += 8;

        if (offset != HeaderSize)
            return false;

        header = new PacketHeader
        {
            CRC32 = crc32,
            TargetNodeType = targetNodeType,
            SourceNodeType = sourceNodeType,
            Signature = signature,
            Protocol = protocol,
            Flags = flags,
            Encryption = encryption,
            TypeScope = typeScope,
            TypeCommand = typeCommand,
            SenderInstanceId = senderInstanceId,
            MessageId = messageId,
            SessionId = sessionId,
            AckForMessageId = ackForMessageId,
            RoutingKey = routingKey,
            PayloadLength = payloadLength
        };

        return true;
    }
}