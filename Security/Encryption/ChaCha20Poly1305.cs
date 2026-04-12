// FileEngine.Security.Encryption/ChaCha20Poly1305PayloadEncryption.cs
using System;
using System.Security.Cryptography;

namespace NetForge.Security.Encryption;

/// <summary>
/// Provides ChaCha20-Poly1305 encryption and decryption helpers for packet payloads
/// stored as <see cref="ArraySegment{Byte}"/>.
/// </summary>
/// <remarks>
/// Encrypted output format:
/// [12-byte nonce][16-byte tag][ciphertext]
///
/// ChaCha20-Poly1305 provides both confidentiality and integrity.
/// If the wrong key or wrong associated data is used, decryption will fail.
/// </remarks>
public static class ChaCha20Encryption
{
    /// <summary>
    /// Standard ChaCha20-Poly1305 key size in bytes.
    /// </summary>
    public const int KeySize = 32;

    /// <summary>
    /// Standard ChaCha20-Poly1305 nonce size in bytes.
    /// </summary>
    public const int NonceSize = 12;

    /// <summary>
    /// Standard ChaCha20-Poly1305 authentication tag size in bytes.
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// Generates a random ChaCha20-Poly1305 key.
    /// </summary>
    public static byte[] GenerateKey()
    {
        return EncryptionKeyGenerator.Generate(32);
    }

    /// <summary>
    /// Returns true when ChaCha20-Poly1305 is supported on the current platform.
    /// </summary>
    public static bool IsSupported()
    {
        return ChaCha20Poly1305.IsSupported;
    }

    /// <summary>
    /// Encrypts a payload using ChaCha20-Poly1305.
    /// </summary>
    /// <param name="payload">The payload segment to encrypt.</param>
    /// <param name="key">Encryption key. Must be exactly 32 bytes.</param>
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
        ValidatePlatformSupport();
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

        using ChaCha20Poly1305 cipher = new(key);
        cipher.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        byte[] output = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, output, NonceSize + TagSize, ciphertext.Length);

        return output;
    }

    /// <summary>
    /// Decrypts a ChaCha20-Poly1305 payload produced by <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="encryptedPayload">Encrypted payload in the format [nonce][tag][ciphertext].</param>
    /// <param name="key">Encryption key. Must be exactly 32 bytes.</param>
    /// <param name="associatedData">
    /// Optional additional authenticated data. Must exactly match the value used during encryption.
    /// </param>
    /// <returns>The decrypted plaintext bytes.</returns>
    public static byte[] Decrypt(
        ArraySegment<byte> encryptedPayload,
        byte[] key,
        ArraySegment<byte>? associatedData = null)
    {
        ValidatePlatformSupport();
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

        using ChaCha20Poly1305 cipher = new(key);
        cipher.Decrypt(nonce, ciphertext, tag, plaintext, aad);

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

    private static void ValidatePlatformSupport()
    {
        if (!ChaCha20Poly1305.IsSupported)
            throw new PlatformNotSupportedException(
                "ChaCha20-Poly1305 is not supported on this platform.");
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key.Length != KeySize)
            throw new ArgumentException(
                $"ChaCha20-Poly1305 key must be exactly {KeySize} bytes.",
                nameof(key));
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
/*
 * using System;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = ChaCha20Poly1305PayloadEncryption.GenerateKey();

byte[] payloadBytes = Encoding.UTF8.GetBytes("Hello from ChaCha20-Poly1305.");
ArraySegment<byte> payload = new(payloadBytes);

byte[] encrypted = ChaCha20Poly1305PayloadEncryption.Encrypt(payload, key);
byte[] decrypted = ChaCha20Poly1305PayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key);

string text = Encoding.UTF8.GetString(decrypted);
Console.WriteLine(text);
*/
/* Partial Buffer Example
 * using System;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = ChaCha20Poly1305PayloadEncryption.GenerateKey();

byte[] buffer = new byte[1024];
byte[] messageBytes = Encoding.UTF8.GetBytes("Packet payload data");

Buffer.BlockCopy(messageBytes, 0, buffer, 200, messageBytes.Length);

// Only encrypt the payload slice, not the whole buffer
ArraySegment<byte> payload = new(buffer, 200, messageBytes.Length);

byte[] encrypted = ChaCha20Poly1305PayloadEncryption.Encrypt(payload, key);
byte[] decrypted = ChaCha20Poly1305PayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key);

Console.WriteLine(Encoding.UTF8.GetString(decrypted));
*/
/*With associated header data
 * using System;
using System.Buffers.Binary;
using System.Text;
using NetForge.Security.Encryption;

byte[] key = ChaCha20Poly1305PayloadEncryption.GenerateKey();

byte[] payloadBytes = Encoding.UTF8.GetBytes("Sensitive payload");
ArraySegment<byte> payload = new(payloadBytes);

byte[] headerBytes = new byte[10];
BinaryPrimitives.WriteUInt16BigEndian(headerBytes.AsSpan(0, 2), 1001); // opcode
BinaryPrimitives.WriteUInt32BigEndian(headerBytes.AsSpan(2, 4), 55);   // messageId
BinaryPrimitives.WriteUInt32BigEndian(headerBytes.AsSpan(6, 4), 1);    // sessionId

ArraySegment<byte> aad = new(headerBytes);

byte[] encrypted = ChaCha20Poly1305PayloadEncryption.Encrypt(payload, key, aad);
byte[] decrypted = ChaCha20Poly1305PayloadEncryption.Decrypt(new ArraySegment<byte>(encrypted), key, aad);

Console.WriteLine(Encoding.UTF8.GetString(decrypted));
*/