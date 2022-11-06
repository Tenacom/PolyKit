#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L110

/// <summary>
/// Applied to a method that will never return under any circumstance.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public // polyfill!
sealed class DoesNotReturnIfAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute"/> class
    /// with the specified parameter value.
    /// </summary>
    /// <param name="parameterValue">
    /// The condition parameter value.
    /// Code after the method will be considered unreachable by diagnostics
    /// if the argument to the associated parameter matches this value.
    /// </param>
    public DoesNotReturnIfAttribute(bool parameterValue)
    {
        ParameterValue = parameterValue;
    }

#pragma warning disable SA1623 // Property summary documentation should match accessors - "Gets a value indicating whether..." is not suitable here.
    /// <summary>
    /// Gets the condition parameter value.
    /// </summary>
    public bool ParameterValue { get; }
#pragma warning restore SA1623 // Property summary documentation should match accessors
}

#endif
