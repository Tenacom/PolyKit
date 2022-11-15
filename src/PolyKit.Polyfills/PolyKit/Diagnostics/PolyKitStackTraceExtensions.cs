#if NET6_0_OR_GREATER

using System.Diagnostics;

namespace PolyKit.Diagnostics;

/// <summary>
/// Provides extension methods for instances of <see cref="StackTrace"/> to support polyfilled features.
/// </summary>
public // polyfill!
static class PolyKitStackTraceExtensions
{
    /// <summary>
    /// Builds a readable representation of a stack trace.
    /// This method returns the same result as <see cref="StackTrace.ToString"/>.
    /// </summary>
    /// <param name="this">The <see cref="StackTrace"/> on which this method is called.</param>
    /// <returns>A readable representation of the stack trace.</returns>
    public static string ToStringHidingFrames(this StackTrace @this) => @this.ToString();
}

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using PolyKit.Diagnostics.Internal;

namespace PolyKit.Diagnostics;

/// <summary>
/// Provides extension methods for instances of <see cref="StackTrace"/> to support polyfilled features.
/// </summary>
public // polyfill!
static class PolyKitStackTraceExtensions
{
    private const string StackTraceHiddenFullName = "System.Diagnostics.StackTraceHiddenAttribute";
    private const string AsyncIteratorStateMachineAttributeFullName = "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";

    // https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L178

    /// <summary>
    /// <para>Builds a readable representation of a stack trace, hiding stack frames marked with <see cref="StackTraceHiddenAttribute"/>.</para>
    /// </summary>
    /// <param name="this">The <see cref="StackTrace"/> on which this method is called.</param>
    /// <returns>A readable representation of the stack trace.</returns>
    /// <remarks>
    /// <para>Unlike <see cref="StackTrace.ToString"/>, this method honors the presence of
    /// <see cref="StackTraceHiddenAttribute"/> on stack frame methods.</para>
    /// <para>There are, however, some limitations:</para>
    /// <list type="bullet">
    /// <item><description>the returned stack trace description is always in English, irrespective of current culture;</description></item>
    /// <item><description>external exception stack trace boundaries ("End of stack trace from previous location" lines) are missing from the returned string.</description></item>
    /// </list>
    /// </remarks>
    // Include a trailing newline for backwards compatibility
    public static string ToStringHidingFrames(this StackTrace @this) => @this.ToStringHidingFrames(TraceFormat.TrailingNewLine);

    // https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L198
    internal static string ToStringHidingFrames(this StackTrace @this, TraceFormat traceFormat)
    {
        var sb = new StringBuilder(256);
        @this.ToStringHidingFrames(traceFormat, sb);
        return sb.ToString();
    }

    // https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L206
    internal static void ToStringHidingFrames(this StackTrace @this, TraceFormat traceFormat, StringBuilder sb)
    {
        var firstFrame = true;
        var numOfFrames = @this.FrameCount;
        for (var frameIndex = 0; frameIndex < numOfFrames; frameIndex++)
        {
            var sf = @this.GetFrame(frameIndex);
            var mb = sf?.GetMethod();

            // Don't filter last frame
            if (mb != null && (ShowInStackTrace(mb) || (frameIndex == numOfFrames - 1)))
            {
                // We want a newline at the end of every line except for the last
                if (firstFrame)
                {
                    firstFrame = false;
                }
                else
                {
                    _ = sb.AppendLine();
                }

                _ = sb.Append("   at ");

                bool isAsync;
                var declaringType = mb.DeclaringType;
                var methodName = mb.Name;
                var methodChanged = false;
                if (declaringType != null && declaringType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                {
                    isAsync = typeof(IAsyncStateMachine).IsAssignableFrom(declaringType);
                    if (isAsync || typeof(IEnumerator).IsAssignableFrom(declaringType))
                    {
                        methodChanged = TryResolveStateMachineMethod(ref mb, out declaringType);
                    }
                }

                // if there is a type (non global method) print it
                // ResolveStateMachineMethod may have set declaringType to null
                if (declaringType != null)
                {
                    // Append t.FullName, replacing '+' with '.'
                    var fullName = declaringType.FullName!;
                    for (var i = 0; i < fullName.Length; i++)
                    {
                        var ch = fullName[i];
                        _ = sb.Append(ch == '+' ? '.' : ch);
                    }

                    _ = sb.Append('.');
                }

                _ = sb.Append(mb.Name);

                // deal with the generic portion of the method
                if (mb is MethodInfo { IsGenericMethod: true } mi)
                {
                    var typeParams = mi.GetGenericArguments();
                    _ = sb.Append('[');
                    var k = 0;
                    var firstTypeParam = true;
                    while (k < typeParams.Length)
                    {
                        if (!firstTypeParam)
                        {
                            _ = sb.Append(',');
                        }
                        else
                        {
                            firstTypeParam = false;
                        }

                        _ = sb.Append(typeParams[k].Name);
                        k++;
                    }

                    _ = sb.Append(']');
                }

                ParameterInfo[]? pi = null;
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    pi = mb.GetParameters();
                }
                catch
                {
                    // The parameter info cannot be loaded, so we don't
                    // append the parameter list.
                }
#pragma warning restore CA1031 // Do not catch general exception types
                if (pi != null)
                {
                    // arguments printing
                    _ = sb.Append('(');
                    var firstParam = true;
                    for (var j = 0; j < pi.Length; j++)
                    {
                        if (!firstParam)
                        {
                            _ = sb.Append(", ");
                        }
                        else
                        {
                            firstParam = false;
                        }

                        var typeName = "<UnknownType>";
                        if (pi[j].ParameterType != null)
                        {
                            typeName = pi[j].ParameterType.Name;
                        }

                        _ = sb.Append(typeName);
                        var parameterName = pi[j].Name;
                        if (parameterName != null)
                        {
                            _ = sb.Append(' ').Append(parameterName);
                        }
                    }

                    _ = sb.Append(')');
                }

                if (methodChanged)
                {
                    // Append original method name e.g. +MoveNext()
                    _ = sb
                       .Append('+')
                       .Append(methodName)
                       .Append('(')
                       .Append(')');
                }

                // source location printing
                if (sf!.GetILOffset() != -1)
                {
                    // If we don't have a PDB or PDB-reading is disabled for the module,
                    // then the file name will be null.
                    var fileName = sf.GetFileName();

                    if (fileName != null)
                    {
                        // tack on " in c:\tmp\MyFile.cs:line 5"
                        _ = sb.AppendFormat(CultureInfo.InvariantCulture, " in {0}:line {1}", fileName, sf.GetFileLineNumber());
                    }
                }

                // TODO @rdeago 2022-08-09: It would be nice to know the value of IsLastFrameFromForeignExceptionStackTrace.
                // The only way seems to use reflection on internal members:
                //
                //   - .NET 6.0.4 has a StackFrame.IsLastFrameFromForeignExceptionStackTrace internal property
                //     https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackFrame.cs#L134
                //
                //   - .NET Framework 4.8 has a StackFrame.GetIsLastFrameFromForeignExceptionStackTrace() internal method
                //     https://referencesource.microsoft.com/#mscorlib/system/diagnostics/stackframe.cs,167
                //
                //   - ...???
                /*
                // Skip EDI boundary for async
                if (sf.IsLastFrameFromForeignExceptionStackTrace && !isAsync)
                {
                    _ = sb
                       .AppendLine()
                       .Append("--- End of stack trace from previous location ---");
                }
                */
            }
        }

        if (traceFormat == TraceFormat.TrailingNewLine)
        {
            _ = sb.AppendLine();
        }
    }

