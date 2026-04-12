// FileEngine.Security.Encryption/RC4.cs
using System;
using System.Security.Cryptography;

namespace NetForge.Security.Encryption;

/// <summary>
/// Provides legacy RC4 encryption and decryption helpers for packet payloads
/// stored as <see cref="ArraySegment{Byte}"/>.
/// </summary>
/// <remarks>
/// IMPORTANT:
/// RC4 is legacy cryptography and should not be used as the primary security layer
/// for new protocol design. RC4 is prohibited on certain protocols such as TLS due
/// to known weaknesses. Use AES-GCM or ChaCha20-Poly1305 for modern encrypted packet
/// protection whenever possible.
/// 
/// RC4 is a symmetric stream cipher, so encryption and decryption are the same
/// byte-wise transform when using the same key.
/// </remarks>
public static class RC4
{
    /// <summary>
    /// Encrypts a payload using RC4.
    /// </summary>
    /// <param name="payload">The payload segment to transform.</param>
    /// <param name="key">The RC4 key bytes.</param>
    /// <returns>The transformed bytes.</returns>
    public static byte[] Encrypt(ArraySegment<byte> payload, byte[] key)
    {
        ValidateSegment(payload, nameof(payload));
        ValidateKey(key);

        return Transform(payload.AsSpan(), key);
    }

    /// <summary>
    /// Decrypts a payload using RC4.
    /// </summary>
    /// <param name="payload">The payload segment to transform.</param>
    /// <param name="key">The RC4 key bytes.</param>
    /// <returns>The transformed bytes.</returns>
    public static byte[] Decrypt(ArraySegment<byte> payload, byte[] key)
    {
        ValidateSegment(payload, nameof(payload));
        ValidateKey(key);

        return Transform(payload.AsSpan(), key);
    }
    /// <summary>
    /// Generates a random RC4 key.
    /// </summary>
    public static byte[] GenerateKey(int length = 16)
    {
        return EncryptionKeyGenerator.Generate(length);
    }
    /// <summary>
    /// Encrypts a payload and returns the result as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    public static ArraySegment<byte> EncryptSegment(ArraySegment<byte> payload, byte[] key)
    {
        return new ArraySegment<byte>(Encrypt(payload, key));
    }

    /// <summary>
    /// Decrypts a payload and returns the result as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    public static ArraySegment<byte> DecryptSegment(ArraySegment<byte> payload, byte[] key)
    {
        return new ArraySegment<byte>(Decrypt(payload, key));
    }

    /// <summary>
    /// Applies the RC4 transform to the provided data.
    /// Since RC4 is symmetric, this method can be used for both encryption and decryption.
    /// </summary>
    /// <param name="data">The input bytes to transform.</param>
    /// <param name="key">The RC4 key bytes.</param>
    /// <returns>A new byte array containing the transformed data.</returns>
    public static byte[] Transform(ArraySegment<byte> data, byte[] key)
    {
        ValidateSegment(data, nameof(data));
        ValidateKey(key);

        return Transform(data.AsSpan(), key);
    }

    private static byte[] Transform(ReadOnlySpan<byte> data, byte[] key)
    {
        byte[] s = InitializeState(key);
        byte[] output = new byte[data.Length];

        int i = 0;
        int j = 0;

        for (int n = 0; n < data.Length; n++)
        {
            i = (i + 1) & 255;
            j = (j + s[i]) & 255;

            Swap(s, i, j);

            byte k = s[(s[i] + s[j]) & 255];
            output[n] = (byte)(data[n] ^ k);
        }

        return output;
    }

    private static byte[] InitializeState(byte[] key)
    {
        byte[] s = new byte[256];

        for (int i = 0; i < 256; i++)
            s[i] = (byte)i;

        int j = 0;

        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 255;
            Swap(s, i, j);
        }

        return s;
    }

    private static void Swap(byte[] s, int a, int b)
    {
        byte temp = s[a];
        s[a] = s[b];
        s[b] = temp;
    }

    private static void ValidateSegment(ArraySegment<byte> segment, string paramName)
    {
        if (segment.Array is null)
            throw new ArgumentNullException(paramName, "ArraySegment<byte> is not backed by an array.");
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key.Length == 0)
            throw new ArgumentException("RC4 key cannot be empty.", nameof(key));
    }
}
/*
 * using System;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = Encoding.UTF8.GetBytes("legacy-rc4-key");
byte[] payloadBytes = Encoding.UTF8.GetBytes("PlayerPosition:X=120,Y=450,Z=33");

ArraySegment<byte> payload = new(payloadBytes);

byte[] encrypted = RC4.Encrypt(payload, key);
byte[] decrypted = RC4.Decrypt(new ArraySegment<byte>(encrypted), key);

string result = Encoding.UTF8.GetString(decrypted);
Console.WriteLine(result);
*/
/* With generic for of a packet and payload
 * public sealed class Packet
{
    public ArraySegment<byte> Payload { get; set; }
}

byte[] key = Encoding.UTF8.GetBytes("legacy-rc4-key");

Packet packet = new()
{
    Payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes("RadarObfuscatedPosition"))
};

packet.Payload = RC4.EncryptSegment(packet.Payload, key);
packet.Payload = RC4.DecryptSegment(packet.Payload, key);
*/