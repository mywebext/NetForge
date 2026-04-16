// NetForge/Tools/EnumManifestService.cs
using CtxSignlib.Certificates;
using CtxSignlib.Manifest;
using CtxSignlib.Signing;
using CtxSignlib.Verify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static CtxSignlib.Certificates.CertHelpers;
using static CtxSignlib.Functions;
using static NetForge.Program.Check;

namespace NetForge
{
    public partial class Program
    {
        /// <summary>
        /// Creates or refreshes an enum manifest for files under Networking\Enums.
        /// The manifest file is physically stored in Tools\OpCodeBuilder, but all
        /// entries are recorded relative to the NetForge project root.
        /// </summary>
        public static class EnumManifestService
        {
            private const string ManifestFileName = "NetOps.json";
            private const string SignatureFileName = "NetOps.sig";
            private const string CertificateCerFileName = "NetForge.cer";
            private const string CertificatePfxFileName = "NetForge.pfx";
            private const string SecurityKeyFileName = "security.key";
            private const string ExcludeFileName = "NetOps.excluded";

            public static Task EnsureCurrentAsync(string projectRoot)
            {
                if (Null(projectRoot))
                    throw new ArgumentException("Project root is required.", nameof(projectRoot));

                string securityDirectory = Path.Combine(projectRoot, "Security");
                string manifestRootDirectory = Path.Combine(projectRoot, "Networking", "Enums");
                string manifestOutDirectory = Path.Combine(projectRoot, "Tools", "OpCodeBuilder");

                string manifestOutPath = Path.Combine(manifestOutDirectory, ManifestFileName);
                string keyPath = Path.Combine(securityDirectory, SecurityKeyFileName);
                string pfxPath = Path.Combine(securityDirectory, CertificatePfxFileName);
                string cerPath = Path.Combine(securityDirectory, CertificateCerFileName);
                string sigPath = Path.Combine(manifestOutDirectory, SignatureFileName);
                string excludePath = Path.Combine(manifestOutDirectory, ExcludeFileName);

                Directory.CreateDirectory(securityDirectory);
                Directory.CreateDirectory(manifestOutDirectory);

                // Build excluded file list for first manifest creation / refresh workflow.
                BuildExcludeFile(projectRoot, manifestRootDirectory, excludePath);

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

                // Build/update manifest. Existing manifest can carry its excludes forward.
                if (!File.Exists(manifestOutPath) || OpCodesChanged())
                { 
                    ManifestBuilder.BuildOrUpdate(projectRoot, manifestOutPath, excludePath);
                    // Re-sign every time after build/update so .sig matches the current manifest.
                    CMSWriter.SignDetachment(manifestOutPath, sigPath, cert);
                    bool ok = SingleFileVerification.VerifyFileByPublicKey(
                        manifestOutPath, sigPath, key, out var result);

                    Console.WriteLine(ok ? "Generated OpCode List" : $"ReGenerated OpCodes: {result}");
                }

                return Task.CompletedTask;
            }

            private static string BuildPubPin(X509Certificate2 cert)
            {
                byte[] spki = GetSpkiBytes(cert);
                return Convert.ToHexString(SHA256.HashData(spki));
            }

            private static void BuildExcludeFile(string baseDirectory, string targetDirectory, string outputFilePath)
            {
                baseDirectory = NormalizePath(baseDirectory);
                targetDirectory = NormalizePath(targetDirectory);
                outputFilePath = NormalizePath(outputFilePath);

                if (Null(baseDirectory))
                    throw new ArgumentException("Base directory is required.", nameof(baseDirectory));

                if (Null(targetDirectory))
                    throw new ArgumentException("Target directory is required.", nameof(targetDirectory));

                if (Null(outputFilePath))
                    throw new ArgumentException("Output file path is required.", nameof(outputFilePath));

                if (!Directory.Exists(baseDirectory))
                    throw new DirectoryNotFoundException($"Base directory was not found: {baseDirectory}");

                if (!Directory.Exists(targetDirectory))
                    throw new DirectoryNotFoundException($"Target directory was not found: {targetDirectory}");

                if (!IsSubPathOf(baseDirectory, targetDirectory))
                    throw new InvalidOperationException("Target directory must be inside the base directory.");

                string relativeTarget = NormalizeManifestPath(
                    Path.GetRelativePath(baseDirectory, targetDirectory));

                string[] keepParts = relativeTarget
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                List<string> excludes = new();
                string currentPath = baseDirectory;
                string currentRelative = string.Empty;

                for (int i = 0; i < keepParts.Length; i++)
                {
                    string keepPart = keepParts[i];

                    foreach (string dir in Directory.GetDirectories(currentPath))
                    {
                        string name = Path.GetFileName(dir);

                        if (!string.Equals(name, keepPart, StringComparison.OrdinalIgnoreCase))
                        {
                            string rel = NormalizeManifestPath(
                                string.IsNullOrEmpty(currentRelative)
                                    ? name
                                    : $"{currentRelative}/{name}");

                            excludes.Add(rel.EndsWith("/", StringComparison.Ordinal) ? rel : rel + "/"); //Commit trigger
                        }
                    }

                    foreach (string file in Directory.GetFiles(currentPath))
                    {
                        string rel = NormalizeManifestPath(
                            string.IsNullOrEmpty(currentRelative)
                                ? Path.GetFileName(file)
                                : $"{currentRelative}/{Path.GetFileName(file)}");

                        if (!string.Equals(
                                Path.GetFullPath(file),
                                Path.GetFullPath(outputFilePath),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            excludes.Add(rel);
                        }
                    }

                    currentPath = Path.Combine(currentPath, keepPart);
                    currentRelative = NormalizeManifestPath(
                        string.IsNullOrEmpty(currentRelative)
                            ? keepPart
                            : $"{currentRelative}/{keepPart}");
                }

                string? outputDirectory = Path.GetDirectoryName(outputFilePath);
                if (!Null(outputDirectory))
                    Directory.CreateDirectory(outputDirectory!);

                File.WriteAllLines(
                    outputFilePath,
                    excludes
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}