﻿#if NET6_0_OR_GREATER

namespace System;

/// <summary>
/// Provides extension methods for exceptions to support polyfilled features.
/// </summary>
public // polyfill!
static class PolyKitExceptionExtensions
{
    /// <summary>
    /// Gets a string representation of the immediate frames on the call stack.
    /// This method returns the value of the exception's <see cref="Exception.StackTrace">StackTrace</see> property.
    /// </summary>
    /// <param name="this">The <see cref="Exception"/> on which this method is called.</param>
    /// <returns>A string that describes the immediate frames of the call stack.</returns>
    public static string? GetStackTraceHidingFrames(this Exception @this) => @this.StackTrace;
}

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace System;

/// <summary>
/// Provides extension methods for exceptions to support polyfilled features.
/// </summary>
public // polyfill!
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
    /* Adapted from System.Exception.GetStackTrace() in .NET 8.0.0
     * https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Exception.cs#L229
     */
    // Do not include a trailing newline for backwards compatibility
    public static string GetStackTraceHidingFrames(this Exception @this)
        => new StackTrace(@this, fNeedFileInfo: true).ToStringHidingFrames(TraceFormat.Normal);
}

#endif
