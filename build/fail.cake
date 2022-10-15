// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Build failure helpers
// ---------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

/*
 * Summary : Fails the build with the specified message.
 *           This method does not return.
 * Params  : message - A message explaining the reason for failing the build.
 */
[DoesNotReturn]
static void Fail(string message) => throw new CakeException(message);

/*
 * Summary : Fails the build with the specified message.
 *           This method does not return.
 * Type    : T - The expected return type.
 * Params  : message - A message explaining the reason for failing the build.
 * Returns : This method never returns.
 */
[DoesNotReturn]
static T Fail<T>(string message) => throw new CakeException(message);

/*
 * Summary : Fails the build with the specified exit code and message.
 *           This method does not return.
 * Params  : exitCode - The Cake exit code.
 *           message  - A message explaining the reason for failing the build.
 */
[DoesNotReturn]
static void Fail(int exitCode, string message) => throw new CakeException(exitCode, message);

/*
 * Summary : Fails the build with the specified exit code and message.
 *           This method does not return.
 * Type    : T - The expected return type.
 * Params  : exitCode - The Cake exit code.
 *           message  - A message explaining the reason for failing the build.
 * Returns : This method never returns.
 */
[DoesNotReturn]
static T Fail<T>(int exitCode, string message) => throw new CakeException(exitCode, message);

/*
 * Summary : Fails the build with the specified message if a condition is not verified.
 * Params  : condition - The condition to verify.
 *           message   - A message explaining the reason for failing the build.
 */
static void Ensure([DoesNotReturnIf(false)] bool condition, string message)
{
    if (!condition)
    {
        throw new CakeException(message);
    }
}

/*
 * Summary : Fails the build with the specified message if a condition is not verified.
 * Params  : condition - The condition to verify.
 *           exitCode  - The Cake exit code.
 *           message   - A message explaining the reason for failing the build.
 */
static void Ensure([DoesNotReturnIf(false)] bool condition, int exitCode, string message)
{
    if (!condition)
    {
        throw new CakeException(exitCode, message);
    }
}
