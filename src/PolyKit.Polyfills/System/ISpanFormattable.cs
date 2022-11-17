#if POLYKIT_USE_SPAN
#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.ISpanFormattable))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System;

// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/ISpanFormattable.cs

/// <summary>Provides functionality to format the string representation of an object into a span.</summary>
public // polyfill!-
interface ISpanFormattable : IFormattable
{
    /// <summary>Tries to format the value of the current instance into the provided span of characters.</summary>
    /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
    /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
    /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
    /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
    /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>An implementation of this interface should produce the same string of characters as an implementation of <see cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    /// on the same type.</para>
    /// <para>TryFormat should return false only if there is not enough space in the destination buffer. Any other failures should throw an exception.</para>
    /// </remarks>
    bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider);
}

#endif
#endif
