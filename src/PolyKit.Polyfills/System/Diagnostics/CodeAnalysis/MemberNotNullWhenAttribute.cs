#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L156

/// <summary>
/// Specifies that the method or property will ensure that the listed field and property members
/// have not-null values when returning with the specified return value condition.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class MemberNotNullWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class
    /// with the specified return value condition and a field or property member.
    /// </summary>
    /// <param name="returnValue">
    /// The return value condition.
    /// If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="member">
    /// The field or property member that is promised to be not-null.
    /// </param>
#pragma warning disable CA1019 // Define accessors for attribute arguments - The member parameter initializes the Members property so we're fine.
    public MemberNotNullWhenAttribute(bool returnValue, string member)
#pragma warning restore CA1019 // Define accessors for attribute arguments
    {
        ReturnValue = returnValue;
        Members = new[] { member };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class
    /// with the specified return value condition and list of field and property members.
    /// </summary>
    /// <param name="returnValue">
    /// The return value condition.
    /// If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="members">
    /// The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }

#pragma warning disable SA1623 // Property summary documentation should match accessors - "Gets a value indicating whether..." is not suitable here.
    /// <summary>
    /// Gets the return value condition.
    /// </summary>
    public bool ReturnValue { get; }
#pragma warning restore SA1623 // Property summary documentation should match accessors

    /// <summary>
    /// Gets field or property member names.
    /// </summary>
    public string[] Members { get; }
}

#endif
