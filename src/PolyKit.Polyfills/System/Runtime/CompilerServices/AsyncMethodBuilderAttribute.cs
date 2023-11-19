#if NETCOREAPP1_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.AsyncMethodBuilderAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncMethodBuilderAttribute.cs

/// <summary>
/// Indicates the type of the async method builder that should be used by a language compiler to
/// build the attributed async method or to build the attributed type when used as the return type
/// of an async method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public // polyfill!
sealed class AsyncMethodBuilderAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="AsyncMethodBuilderAttribute"/> class.</summary>
    /// <param name="builderType">The <see cref="Type"/> of the associated builder.</param>
    public AsyncMethodBuilderAttribute(Type builderType)
    {
        BuilderType = builderType;
    }

    /// <summary>Gets the <see cref="Type"/> of the associated builder.</summary>
    public Type BuilderType { get; }
}

#endif
