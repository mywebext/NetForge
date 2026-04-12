// NetForge.Networking.Packets/PacketHeader.cs
using NetForge.Networking.Enums;

namespace NetForge.Networking.Packets;

public sealed class PacketHeader
{
    public uint CRC32;                  // The value CRC32 should return
    public NodeTypes TargetNodeType;    // byte
    public NodeTypes SourceNodeType;    // byte
    public uint Signature;              // FileEngine family signature
    public ProtocolTypes Protocol;      // byte
    public PacketFlags Flags;           // ushort with System.Flags
    public Algorithms Encryption;       // byte
    public TypeScopes TypeScope;        // byte
    public CommandTypes TypeCommand;    // byte
    public ulong SenderInstanceId;
    public ulong MessageId;
    public ulong SessionId;
    public ulong AckForMessageId;
    public ulong RoutingKey;
    public ulong PayloadLength;
}