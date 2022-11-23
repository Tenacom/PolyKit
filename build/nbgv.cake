// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Nerdbank.GitVersioning helpers
// ---------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json.Nodes;

/*
 * Summary : Gets version information using the NBGV tool.
 * Params  : context - The Cake context.
 * Returns : VersionStr      - The project version.
 *           Ref             - The Git ref from which we are building.
 *           IsPublicRelease - True if a public release can be built, false otherwise.
 *           IsPrerelease    - True if the project version is tagged as prerelease, false otherwise.
 */
static (string VersionStr, string Ref, bool IsPublicRelease, bool IsPrerelease) GetVersionInformation(this ICakeContext context)
{
    var nbgvOutput = new StringBuilder();
    context.DotNetTool(
        "nbgv get-version --format json",
        new DotNetToolSettings {
            SetupProcessSettings = s => s
                .SetRedirectStandardOutput(true)
                .SetRedirectedStandardOutputHandler(x => {
                    nbgvOutput.AppendLine(x);
                    return x;
                }),
        });

    var json = ParseJsonObject(nbgvOutput.ToString(), "The output of nbgv");
    return (
        VersionStr: GetJsonPropertyValue<string>(json, "NuGetPackageVersion", "the output of nbgv"),
        Ref: GetJsonPropertyValue<string>(json, "BuildingRef", "the output of nbgv"),
        IsPublicRelease: GetJsonPropertyValue<bool>(json, "PublicRelease", "the output of nbgv"),
        IsPrerelease: !string.IsNullOrEmpty(GetJsonPropertyValue<string>(json, "PrereleaseVersion", "the output of nbgv")));
}
