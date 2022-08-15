#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L82

/// <summary>
/// Specifies that the output will be non-null if the named parameter is non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNullIfNotNullAttribute"/> class
    /// with the associated parameter name.
    /// </summary>
    /// <param name="parameterName">
    /// The associated parameter name.
    /// The output will be non-null if the argument to the parameter specified is non-null.
    /// </param>
    public NotNullIfNotNullAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Gets the associated parameter name.
    /// </summary>
    public string ParameterName { get; }
}

#endif
