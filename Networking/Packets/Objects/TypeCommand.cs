//NetForge.Networking.Packets.Objects/TypeCommand.cs;
using System;

namespace NetForge.Networking.Packets.Objects;

public abstract class TypeCommand
{
    public ulong MessageId { get; set; }
    public ulong SessionId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ulong RoutingKey { get; set; } = 0;
}

