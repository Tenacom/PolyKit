#if POLYKIT_NETSDK8_0_OR_GREATER
#if NET8_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.ExperimentalAttribute))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis;

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/ExperimentalAttribute.cs

/// <summary>
/// Indicates that an API is experimental and it may change in the future.
/// </summary>
/// <remarks>
/// <para>This attribute allows call sites to be flagged with a diagnostic that indicates that an experimental feature is used.</para>
/// <para>Authors can use this attribute to ship preview features in their assemblies.</para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Assembly
  | AttributeTargets.Module
  | AttributeTargets.Class
  | AttributeTargets.Struct
  | AttributeTargets.Enum
  | AttributeTargets.Constructor
  | AttributeTargets.Method
  | AttributeTargets.Property
  | AttributeTargets.Field
  | AttributeTargets.Event
  | AttributeTargets.Interface
  | AttributeTargets.Delegate,
    Inherited = false)]
public // polyfill!
sealed class ExperimentalAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalAttribute"/> class, specifying the ID that the compiler will use
    /// when reporting a use of the API the attribute applies to.
    /// </summary>
    /// <param name="diagnosticId">The ID that the compiler will use when reporting a use of the API the attribute applies to.</param>
    public ExperimentalAttribute(string diagnosticId)
    {
        DiagnosticId = diagnosticId;
    }

    /// <summary>
    /// Gets the ID that the compiler will use when reporting a use of the API the attribute applies to.
    /// </summary>
    /// <value>The unique diagnostic ID.</value>
    /// <remarks>
    /// <para>The diagnostic ID is shown in build output for warnings and errors.</para>
    /// <para>This property represents the unique ID that can be used to suppress the warnings or errors, if needed.</para>
    /// </remarks>
    public string DiagnosticId { get; }

    /// <summary>
    /// <para>Gets or sets the URL for corresponding documentation.</para>
    /// <para>The API accepts a format string instead of an actual URL, creating a generic URL that includes the diagnostic ID.</para>
    /// </summary>
    /// <value>The format string that represents a URL to corresponding documentation.</value>
    /// <remarks>
    /// <para>An example format string is <c>https://contoso.com/obsoletion-warnings/{0}</c>.</para>
    /// </remarks>
    public string? UrlFormat { get; set; }
}

#endif
#endif
