//NetForge.Networking/Functions.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking;


/// <summary>
/// Provides shared helper functions used throughout FileEngine.
/// </summary>
public static partial class Functions
{
    /// <summary>
    /// Extracts all numeric characters from a type name and attempts to convert
    /// the result into an integer value.
    /// </summary>
    /// <param name="type">
    /// The type whose name will be inspected for numeric characters.
    /// </param>
    /// <returns>
    /// An integer built from the numeric characters found in the type name;
    /// otherwise <c>0</c> if the type is <c>null</c>, contains no digits,
    /// or the extracted digits cannot be parsed.
    /// </returns>
    /// <remarks>
    /// This is useful for patterns where version information is embedded in a
    /// type name, such as <c>PacketV2</c> or <c>Protocol202</c>.
    /// </remarks>
    public static int NumbersFromTypeName(Type? type)
    {
        if (type == null)
            return 0;

        string digits = new string(type.Name.Where(char.IsDigit).ToArray());

        if (Null(digits))
            return 0;

        return int.TryParse(digits, out int version) ? version : 0;
    }
}
public static class NetOrder
{
    /// <summary>
    /// Converts a 16-bit unsigned integer to a 2-byte big-endian array
    /// using network byte order.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer to convert.</param>
    /// <returns>
    /// A 2-byte array in big-endian order, with the most significant byte first.
    /// </returns>
    public static byte[] U16BE(ushort value) =>
    [
        (byte)(value >> 8),
        (byte)value
    ];

    /// <summary>
    /// Converts a 64-bit unsigned integer to an 8-byte big-endian array
    /// using network byte order.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer to convert.</param>
    /// <returns>
    /// An 8-byte array in big-endian order, with the most significant byte first.
    /// </returns>
    public static byte[] U64BE(ulong value) =>
    [
        (byte)(value >> 56),
        (byte)(value >> 48),
        (byte)(value >> 40),
        (byte)(value >> 32),
        (byte)(value >> 24),
        (byte)(value >> 16),
        (byte)(value >> 8),
        (byte)value
    ];
}