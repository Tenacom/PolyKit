﻿#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.SkipLocalsInitAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/SkipLocalsInitAttribute.cs

/// <summary>
/// Used to indicate to the compiler that the <c>.locals init</c>
/// flag should not be set in method headers.
/// </summary>
/// <remarks>
/// This attribute is unsafe because it may reveal uninitialized memory to
/// the application in certain instances (e.g., reading from uninitialized
/// stackalloc'd memory). If applied to a method directly, the attribute
/// applies to that method and all nested functions (lambdas, local
/// functions) below it. If applied to a type or module, it applies to all
/// methods nested inside. This attribute is intentionally not permitted on
/// assemblies. Use at the module level instead to apply to multiple type
/// declarations.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Module
  | AttributeTargets.Class
  | AttributeTargets.Struct
  | AttributeTargets.Interface
  | AttributeTargets.Constructor
  | AttributeTargets.Method
  | AttributeTargets.Property
  | AttributeTargets.Event,
    Inherited = false)]
public // polyfill!
sealed class SkipLocalsInitAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipLocalsInitAttribute"/> class.
    /// </summary>
    public SkipLocalsInitAttribute()
    {
    }
}

#endif
