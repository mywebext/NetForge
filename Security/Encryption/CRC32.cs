// Network.Security.Encryption/CRC32.cs
using System;
using System.Buffers.Binary;

namespace NetForge.Security.Encryption;

/// <summary>
/// Provides CRC-32 checksum helpers for packet and payload integrity validation.
/// </summary>
public static class CRC32
{
    private const uint Polynomial = 0xEDB88320u;
    private static readonly uint[] Table = CreateTable();

    /// <summary>
    /// Computes the CRC-32 checksum for the specified buffer range.
    /// </summary>
    public static uint Compute(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0 || offset + count > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        uint crc = 0xFFFFFFFFu;

        for (int i = offset; i < offset + count; i++)
            crc = (crc >> 8) ^ Table[(crc ^ data[i]) & 0xFF];

        return crc ^ 0xFFFFFFFFu;
    }

    /// <summary>
    /// Computes the CRC-32 checksum for the entire buffer.
    /// </summary>
    public static uint Compute(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Compute(data, 0, data.Length);
    }

    /// <summary>
    /// Writes a CRC-32 value to the buffer in big-endian order.
    /// </summary>
    public static void WriteTo(byte[] buffer, int offset, uint value)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || offset + 4 > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset, 4), value);
    }

    /// <summary>
    /// Reads a CRC-32 value from the buffer in big-endian order.
    /// </summary>
    public static uint ReadFrom(byte[] buffer, int offset)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || offset + 4 > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
    }

    /// <summary>
    /// Computes a packet CRC assuming the first 4 bytes are reserved for the CRC field itself.
    /// </summary>
    /// <remarks>
    /// The checksum is computed over all bytes after the first 4 bytes.
    /// </remarks>
    public static uint ComputePacket(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Length < 4)
            throw new ArgumentException("Packet must be at least 4 bytes long.", nameof(packet));

        return Compute(packet, 4, packet.Length - 4);
    }

    /// <summary>
    /// Computes the packet CRC and writes it into the first 4 bytes of the packet.
    /// </summary>
    public static void StampPacket(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Length < 4)
            throw new ArgumentException("Packet must be at least 4 bytes long.", nameof(packet));

        uint crc = ComputePacket(packet);
        WriteTo(packet, 0, crc);
    }

    /// <summary>
    /// Verifies a packet whose first 4 bytes contain the CRC-32 of the remaining bytes.
    /// </summary>
    public static bool VerifyPacket(byte[] packet)
    {
        if (packet == null || packet.Length < 4)
            return false;

        uint expected = ReadFrom(packet, 0);
        uint actual = ComputePacket(packet);
        return expected == actual;
    }

    private static uint[] CreateTable()
    {
        uint[] table = new uint[256];

        for (uint i = 0; i < table.Length; i++)
        {
            uint value = i;

            for (int j = 0; j < 8; j++)
                value = (value & 1) != 0 ? (value >> 1) ^ Polynomial : value >> 1;

            table[i] = value;
        }

        return table;
    }
}