// FileEngine.Security.Encryption/AesPayloadEncryption.cs
using System;
using System.Security.Cryptography;

namespace NetForge.Security.Encryption;

/// <summary>
/// Provides AES-GCM encryption and decryption helpers for packet payloads stored as <see cref="ArraySegment{Byte}"/>.
/// </summary>
/// <remarks>
/// Encrypted output format:
/// [12-byte nonce][16-byte tag][ciphertext]
///
/// AES-GCM provides both confidentiality and integrity.
/// If the wrong key or wrong associated data is used, decryption will fail.
/// </remarks>
public static class AESGcmEncryption
{
    /// <summary>
    /// Standard AES-GCM nonce size in bytes.
    /// </summary>
    public const int NonceSize = 12;

    /// <summary>
    /// Standard AES-GCM tag size in bytes.
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// Generates a random AES key of the requested size.
    /// Valid sizes are 16, 24, or 32 bytes.
    /// </summary>
    /// <param name="sizeBytes">Key size in bytes.</param>
    /// <returns>A new random AES key.</returns>
    public static byte[] GenerateKey(int sizeBytes = 32)
    {
        if (sizeBytes is not 16 and not 24 and not 32)
            throw new ArgumentException("AES key must be exactly 16, 24, or 32 bytes.", nameof(sizeBytes));

        return EncryptionKeyGenerator.Generate(sizeBytes);
    }

    /// <summary>
    /// Encrypts a payload using AES-GCM.
    /// </summary>
    /// <param name="payload">The payload segment to encrypt.</param>
    /// <param name="key">AES key bytes. Must be 16, 24, or 32 bytes.</param>
    /// <param name="associatedData">
    /// Optional additional authenticated data. This is not encrypted, but it must match during decryption.
    /// Useful for binding packet header fields to the ciphertext.
    /// </param>
    /// <returns>A byte array in the format [nonce][tag][ciphertext].</returns>
    public static byte[] Encrypt(
        ArraySegment<byte> payload,
        byte[] key,
        ArraySegment<byte>? associatedData = null)
    {
        ValidateSegment(payload, nameof(payload));
        ValidateKey(key);

        ReadOnlySpan<byte> plaintext = payload.AsSpan();

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[plaintext.Length];

        RandomNumberGenerator.Fill(nonce);

        ReadOnlySpan<byte> aad = associatedData.HasValue
            ? GetValidatedSpan(associatedData.Value, nameof(associatedData))
            : ReadOnlySpan<byte>.Empty;

        using AesGcm aes = new(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        byte[] output = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, output, NonceSize + TagSize, ciphertext.Length);

        return output;
    }

    /// <summary>
    /// Decrypts an AES-GCM payload produced by <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="encryptedPayload">Encrypted payload in the format [nonce][tag][ciphertext].</param>
    /// <param name="key">AES key bytes. Must be 16, 24, or 32 bytes.</param>
    /// <param name="associatedData">
    /// Optional additional authenticated data. Must exactly match the value used during encryption.
    /// </param>
    /// <returns>The decrypted plaintext bytes.</returns>
    public static byte[] Decrypt(
        ArraySegment<byte> encryptedPayload,
        byte[] key,
        ArraySegment<byte>? associatedData = null)
    {
        ValidateSegment(encryptedPayload, nameof(encryptedPayload));
        ValidateKey(key);

        ReadOnlySpan<byte> input = encryptedPayload.AsSpan();

        if (input.Length < NonceSize + TagSize)
            throw new ArgumentException(
                $"Encrypted payload is too short. Minimum size is {NonceSize + TagSize} bytes.",
                nameof(encryptedPayload));

        ReadOnlySpan<byte> nonce = input[..NonceSize];
        ReadOnlySpan<byte> tag = input.Slice(NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = input[(NonceSize + TagSize)..];

        byte[] plaintext = new byte[ciphertext.Length];

        ReadOnlySpan<byte> aad = associatedData.HasValue
            ? GetValidatedSpan(associatedData.Value, nameof(associatedData))
            : ReadOnlySpan<byte>.Empty;

        using AesGcm aes = new(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);

        return plaintext;
    }

    /// <summary>
    /// Encrypts a payload and returns the result as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    public static ArraySegment<byte> EncryptSegment(
        ArraySegment<byte> payload,
        byte[] key,
        ArraySegment<byte>? associatedData = null)
    {
        return new ArraySegment<byte>(Encrypt(payload, key, associatedData));
    }

