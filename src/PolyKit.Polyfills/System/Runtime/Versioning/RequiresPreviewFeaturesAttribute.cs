#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Versioning.RequiresPreviewFeaturesAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Runtime.Versioning;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/Versioning/RequiresPreviewFeaturesAttribute.cs

/// <summary>
/// <para>Indicates that an API is in preview.</para>
/// <para>This attribute allows call sites to be flagged with a diagnostic that indicates that a preview feature is used.
/// Authors can use this attribute to ship preview features in their assemblies.</para>
/// </summary>
/// <remarks>
/// <para>See also <see href="https://aka.ms/dotnet-warnings/preview-features">Preview Features</see>.</para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Assembly |
    AttributeTargets.Module |
    AttributeTargets.Class |
    AttributeTargets.Interface |
    AttributeTargets.Delegate |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Constructor |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Event,
    Inherited = false)]
public // polyfill!
sealed class RequiresPreviewFeaturesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresPreviewFeaturesAttribute"/> class.
    /// </summary>
    public RequiresPreviewFeaturesAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresPreviewFeaturesAttribute"/> class with the specified message.
    /// </summary>
    /// <param name="message">An optional message associated with this attribute instance.</param>
    public RequiresPreviewFeaturesAttribute(string? message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the optional message associated with this attribute instance.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets or sets the optional URL associated with this attribute instance.
    /// </summary>
    public string? Url { get; set; }
}

#endif
