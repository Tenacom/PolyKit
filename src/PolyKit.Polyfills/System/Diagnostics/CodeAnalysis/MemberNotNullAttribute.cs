#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.MemberNotNullAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L131

/// <summary>
/// Specifies that the method or property will ensure that the listed field and property members have not-null values.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class
    /// with a field or property member.
    /// </summary>
    /// <param name="member">
    /// The field or property member that is promised to be not-null.
    /// </param>
#pragma warning disable CA1019 // Define accessors for attribute arguments - The member parameter initializes the Members property so we're fine.
    public MemberNotNullAttribute(string member)
#pragma warning restore CA1019 // Define accessors for attribute arguments
    {
        Members = new[] { member };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class
    /// with the list of field and property members.
    /// </summary>
    /// <param name="members">
    /// The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullAttribute(params string[] members)
    {
        Members = members;
    }

    /// <summary>
    /// Gets field or property member names.
    /// </summary>
    public string[] Members { get; }
}

#endif
