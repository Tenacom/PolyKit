#if POLYKIT_NETSDK7_0_OR_GREATER
#if NET7_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.ConstantExpectedAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/ConstantExpectedAttribute.cs

/// <summary>
/// Indicates that the specified method parameter expects a constant.
/// </summary>
/// <remarks>
/// This can be used to inform tooling that a constant should be used as an argument for the annotated parameter.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public // polyfill!
sealed class ConstantExpectedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the minimum bound of the expected constant, inclusive.
    /// </summary>
    public object? Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum bound of the expected constant, inclusive.
    /// </summary>
    public object? Max { get; set; }
}

#endif
#endif
