//NetForge.Networking.Udp/Constants.cs
namespace NetForge.Networking.Udp
{
    internal static class Constants
    {
        // 4-byte magic. Pick one and never change it.
        public const uint Magic = 0x554E4546; // 'FENU' little-endian friendly marker

        public const byte VersionMajor = 1;
        public const byte VersionMinor = 0;

        public const int HeaderSize = 26;

        // Flags
        public const byte FlagAck = 0x01;

        // Opcodes
        public const byte OpHello = 0x01;
    }
}