    /// <summary>
    /// Decrypts a payload and returns the result as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    public static ArraySegment<byte> DecryptSegment(
        ArraySegment<byte> encryptedPayload,
        byte[] key,
        ArraySegment<byte>? associatedData = null)
    {
        return new ArraySegment<byte>(Decrypt(encryptedPayload, key, associatedData));
    }

    /// <summary>
    /// Returns the exact number of bytes required to hold an encrypted payload.
    /// </summary>
    public static int GetEncryptedLength(int plaintextLength)
    {
        if (plaintextLength < 0)
            throw new ArgumentOutOfRangeException(nameof(plaintextLength));

        return NonceSize + TagSize + plaintextLength;
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key.Length is not 16 and not 24 and not 32)
            throw new ArgumentException("AES key must be exactly 16, 24, or 32 bytes.", nameof(key));
    }

    private static void ValidateSegment(ArraySegment<byte> segment, string paramName)
    {
        if (segment.Array is null)
            throw new ArgumentNullException(paramName, "ArraySegment<byte> is not backed by an array.");
    }

    private static ReadOnlySpan<byte> GetValidatedSpan(ArraySegment<byte> segment, string paramName)
    {
        if (segment.Array is null)
            throw new ArgumentNullException(paramName, "ArraySegment<byte> is not backed by an array.");

        return segment.AsSpan();
    }
}
/* Usage
 * using System;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = AesPayloadEncryption.GenerateKey(32);

byte[] rawPayload = Encoding.UTF8.GetBytes("Hello from FileEngine.");
ArraySegment<byte> payload = new(rawPayload);

byte[] encrypted = AesPayloadEncryption.Encrypt(payload, key);
byte[] decrypted = AesPayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key);

string text = Encoding.UTF8.GetString(decrypted);
Console.WriteLine(text);
*/

/*
 * using System;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = AesPayloadEncryption.GenerateKey(32);

// Larger working buffer
byte[] buffer = new byte[1024];

// Write payload starting at offset 100
byte[] messageBytes = Encoding.UTF8.GetBytes("Packet payload data");
Buffer.BlockCopy(messageBytes, 0, buffer, 100, messageBytes.Length);

// Segment points only to the actual payload
ArraySegment<byte> payload = new(buffer, 100, messageBytes.Length);

byte[] encrypted = AesPayloadEncryption.Encrypt(payload, key);
byte[] decrypted = AesPayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key);

Console.WriteLine(Encoding.UTF8.GetString(decrypted));
*/

/*
 * using System;
using System.Buffers.Binary;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = AesPayloadEncryption.GenerateKey(32);

// Example payload
byte[] payloadBytes = Encoding.UTF8.GetBytes("Sensitive payload");
ArraySegment<byte> payload = new(payloadBytes);

// Example header/authenticated metadata
byte[] headerBytes = new byte[10];
BinaryPrimitives.WriteUInt16BigEndian(headerBytes.AsSpan(0, 2), 1001); // opcode
BinaryPrimitives.WriteUInt32BigEndian(headerBytes.AsSpan(2, 4), 55);   // messageId
BinaryPrimitives.WriteUInt32BigEndian(headerBytes.AsSpan(6, 4), 1);    // sessionId

ArraySegment<byte> aad = new(headerBytes);

byte[] encrypted = AesPayloadEncryption.Encrypt(payload, key, aad);
byte[] decrypted = AesPayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key, aad);

Console.WriteLine(Encoding.UTF8.GetString(decrypted));
*/

/* Exampled with Fake Packet Type
 * using System;
using System.Text;
using NetForge.Security.Encryption;

public sealed class Packet
{
    public ArraySegment<byte> Payload { get; set; }
}

byte[] key = AesPayloadEncryption.GenerateKey();

Packet packet = new()
{
    Payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes("My packet body"))
};

// Encrypt
packet.Payload = AesPayloadEncryption.EncryptSegment(packet.Payload, key);

// Decrypt
packet.Payload = AesPayloadEncryption.DecryptSegment(packet.Payload, key);

// Read back
string result = Encoding.UTF8.GetString(packet.Payload.Array!, packet.Payload.Offset, packet.Payload.Count);
Console.WriteLine(result);
*/