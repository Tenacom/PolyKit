#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ModuleInitializerAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/ModuleInitializerAttribute.cs

/// <summary>
/// Used to indicate to the compiler that a method should be called
/// in its containing module's initializer.
/// </summary>
/// <remarks>
/// <para>When one or more valid methods with this attribute are found in a compilation,
/// the compiler will emit a module initializer which calls each of the attributed methods.</para>
/// <para>Certain requirements are imposed on any method targeted with this attribute:</para>
/// <list type="bullet">
/// <item>The method must be <see langword="static"/>.</item>
/// <item>The method must be an ordinary member method, as opposed to a property accessor, constructor, local function, etc.</item>
/// <item>The method must be parameterless.</item>
/// <item>The method must return <see langword="void"/>.</item>
/// <item>The method must not be generic or be contained in a generic type.</item>
/// <item>The method's effective accessibility must be <see langword="internal"/> or <see langword="public"/>.</item>
/// </list>
/// <para>For more information, see <see href="https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md#module-initializer"
/// >the specification for module initializers</see>.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public // polyfill!
sealed class ModuleInitializerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleInitializerAttribute"/> class.
    /// </summary>
    public ModuleInitializerAttribute()
    {
    }
}

#endif
