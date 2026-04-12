/*
using System.Net;

namespace NetForge.Networking
{
    internal sealed class UdpPacket
    {
        public string Signature;              // "FEUC" or "FESH" (4 chars)
        public PacketType Type;               // your enum
        public UdpFlags Flags;

        public ulong SenderInstanceId;        // sender Service.InstanceId
        public ulong MessageId;               // per-sender unique
        public ulong SessionId;               // host-issued, 0 if not established
        public ulong AckForMessageId;         // used when Flags.IsAck

        public ArraySegment<byte> Payload;    // raw payload (can be empty)
        public IPEndPoint RemoteEndPoint;     // where it came from
    }
}
*/