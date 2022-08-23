#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v6.0.8/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/InterpolatedStringHandlerArgumentAttribute.cs

/// <summary>
/// Indicates which arguments to a method involving an interpolated string handler should be passed to that handler.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterpolatedStringHandlerArgumentAttribute"/> class.
    /// </summary>
    /// <param name="argument">The name of the argument that should be passed to the handler.</param>
    /// <remarks><see langword="null"/> may be used as the name of the receiver in an instance method.</remarks>
#pragma warning disable CA1019 // Define accessors for attribute arguments - Preserving original code.
    public InterpolatedStringHandlerArgumentAttribute(string argument)
#pragma warning restore CA1019 // Define accessors for attribute arguments
    {
        Arguments = new string[] { argument };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterpolatedStringHandlerArgumentAttribute"/> class.
    /// </summary>
    /// <param name="arguments">The names of the arguments that should be passed to the handler.</param>
    /// <remarks><see langword="null"/> may be used as the name of the receiver in an instance method.</remarks>
    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
    {
        Arguments = arguments;
    }

    /// <summary>Gets the names of the arguments that should be passed to the handler.</summary>
    /// <remarks><see langword="null"/> may be used as the name of the receiver in an instance method.</remarks>
    public string[] Arguments { get; }
}

#endif
