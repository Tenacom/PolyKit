﻿#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.NotNullAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L34

/// <summary>
/// Specifies that an output will not be null even if the corresponding type allows it.
/// Specifies that an input argument was not null when the call returns.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
public // polyfill!
sealed class NotNullAttribute : Attribute
{
}

#endif
