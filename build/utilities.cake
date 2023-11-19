// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Miscellaneous utilities
// ---------------------------------------------------------------------------------------------

using System;
using System.Linq;

/*
 * Summary     : Filters a sequence of nullable values, taking only those that are not null.
 * Type params : T - The type of the elements of this.
 * Params      : this - An IEnumerable<T> to filter.</param>
 * Returns     : An IEnumerable<T> that contains elements from the input sequence that are not null.
 */
static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> @this)
    where T : class
{
    return @this.Where(IsNotNull) as IEnumerable<T>;

    static bool IsNotNull(T? x) => x is not null;
}

/*
 * Summary     : Filters a sequence of nullable values, taking only those that are not null.
 * Type params : T - The type of the elements of this.
 * Params      : this - An IEnumerable<T> to filter.</param>
 * Returns     : An IEnumerable<T> that contains elements from the input sequence that are not null.
 */
public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> @this)
    where T : struct
{
    return @this.Where(IsNotNull).Select(GetValue);

    static bool IsNotNull(T? x) => x.HasValue;

    static T GetValue(T? x) => x!.Value;
}
