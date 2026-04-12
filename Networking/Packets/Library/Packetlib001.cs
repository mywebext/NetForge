//NetForge.Networking.Packets.Library/Packetlib001.cs
using System;

namespace NetForge.Networking.Packets.Library;

public class Packetlib001 : Packetlib
{
    public override void Process(PacketHeader header, ArraySegment<byte> payload)
    {
        base.Process(header, payload);
    }

    public override void Process(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        // Real packet processing pipeline will go here.
        // For now this class acts as the versioned Packetlib entry point.
    }
}