﻿// Copyright (C) Tenacom and Contributors. Licensed under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1050 // Declare types in namespaces

/// <summary>
/// Adds polyfill source files to a project.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class PolyfillGenerator : IIncrementalGenerator
{
    private const string Eol = "\r\n";

    // All polyfill source files are saved as UTF-8 with BOM, with lines separated by CR+LF.
    // (see .editorconfig in the project root)
    private static readonly IReadOnlyCollection<string> Header = new[]
    {
        "// <auto-generated>",
        "//                                   >>>> DO NOT MODIFY THIS FILE <<<<",
        "//",
        "// This file is part of the PolyKit.Embedded NuGet package and resides in your local NuGet cache.",
        "// Any modification you make will influence all projects on your machine that reference PolyKit.Embedded.",
        "// Modifications will be undone anyway as soon as you update PolyKit.Embedded or clear your local NuGet cache.",
        "//",
        "// If there seems to be a problem with this file:",
        "//   - First of all, be sure to read the documentation at https://github.com/Tenacom/PolyKit#readme",
        "//   - If you have a doubt or want to ask a question, you are welcome to our Discussions area",
        "//     at https://github.com/Tenacom/PolyKit/discussions",
        "//   - Please check whether there is already an issue that applies to your problem",
        "//     at https://github.com/Tenacom/PolyKit/issues - you may find an existing solution or workaround",
        "//   - If you think you have found a bug that has not been reported yet,",
        "//     or have an idea that may help improve the PolyKit project,",
        "//     you are welcome to open a new issue at https://github.com/Tenacom/PolyKit/issues/new",
        "//",
        "// This file is part of PolyKit.Embedded version " + ThisAssembly.AssemblyInformationalVersion,
        "// and is provided under one or more license agreements.",
        "// Please see https://github.com/Tenacom/PolyKit for full license information.",
        "// </auto-generated>",
        string.Empty,
        "#nullable enable",
        string.Empty,
        "#pragma warning disable RS0016 // Add public types and members to the declared API",
        "#pragma warning disable RS0041 // Public members should not use oblivious types",
        string.Empty,
    };

    private static readonly Regex PolyfillRegex = new(
        @"^(\s*)public(?:\s*)//(?:\s*)polyfill!((?:\+|-)?)(?:\s|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    // For the rationale behind the added attributes, see this article:
    // https://riccar.do/posts/2022/2022-05-30-well-behaved-guest-code.html
    private static readonly IReadOnlyCollection<string> AdditionalPolyfillLines = new[]
    {
        "[System.Diagnostics.DebuggerNonUserCode]",
        "[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]",
    };
    private static readonly IReadOnlyCollection<string> PolyfillLines = new[]
    {
        "[System.CodeDom.Compiler.GeneratedCode(\"PolyKit.Embedded\", \"" + ThisAssembly.AssemblyInformationalVersion + "\")]",
        "#if POLYKIT_PUBLIC",
        "public",
        "#else",
        "internal",
        "#endif",
    };

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(static ctx =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames()
                                .Where(n => n.EndsWith(".cs", StringComparison.Ordinal))
                                .Select(s => s.Substring(0, s.Length - 3));

            var sb = new StringBuilder(16 * 1024);
            foreach (var line in Header)
            {
                _ = sb.Append(line).Append(Eol);
            }

            var headerLength = sb.Length;
            foreach (var name in names)
            {
                using (var inStream = assembly.GetManifestResourceStream(name + ".cs"))
                using (var reader = new StreamReader(inStream, Encoding.UTF8))
                {
                    for (; ;)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        var match = PolyfillRegex.Match(line);
                        if (!match.Success)
                        {
                            _ = sb.Append(line).Append(Eol);
                            continue;
                        }

                        var indentation = match.Groups[1].Captures[0].Value;
                        var modifier = match.Groups[2].Captures[0].Value;

                        // Additional polyfill lines contain attributes that are not valid on enums
                        var replacementLines = modifier == "-"
                            ? PolyfillLines
                            : AdditionalPolyfillLines.Concat(PolyfillLines);

                        foreach (var replacementLine in replacementLines)
                        {
                            // Only indent non-empty lines; never indent preprocessor directives
                            if (replacementLine.Length > 0 && replacementLine[0] != '#')
                            {
                                _ = sb.Append(indentation);
                            }

                            _ = sb.Append(replacementLine).Append(Eol);
                        }
                    }
                }

                ctx.AddSource(name + ".g.cs", sb.ToString());
                sb.Length = headerLength;
            }
        });
}
