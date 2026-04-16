//./Tools/OpCodeBuilder/Program.cs
using NetForge.Networking.Enums;
using NetForge.Test;
using System;
using System.IO;
using static OpCodeBuilder.Check;
if (!EnumsHaveChanged()) return 0;
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
#warning repoRoot is machine dependent and this code might now work for all setups.You may need to edit this to match your path settings
    string repoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));

    string outputPath = Path.Combine(
        repoRoot,
        "Networking",
        "Enums",
        "OpCodes.cs");

    return outputPath;
}