#if NET6_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PolyKit.Diagnostics;

/// <summary>
/// Provides extension methods for exceptions to support polyfilled features.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
static class PolyKitExceptionExtensions
{
    /// <summary>
    /// Gets a string representation of the immediate frames on the call stack.
    /// This method returns the value of the exception's <see cref="Exception.StackTrace">StackTrace</see> property.
    /// </summary>
    /// <param name="this">The <see cref="Exception"/> on which this method is called.</param>
    /// <returns>A string that describes the immediate frames of the call stack.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetStackTraceHidingFrames(this Exception @this) => @this.StackTrace;
}

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PolyKit.Diagnostics.Internal;

namespace PolyKit.Diagnostics;

/// <summary>
/// Provides extension methods for exceptions to support polyfilled features.
/// </summary>
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
static class PolyKitExceptionExtensions
{
    /// <summary>
    /// <para>Gets a string representation of the immediate frames on the call stack,
    /// hiding stack frames marked with <see cref="StackTraceHiddenAttribute"/>.</para>
    /// </summary>
    /// <param name="this">The <see cref="Exception"/> on which this method is called.</param>
    /// <returns>A string that describes the immediate frames of the call stack.</returns>
    /// <remarks>
    /// <para>Unlike <see cref="Exception.StackTrace"/>, this method honors the presence of
    /// <see cref="StackTraceHiddenAttribute"/> on stack frame methods.</para>
    /// <para>There are, however, some limitations:</para>
    /// <list type="bullet">
    /// <item><description>the returned stack trace description is always in English, irrespective of current culture;</description></item>
    /// <item><description>external exception stack trace boundaries ("End of stack trace from previous location" lines) are missing from the returned string.</description></item>
    /// </list>
    /// </remarks>
    /* Adapted from System.Exception.GetStackTrace() in .NET 6.0.4
     * https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Exception.cs#L223
     */
    // Do not include a trailing newline for backwards compatibility
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetStackTraceHidingFrames(this Exception @this)
        => new StackTrace(@this, fNeedFileInfo: true)
           .ToStringHidingFrames(TraceFormat.Normal);
}

#endif
