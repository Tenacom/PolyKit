#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v6.0.8/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/InterpolatedStringHandlerAttribute.cs

/// <summary>
/// Indicates the attributed type is to be used as an interpolated string handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class InterpolatedStringHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterpolatedStringHandlerAttribute"/> class.
    /// </summary>
    public InterpolatedStringHandlerAttribute()
    {
    }
}

#endif
