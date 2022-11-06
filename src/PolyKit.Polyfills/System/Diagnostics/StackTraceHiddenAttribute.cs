#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.StackTraceHiddenAttribute))]
#endif

#else

using System.Diagnostics.CodeAnalysis;
using PolyKit.Diagnostics;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTraceHiddenAttribute.cs

/// <summary>
/// Types and methods attributed with StackTraceHidden will be omitted from the stack trace text returned by
/// <see cref="PolyKitStackTraceExtensions.ToStringHidingFrames(StackTrace)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Struct, Inherited = false)]
public // polyfill!
sealed class StackTraceHiddenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StackTraceHiddenAttribute"/> class.
    /// </summary>
    public StackTraceHiddenAttribute()
    {
    }
}

#endif
