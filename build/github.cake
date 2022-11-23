// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#addin nuget:?package=Cake.Http&version=2.0.0
#addin nuget:?package=Octokit&version=3.0.1

#nullable enable

// ---------------------------------------------------------------------------------------------
// GitHub API helpers
// ---------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Octokit;

using SysFile = System.IO.File;

/*
 * Summary : Asynchronously creates a new draft release on the GitHub repository.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 * Returns : A Task, representing the ongoing operation, whose value will be the ID of the newly created release.
 */
static async Task<int> CreateDraftReleaseAsync(this ICakeContext context, BuildData data)
{
    var tag = data.VersionStr;
    var client = context.CreateGitHubClient();
    context.Information($"Creating a provisional draft release...");
    var newRelease = new NewRelease(tag)
    {
        Name = $"{tag} [provisional]",
        TargetCommitish = data.Branch,
        Prerelease = data.IsPrerelease,
        Draft = true,
    };

    var createReleaseResponse = await client.Repository.Release.Create(data.RepositoryOwner, data.RepositoryName, newRelease).ConfigureAwait(false);
    return createReleaseResponse.Id;
}

/*
 * Summary : Asynchronously publishes a draft release on the GitHub repository.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 *           id      - The ID of the release.
 * Returns : A Task that represents the ongoing operation.
 */
static async Task PublishReleaseAsync(this ICakeContext context, BuildData data, int id)
{
    var tag = data.VersionStr;
    var client = context.CreateGitHubClient();
    context.Information($"Generating release notes for {tag}...");
    var releaseNotesRequest = new GenerateReleaseNotesRequest(tag)
    {
        TargetCommitish = data.Branch,
    };

    var generateNotesResponse = await client.Repository.Release.GenerateReleaseNotes(data.RepositoryOwner, data.RepositoryName, releaseNotesRequest).ConfigureAwait(false);
    var body = $"We also have a [human-curated changelog]({data.RepositoryHostUrl}/{data.RepositoryOwner}/{data.RepositoryName}/blob/main/CHANGELOG.md).\n\n---\n\n"
             + generateNotesResponse.Body;

    context.Information($"Publishing the previously created release as {tag}...");
    var update = new ReleaseUpdate
    {
        TagName = tag,
        Name = tag,
        Body = body,
        Prerelease = data.IsPrerelease,
        Draft = false,
    };

    _ = await client.Repository.Release.Edit(data.RepositoryOwner, data.RepositoryName, id, update).ConfigureAwait(false);
}

/*
 * Summary : Asynchronously deletes a release and, optionally, the corresponding tag on the GitHub repository.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 *           id      - The ID of the release.
 *           tagName - The tag name, or null to not delete a tag.
 * Returns : A Task that represents the ongoing operation.
 */
static async Task DeleteReleaseAsync(this ICakeContext context, BuildData data, int id, string? tagName)
{
    context.Information("Deleting the previously created release...");
    var client = context.CreateGitHubClient();
    await client.Repository.Release.Delete(data.RepositoryOwner, data.RepositoryName, id).ConfigureAwait(false);
    if (tagName != null)
    {
        var reference = "refs/tags/" + tagName;
        context.Information($"Looking for reference '{reference}' in GitHub repository...");
        try
        {
            _ = await client.Git.Reference.Get(data.RepositoryOwner, data.RepositoryName, reference).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            context.Information($"Reference '{reference}' not found in GitHub repository.");
            return;
        }

        context.Information($"Deleting reference '{reference}' in GitHub repository...");
        await client.Git.Reference.Delete(data.RepositoryOwner, data.RepositoryName, reference).ConfigureAwait(false);
    }
}

/*
 * Summary : Asynchronously creates a workflow dispatch event on the GitHub repository.
 * Params  : context  - The Cake context.
 *           data     - Build configuration data.
 *           filename - The name of the workflow file to run, including extension.
 *           ref      - The branch or tag on which to dispatch the workflow run.
 *           inputs   - An optional anonymous object containing the inputs for the workflow.
 * Returns : A Task that represents the ongoing operation.
 */
static async Task DispatchWorkflow(this ICakeContext context, BuildData data, string filename, string @ref, object? inputs = null)
{
    context.Information($"Dispatching workflow '{filename}' on '{@ref}'...");
    object requestBody = inputs == null
        ? new { @ref = @ref }
        : new { @ref = @ref, inputs = inputs };

    var httpSettings = new HttpSettings()
        .SetAccept("application/vnd.github.v3")
        .AppendHeader("Authorization", "Token " + context.GetOptionOrFail<string>("githubToken"))
        .AppendHeader("User-Agent", "Buildvana (Win32NT 10.0.19044; amd64; en-US)")
        .SetJsonRequestBody(requestBody)
        .EnsureSuccessStatusCode(true);

    _ = await context.HttpPostAsync($"https://api.github.com/repos/{data.RepositoryOwner}/{data.RepositoryName}/actions/workflows/{filename}/dispatches", httpSettings);
}

/*
 * Summary : Sets a GitHub Actions step output.
 * Params  : context  - The Cake context.
 *           name     - The output name.
 *           value    - The output value.
 */
static void SetActionsStepOutput(this ICakeContext context, string name, string value)
{
    var outputFile = context.EnvironmentVariable("GITHUB_OUTPUT");
    Ensure(!string.IsNullOrEmpty(outputFile), "Cannot set Actions step output: GITHUB_OUTPUT not set.");
    SysFile.AppendAllLines(outputFile, new[] { $"{name}={value}" }, Encoding.UTF8);
}

static GitHubClient CreateGitHubClient(this ICakeContext context)
{
    var client = new GitHubClient(new ProductHeaderValue("Buildvana"));
    client.Credentials = new Credentials(context.GetOptionOrFail<string>("githubToken"));
    return client;
}
