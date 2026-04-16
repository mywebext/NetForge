using static CtxSignlib.Certificates.CertHelpers;
using CtxSignlib.Diagnostics;
using CtxSignlib.Manifest;
using CtxSignlib.Verify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NetForge
{
    public partial class Program
    {
        public class Check
        {
            private const string ManifestFileName = "NetOps.json";
            private const string SignatureFileName = "NetOps.sig";
            private const string CertificateCerFileName = "NetForge.cer";
            private const string CertificatePfxFileName = "NetForge.pfx";
            private const string SecurityKeyFileName = "security.key";
            private const string ExcludeFileName = "NetOps.excluded";

            public static bool OpCodesChanged()
            {
                string projectRoot = NetForge.Program.ResolveProjectRoot();
                if (Null(projectRoot))
                    throw new ArgumentException("Project root is required.", nameof(projectRoot));

                string securityDirectory = Path.Combine(projectRoot, "Security");
                string manifestOutDirectory = Path.Combine(projectRoot, "Tools", "OpCodeBuilder");

                string manifestOutPath = Path.Combine(manifestOutDirectory, ManifestFileName);
                string keyPath = Path.Combine(securityDirectory, SecurityKeyFileName);
                string pfxPath = Path.Combine(securityDirectory, CertificatePfxFileName);
                string cerPath = Path.Combine(securityDirectory, CertificateCerFileName);
                string sigPath = Path.Combine(manifestOutDirectory, SignatureFileName);

                Directory.CreateDirectory(securityDirectory);
                Directory.CreateDirectory(manifestOutDirectory);

                X509Certificate2 cert;
                string key;

                if (!File.Exists(pfxPath))
                {
                    cert = CreateSelfSignedRsa("NetForge");
                    File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx));
                    File.WriteAllBytes(cerPath, cert.Export(X509ContentType.Cert));

                    key = BuildPubPin(cert);
                    File.WriteAllText(keyPath, key);
                }
                else
                {
                    cert = LoadPfxFile(pfxPath, string.Empty);

                    if (File.Exists(keyPath))
                    {
                        key = File.ReadAllText(keyPath).Trim();
                    }
                    else
                    {
                        key = BuildPubPin(cert);
                        File.WriteAllText(keyPath, key);
                    }
                }

                VerifyResult verification;
                ManifestPartialVerificationResult chk =
                    SignedManifestPartialVerifier.VerifySignedManifestPartialDetailed(
                        projectRoot,
                        manifestOutPath,
                        sigPath,
                        key,
                        out verification);

                PrintDetailedResult(chk, verification);

                return !chk.Success;
            }

            private static string BuildPubPin(X509Certificate2 cert)
            {
                byte[] spki = GetSpkiBytes(cert);
                return Convert.ToHexString(SHA256.HashData(spki));
            }

            private static void PrintDetailedResult(
                ManifestPartialVerificationResult result,
                VerifyResult verification)
            {
                Console.WriteLine($"OpCodes: {verification}");
                Console.WriteLine($"Loaded Original Manifest: {result.Success}");

                if (result.PassedFiles is not null && result.PassedFiles.Count > 0)
                {
                    Console.WriteLine("Matched:");
                    foreach (string file in result.PassedFiles)
                        Console.WriteLine($"  OK  {file}");
                }

                if (result.FailedFiles is not null && result.FailedFiles.Count > 0)
                {
                    Console.WriteLine("Changed:");
                    foreach (string file in result.FailedFiles)
                        Console.WriteLine($"  BAD {file}");
                }

                if (result.MissingFiles is not null && result.MissingFiles.Count > 0)
                {
                    Console.WriteLine("Missing:");
                    foreach (string file in result.MissingFiles)
                        Console.WriteLine($"  MIS {file}");
                }

                if (result.UnreadableFiles is not null && result.UnreadableFiles.Count > 0)
                {
                    Console.WriteLine("Unreadable:");
                    foreach (string file in result.UnreadableFiles)
                        Console.WriteLine($"  ERR {file}");
                }
            }
        }
    }
}
