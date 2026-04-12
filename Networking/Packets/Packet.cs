//NetForge.Networking/Packet.cs
using System.Net;

namespace NetForge.Networking.Packets;

public class Packet
{
    public PacketHeader Header { get; set; } = new();
    public ArraySegment<byte> Payload { get; set; }
    public IPEndPoint? RemoteEndPoint { get; set; }
}