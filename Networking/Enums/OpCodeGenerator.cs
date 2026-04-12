using NetForge.Networking.Enums;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NetForge.Test;

public static class OpCodeEnumGenerator
{
    /// <summary>
    /// Builds a C# enum source string that combines TypeScopes and CommandTypes
    /// into a development-only OpCodes enum using:
    /// (TypeScope * 100) + CommandType
    /// </summary>
    public static string BuildOpCodeEnumSource(
        string namespaceName = "NetForge.Networking.Enums",
        string enumName = "OpCodes",
        string underlyingType = "ushort",
        bool includeNone = true,
        bool includeComments = false)
    {
        StringBuilder sb = new();

        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated development enum built from TypeScopes and CommandTypes.");
        sb.AppendLine("/// Value formula: (TypeScope * 100) + CommandType");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public enum {enumName} : {underlyingType}");
        sb.AppendLine("{");

        TypeScopes[] scopes = Enum.GetValues<TypeScopes>();
        CommandTypes[] commands = Enum.GetValues<CommandTypes>();

        bool wroteAny = false;

        foreach (TypeScopes scope in scopes)
        {
            int scopeValue = (int)scope;

            if (!includeNone && scopeValue == 0)
                continue;

            foreach (CommandTypes command in commands)
            {
                int commandValue = (int)command;

                if (!includeNone && commandValue == 0)
                    continue;

                int combinedValue = (scopeValue * 100) + commandValue;
                string memberName = $"{scope}{command}";

                if (includeComments)
                {
                    sb.AppendLine(
                        $"    /// <summary>{scope}({scopeValue}) * 100 + {command}({commandValue}) = {combinedValue}</summary>");
                }

                sb.AppendLine($"    {memberName} = {combinedValue},");
                wroteAny = true;
            }

            if (wroteAny)
                sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Prints the generated enum source to the Visual Studio debug output window.
    /// </summary>
    public static void PrintOpCodeEnumToDebug(
        string namespaceName = "NetForge.Networking.Enums",
        string enumName = "OpCodes",
        string underlyingType = "ushort",
        bool includeNone = true,
        bool includeComments = false)
    {
        string source = BuildOpCodeEnumSource(
            namespaceName,
            enumName,
            underlyingType,
            includeNone,
            includeComments);

        Debug.WriteLine(source);
    }

    /// <summary>
    /// Prints each generated opcode as a simple name = value line.
    /// Useful for quick debugging without printing the full enum source wrapper.
    /// </summary>
    public static void PrintOpCodeValuesToDebug(bool includeNone = true)
    {
        TypeScopes[] scopes = Enum.GetValues<TypeScopes>();
        CommandTypes[] commands = Enum.GetValues<CommandTypes>();

        foreach (TypeScopes scope in scopes)
        {
            int scopeValue = (int)scope;

            if (!includeNone && scopeValue == 0)
                continue;

            foreach (CommandTypes command in commands)
            {
                int commandValue = (int)command;

                if (!includeNone && commandValue == 0)
                    continue;

                int combinedValue = (scopeValue * 100) + commandValue;
                string memberName = $"{scope}{command}";
                Debug.WriteLine($"{memberName} = {combinedValue}");
            }
        }
    }
}