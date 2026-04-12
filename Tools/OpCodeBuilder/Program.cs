//./Tools/OpCodeBuilder/Program.cs
using NetForge.Networking.Enums;
using NetForge.Test;
using System;
using System.IO;

string outputPath = ResolveOutputPath();

string source = OpCodeEnumGenerator.BuildOpCodeEnumSource(
    namespaceName: "NetForge.Networking.Enums",
    enumName: "OpCodes",
    underlyingType: "ushort",
    includeNone: true,
    includeComments: false);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, source);

Console.WriteLine($"Generated: {outputPath}");
return 0;

static string ResolveOutputPath()
{
    string repoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));

    string outputPath = Path.Combine(
        repoRoot,
        "Networking",
        "Enums",
        "OpCodes.cs");

    return outputPath;
}