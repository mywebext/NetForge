//NetForge.Networking.Packets/PacketFlagsHelper.cs
using System;
using NetForge.Networking.Enums;

namespace NetForge.Networking.Packets;

public static class PacketFlagsHelper
{
    public static bool Has(PacketFlags value, PacketFlags flag)
    {
        return (value & flag) == flag;
    }

    public static PacketFlags Set(PacketFlags value, PacketFlags flag)
    {
        return value | flag;
    }

    public static PacketFlags Clear(PacketFlags value, PacketFlags flag)
    {
        return value & ~flag;
    }

    public static PacketFlags Toggle(PacketFlags value, PacketFlags flag)
    {
        return value ^ flag;
    }

    public static PacketFlags Set(PacketFlags value, PacketFlags flag, bool enabled)
    {
        return enabled ? Set(value, flag) : Clear(value, flag);
    }

    public static bool IsNone(PacketFlags value)
    {
        return value == PacketFlags.None;
    }

    public static bool RequiresAck(PacketFlags value)
    {
        return Has(value, PacketFlags.AckRequired);
    }

    public static bool IsAck(PacketFlags value)
    {
        return Has(value, PacketFlags.IsAck);
    }

    public static bool IsHandshake(PacketFlags value)
    {
        return Has(value, PacketFlags.IsHandshake);
    }

    public static bool IsKeepAlive(PacketFlags value)
    {
        return Has(value, PacketFlags.IsKeepAlive);
    }

    public static bool IsCompressed(PacketFlags value)
    {
        return Has(value, PacketFlags.IsCompressed);
    }

    public static bool RequiresAck(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return RequiresAck(header.Flags);
    }

    public static bool IsAck(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return IsAck(header.Flags);
    }

    public static bool IsHandshake(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return IsHandshake(header.Flags);
    }

    public static bool IsKeepAlive(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return IsKeepAlive(header.Flags);
    }

    public static bool IsCompressed(PacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return IsCompressed(header.Flags);
    }

    public static void SetFlag(PacketHeader header, PacketFlags flag, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(header);
        header.Flags = Set(header.Flags, flag, enabled);
    }

    public static void ClearFlag(PacketHeader header, PacketFlags flag)
    {
        ArgumentNullException.ThrowIfNull(header);
        header.Flags = Clear(header.Flags, flag);
    }

    public static void ToggleFlag(PacketHeader header, PacketFlags flag)
    {
        ArgumentNullException.ThrowIfNull(header);
        header.Flags = Toggle(header.Flags, flag);
    }
}