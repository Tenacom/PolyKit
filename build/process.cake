// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Process helpers
// ---------------------------------------------------------------------------------------------

using System.Collections.Generic;

/*
 * Summary : Executes an external command, capturing standard output and failing if the exit code is not zero.
 * Params  : context   - The Cake context.
 *           command   - The name of the command to execute.
 *           arguments - The arguments to pass to the command.
 * Returns : The captured output of the command.
 */
static IEnumerable<string> Exec(this ICakeContext context, string command, ProcessArgumentBuilder arguments)
{
    var exitCode = context.Exec(command, arguments, out var output);
    context.Ensure(exitCode == 0, $"'{command} {arguments.RenderSafe()}' exited with code {exitCode}.");
    return output;
}

/*
 * Summary : Executes an external command, capturing standard output and failing if the exit code is not zero.
 * Params  : context    - The Cake context.
 *           command    - The name of the command to execute.
 *           arguments  - The arguments to pass to the command.
 *           out output - The captured output of the command.
 * Returns : The exit code of the command.
 */
static int Exec(this ICakeContext context, string command, ProcessArgumentBuilder arguments, out IEnumerable<string> output)
    => context.StartProcess(
        command,
        new ProcessSettings { Arguments = arguments, RedirectStandardOutput = true },
        out output);
