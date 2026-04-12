//NetForge.Networking.Enums/Algorithms.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetForge.Networking.Enums;

public enum Algorithms : byte
{
    None = 0,
    RC4,
    CRC32,
    AESGcm,
    ChaCha20Poly1305,
    HMACSHA256,
    ECDiffieHellman,
    RSA,
    Sha256
}
