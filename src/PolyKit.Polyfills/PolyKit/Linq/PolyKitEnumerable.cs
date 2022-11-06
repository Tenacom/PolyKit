#if !NET6_0_OR_GREATER
using System;
#endif
using System.Collections;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using System.Linq;
#endif

namespace PolyKit.Linq;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* Adapted from System.Linq.Enumerable.TryGetNonEnumeratedCount in .NET 6.0.10
 * https://github.com/dotnet/runtime/blob/v6.0.4/src/libraries/System.Linq/src/System/Linq/Count.cs#L75
 */

/// <summary>
/// Combines the hash code for multiple values into a single hash code.
/// </summary>
public // polyfill!
static class PolyKitEnumerable
{
    // Conditional compilation inside XML doc blocks is not supported.
    // https://github.com/dotnet/csharplang/discussions/295
#if NET6_0_OR_GREATER
    /// <summary>
    /// Attempts to determine the number of elements in a sequence without forcing an enumeration.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">A sequence that contains elements to be counted.</param>
    /// <param name="count">When this method returns, contains the count of <paramref name="source"/> if successful,
    /// or zero if the method failed to determine the count.</param>
    /// <returns><see langword="true"/> if the count of <paramref name="source"/> can be determined without enumeration;
    /// otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>On .NET 6.0 and later, this method just calls <see cref="Enumerable.TryGetNonEnumeratedCount{TSource}"/>.
    /// The following remarks only refer to the implementation for older frameworks.</para>
    /// <para>The method performs a series of type tests, identifying common subtypes whose
    /// count can be determined without enumerating; this includes <see cref="ICollection{T}"/>
    /// and <see cref="ICollection"/>.</para>
    /// <para>The method is typically a constant-time operation, but ultimately this depends on the complexity
    /// characteristics of the underlying collection implementation.</para>
    /// </remarks>
#else
    /// <summary>
    /// Attempts to determine the number of elements in a sequence without forcing an enumeration.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">A sequence that contains elements to be counted.</param>
    /// <param name="count">When this method returns, contains the count of <paramref name="source"/> if successful,
    /// or zero if the method failed to determine the count.</param>
    /// <returns><see langword="true"/> if the count of <paramref name="source"/> can be determined without enumeration;
    /// otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>On .NET 6.0 and later, this method just calls <c>Enumerable.TryGetNonEnumeratedCount&lt;TSource&gt;</c>.
    /// The following remarks only refer to the implementation for older frameworks.</para>
    /// <para>The method performs a series of type tests, identifying common subtypes whose
    /// count can be determined without enumerating; this includes <see cref="ICollection{T}"/>
    /// and <see cref="ICollection"/>.</para>
    /// <para>The method is typically a constant-time operation, but ultimately this depends on the complexity
    /// characteristics of the underlying collection implementation.</para>
    /// </remarks>
#endif
    public static bool TryGetCountWithoutEnumerating<TSource>(this IEnumerable<TSource> source, out int count)
    {
#if NET6_0_OR_GREATER
        return Enumerable.TryGetNonEnumeratedCount(source, out count);
#else
        switch (source)
        {
            case null:
                throw new ArgumentNullException(nameof(source));
            case ICollection<TSource> genericCollection:
                count = genericCollection.Count;
                return true;
            case ICollection collection:
                count = collection.Count;
                return true;
            default:
                count = 0;
                return false;
        }
#endif
    }
}
