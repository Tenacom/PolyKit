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
 * Params  : context - The Cake context.
 *           message - A message explaining the reason for failing the build.
 */
[DoesNotReturn]
static void Fail(this ICakeContext context, string message)
{
    context.Error(message);
    throw new CakeException(message);
}

/*
 * Summary : Fails the build with the specified message.
 *           This method does not return.
 * Type    : T - The expected return type.
 * Params  : context - The Cake context.
 *           message - A message explaining the reason for failing the build.
 * Returns : This method never returns.
 */
[DoesNotReturn]
static T Fail<T>(this ICakeContext context, string message)
{
    context.Error(message);
    throw new CakeException(message);
}

/*
 * Summary : Fails the build with the specified exit code and message.
 *           This method does not return.
 * Params  : context  - The Cake context.
 *           exitCode - The Cake exit code.
 *           message  - A message explaining the reason for failing the build.
 */
[DoesNotReturn]
static void Fail(this ICakeContext context, int exitCode, string message)
{
    context.Error(message);
    throw new CakeException(exitCode, message);
}

/*
 * Summary : Fails the build with the specified exit code and message.
 *           This method does not return.
 * Type    : T - The expected return type.
 * Params  : context  - The Cake context.
 *           exitCode - The Cake exit code.
 *           message  - A message explaining the reason for failing the build.
 * Returns : This method never returns.
 */
[DoesNotReturn]
static T Fail<T>(this ICakeContext context, int exitCode, string message)
{
    context.Error(message);
    throw new CakeException(exitCode, message);
}

/*
 * Summary : Fails the build with the specified message if a condition is not verified.
 * Params  : context   - The Cake context.
 *           condition - The condition to verify.
 *           message   - A message explaining the reason for failing the build.
 */
static void Ensure(this ICakeContext context, [DoesNotReturnIf(false)] bool condition, string message)
{
    if (!condition)
    {
        context.Error(message);
        throw new CakeException(message);
    }
}

/*
 * Summary : Fails the build with the specified message if a condition is not verified.
 * Params  : context   - The Cake context.
 *           condition - The condition to verify.
 *           exitCode  - The Cake exit code.
 *           message   - A message explaining the reason for failing the build.
 */
static void Ensure(this ICakeContext context, [DoesNotReturnIf(false)] bool condition, int exitCode, string message)
{
    if (!condition)
    {
        context.Error(message);
        throw new CakeException(exitCode, message);
    }
}
