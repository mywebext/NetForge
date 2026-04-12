// NetForge.Networking/PacketFlags.cs
using System;

namespace NetForge.Networking.Enums;

/// <summary>
/// Defines bitwise flags that describe packet behavior and session intent
/// within the FileEngine networking layer.
/// </summary>
/// <remarks>
/// Multiple flags can be combined to describe a single packet.
/// For example, a packet may require acknowledgment while also being part
/// of handshake traffic.
/// </remarks>
[Flags]
public enum PacketFlags : ushort
{
    /// <summary>
    /// Indicates that no special packet flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the receiving side must acknowledge this packet
    /// by responding with an ACK that references the packet's message ID.
    /// </summary>
    AckRequired = 1 << 0,

    /// <summary>
    /// Indicates that this packet is an acknowledgment packet for a
    /// previously received message, typically identified by AckForMessageId.
    /// </summary>
    IsAck = 1 << 1,

    /// <summary>
    /// Indicates that this packet is part of handshake traffic used during
    /// connection or session establishment.
    /// </summary>
    IsHandshake = 1 << 2,

    /// <summary>
    /// Indicates that this packet is used as keep-alive traffic to help
    /// maintain session activity and detect stale connections.
    /// </summary>
    IsKeepAlive = 1 << 3,
    /// <summary>
    /// Indicates that this packet is compressed to help reduce
    /// packet size and network traffic
    /// </summary>
    IsCompressed = 1 << 4,
}