    // https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L360
    private static bool ShowInStackTrace(MethodBase mb)
    {
        if ((mb.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0)
        {
            // Aggressive Inlines won't normally show in the StackTrace; however for Tier0 Jit and
            // cross-assembly AoT/R2R these inlines will be blocked until Tier1 Jit re-Jits
            // them when they will inline. We don't show them in the StackTrace to bring consistency
            // between this first-pass asm and fully optimized asm.
            return false;
        }

#pragma warning disable CA1031 // Do not catch general exception types - Preserving original code.
        try
        {
            // RDA 2022-05-02: Check for StackTraceHiddenAttribute by name instead of by type,
            // so we can detect both our attribute and the CLR's.
            if (HasStackTraceHiddenAttribute(mb))
            {
                // Don't show where StackTraceHidden is applied to the method.
                return false;
            }

            var declaringType = mb.DeclaringType;

            // Methods don't always have containing types, for example dynamic RefEmit generated methods.
            if (declaringType != null && HasStackTraceHiddenAttribute(declaringType))
            {
                // Don't show where StackTraceHidden is applied to the containing Type of the method.
                return false;
            }
        }
        catch
        {
            // Getting the StackTraceHiddenAttribute has failed, behave as if it was not present.
            // One of the reasons can be that the method mb or its declaring type use attributes
            // defined in an assembly that is missing.
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return true;

        static bool HasStackTraceHiddenAttribute(MemberInfo member)
            => member.GetCustomAttributes(false).Any(attr => attr.GetType().FullName == StackTraceHiddenFullName);
    }

    // https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L400
    private static bool TryResolveStateMachineMethod(ref MethodBase method, out Type declaringType)
    {
        declaringType = method.DeclaringType!;

        // State machine methods always belong to nested types.
        var parentType = declaringType.DeclaringType;
        if (parentType == null)
        {
            return false;
        }

        static MethodInfo[]? GetDeclaredMethods(Type type)
            => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var methods = GetDeclaredMethods(parentType);
        if (methods == null)
        {
            return false;
        }

        var localDeclaringType = declaringType;
        foreach (var candidateMethod in methods)
        {
            var attributes = candidateMethod.GetCustomAttributes<StateMachineAttribute>(inherit: false);
            if (attributes == null)
            {
                continue;
            }

            var foundAttribute = false;
            var foundIteratorAttribute = false;
            foreach (var asma in attributes.Where(a => a.StateMachineType == localDeclaringType))
            {
                foundAttribute = true;

                // AsyncIteratorStateMachineAttribute is not present in .NET Standard 2.0,
                // but we could well be running in a .NET 6 application,
                // so we need to recognize the attribute someway.
                foundIteratorAttribute |= asma is IteratorStateMachineAttribute || asma.GetType().FullName == AsyncIteratorStateMachineAttributeFullName;
            }

            if (foundAttribute)
            {
                // If this is an iterator (sync or async), mark the iterator as changed, so it gets the + annotation
                // of the original method. Non-iterator async state machines resolve directly to their builder methods
                // so aren't marked as changed.
                method = candidateMethod;
                declaringType = candidateMethod.DeclaringType!;
                return foundIteratorAttribute;
            }
        }

        return false;
    }
}

#endif
