#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/RequiresAssemblyFilesAttribute.cs

/// <summary>
/// Indicates that the specified member requires assembly files to be on disk.
/// </summary>
[AttributeUsage(
    AttributeTargets.Constructor |
    AttributeTargets.Event |
    AttributeTargets.Method |
    AttributeTargets.Property,
    Inherited = false,
    AllowMultiple = false)]
public // polyfill!
sealed class RequiresAssemblyFilesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresAssemblyFilesAttribute"/> class.
    /// </summary>
    public RequiresAssemblyFilesAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresAssemblyFilesAttribute"/> class.
    /// </summary>
    /// <param name="message">
    /// A message that contains information about the need for assembly files to be on disk.
    /// </param>
    public RequiresAssemblyFilesAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets an optional message that contains information about the need for
    /// assembly files to be on disk.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets or sets an optional URL that contains more information about the member,
    /// why it requires assembly files to be on disk, and what options a consumer has
    /// to deal with it.
    /// </summary>
    public string? Url { get; set; }
}

#endif
