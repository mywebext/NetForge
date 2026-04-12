//NetForge.Networking.Packets/PacketHeaderWriter.cs
using System;
using System.Buffers.Binary;

namespace NetForge.Networking.Packets;

public static class PacketHeaderWriter
{
    public const int HeaderSize = PacketHeaderReader.HeaderSize;

    public static byte[] WriteHeader(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        byte[] buffer = new byte[HeaderSize];
        WriteHeader(header, buffer);
        return buffer;
    }

    public static bool WriteHeader(PacketHeader header, Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(header);

        if (destination.Length < HeaderSize)
            return false;

        int offset = 0;

        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(offset, 4), header.CRC32);
        offset += 4;

        destination[offset++] = (byte)header.TargetNodeType;
        destination[offset++] = (byte)header.SourceNodeType;

        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(offset, 4), header.Signature);
        offset += 4;

        destination[offset++] = (byte)header.Protocol;

        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(offset, 2), (ushort)header.Flags);
        offset += 2;

        destination[offset++] = (byte)header.Encryption;
        destination[offset++] = (byte)header.TypeScope;
        destination[offset++] = (byte)header.TypeCommand;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.SenderInstanceId);
        offset += 8;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.MessageId);
        offset += 8;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.SessionId);
        offset += 8;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.AckForMessageId);
        offset += 8;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.RoutingKey);
        offset += 8;

        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset, 8), header.PayloadLength);
        offset += 8;

        return offset == HeaderSize;
    }
}