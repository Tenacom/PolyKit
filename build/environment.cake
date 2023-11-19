// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Environment helpers
// ---------------------------------------------------------------------------------------------

/*
 * Summary : Gets a string from the environment, failing if the value is not found or is the empty string.
 * Params  : context      - The Cake context.
 *           name         - The name of the environment variable to read.
 *           fallbackName - The name of another environment variable to read if name is not found or its value is the empty string.
 * Returns : The value of an environment variable.
 */
static string GetEnvironmentString(this ICakeContext context, string name, string fallbackName = "")
{
    var result = context.EnvironmentVariable<string>(name, string.Empty);
    if (!string.IsNullOrEmpty(result))
    {
        return result;
    }

    context.Ensure(!string.IsNullOrEmpty(fallbackName), $"Environment variable {name} is missing or has an empty value.");
    result = context.EnvironmentVariable<string>(fallbackName, string.Empty);
    context.Ensure(!string.IsNullOrEmpty(result), 255, $"Both environment variables {name} and {fallbackName} are missing or have an empty value.");
    return result;
}
