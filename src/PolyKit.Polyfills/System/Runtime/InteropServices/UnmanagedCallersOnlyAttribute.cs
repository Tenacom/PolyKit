#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute))]
#endif

#else

#pragma warning disable SA1401 // Fields should be private

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Runtime.InteropServices;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/UnmanagedCallersOnlyAttribute.cs

/// <summary>
/// <para>Any method marked with this attribute can be directly called from native code.
/// The function token can be loaded to a local variable using the
/// <see href="https://docs.microsoft.com/dotnet/csharp/language-reference/operators/pointer-related-operators#address-of-operator-">address-of</see>
/// operator in C# and passed as a callback to a native method.</para>
/// </summary>
/// <remarks>
/// <para>Methods marked with this attribute have the following restrictions:</para>
/// <list type="bullet">
/// <item>Method must be marked <see langword="static"/>.</item>
/// <item>Must not be called from managed code.</item>
/// <item>Must only have <see href="https://docs.microsoft.com/dotnet/framework/interop/blittable-and-non-blittable-types">blittable</see> arguments.</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public // polyfill!
sealed class UnmanagedCallersOnlyAttribute : Attribute
{
    /// <summary>
    /// Optional. If omitted, the runtime will use the default platform calling convention.
    /// </summary>
    /// <remarks>
    /// Supplied types must be from the official "System.Runtime.CompilerServices" namespace and
    /// be of the form "CallConvXXX".
    /// </remarks>
    public Type[]? CallConvs;

    /// <summary>
    /// Optional. If omitted, no named export is emitted during compilation.
    /// </summary>
    public string? EntryPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedCallersOnlyAttribute"/> class.
    /// </summary>
    public UnmanagedCallersOnlyAttribute()
    {
    }
}

#endif
