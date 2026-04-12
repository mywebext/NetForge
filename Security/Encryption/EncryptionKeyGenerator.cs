// FileEngine.Security.Encryption/EncryptionKeyGenerator.cs
using System;
using System.Security.Cryptography;

namespace NetForge.Security.Encryption;

public static class EncryptionKeyGenerator
{
    public static byte[] Generate(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Key length must be greater than zero.");

        byte[] key = new byte[length];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
