// NetForge.Networking/PacketSignatures.cs
using System;
using System.Text;
using NetForge.Networking.Enums;

namespace NetForge.Networking;

public static class PacketSignatures
{
    public static readonly uint MYUC = FromAscii("MYUC"); // My User Client
    public static readonly uint MYSH = FromAscii("MYSH"); // My Server Host
    public static readonly uint MYSE = FromAscii("MYSE"); // My Seeder
    public static readonly uint MYSY = FromAscii("MYSY"); // My System

    public static uint FromAscii(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != 4)
            throw new ArgumentException("Signature must be exactly 4 ASCII characters.", nameof(value));

        byte[] bytes = Encoding.ASCII.GetBytes(value);

        return ((uint)bytes[0] << 24) |
               ((uint)bytes[1] << 16) |
               ((uint)bytes[2] << 8) |
               bytes[3];
    }

    public static string ToAscii(uint value)
    {
        char[] chars =
        [
            (char)((value >> 24) & 0xFF),
            (char)((value >> 16) & 0xFF),
            (char)((value >> 8) & 0xFF),
            (char)(value & 0xFF)
        ];

        return new string(chars);
    }

    /// <summary>
    /// Returns true if the signature belongs to the FileEngine packet family.
    /// </summary>
    public static bool IsFileEngineSignature(uint signature)
    {
        return signature == MYUC ||
               signature == MYSH ||
               signature == MYSE ||
               signature == MYSY;
    }

    /// <summary>
    /// Returns the default FileEngine signature for the specified node type.
    /// </summary>
    public static uint GetDefaultSignature(NodeTypes nodeType)
    {
        return nodeType switch
        {
            NodeTypes.Client => MYUC,
            NodeTypes.Host => MYSH,
            NodeTypes.Seeder => MYSE,
            NodeTypes.System => MYSY,
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unsupported node type.")
        };
    }
    public static bool IsDefinedNodeType(NodeTypes nodeType)
    {
        return nodeType == NodeTypes.Client ||
               nodeType == NodeTypes.Host ||
               nodeType == NodeTypes.System ||
               nodeType == NodeTypes.Seeder;
    }
}