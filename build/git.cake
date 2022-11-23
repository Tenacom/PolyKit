// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Git repository helpers
// ---------------------------------------------------------------------------------------------

using System;
using System.Linq;

/*
 * Summary : Gets the name of the current Git branch.
 * Params  : context - The Cake context.
 * Returns : If HEAD is on a branch, the name of the branch; otherwise, the empty string.
 */
static string GetCurrentGitBranch(this ICakeContext context) => context.Exec("git", "branch --show-current").FirstOrDefault(string.Empty);

/*
 * Summary : Attempts to get information about the remote repository.
 * Params  : context - The Cake context.
 * Returns : Remote  - The Git remote name.
 *           HostUrl - The base URL of the Git repository host.
 *           Owner   - The repository owner.
 *           Name    - The repository name.
 * Remarks : - If the githubRepository argument is given, or the GITHUB_REPOSITORY environment variable is set
 *             (as it happens in GitHub Actions,) Owner and Name are taken from there, while Remote is set
 *             to the first Git remote found whose fetch URL matches them.
 *           - If GITHUB_REPOSITORY is not available, Git remote fetch URLs are parsed for Owner and Name;
 *             remotes "upstream" and "origin" are tested, in that order, in case "origin" is a fork.
 */
static bool TryGetRepositoryInfo(this ICakeContext context, out (string Remote, string HostUrl, string Owner, string Name) result)
{
    return TryGetRepositoryInfoFromGitHubActions(out result)
        || TryGetRepositoryInfoFromGitRemote("upstream", out result)
        || TryGetRepositoryInfoFromGitRemote("origin", out result);

    bool TryGetRepositoryInfoFromGitHubActions(out (string Remote, string HostUrl, string Owner, string Name) result)
    {
        var repository = context.GetOption<string>("githubRepository", string.Empty);
        if (string.IsNullOrEmpty(repository))
        {
            result = default;
            return false;
        }

        var hostUrl = context.GetOptionOrFail<string>("githubServerUrl");
        var segments = repository.Split('/');
        foreach (var remote in context.Exec("git", "remote"))
        {
            if (TryGetRepositoryInfoFromGitRemote(remote, out result)
                && string.Equals(result.HostUrl, hostUrl, StringComparison.Ordinal)
                && string.Equals(result.Owner, segments[0], StringComparison.Ordinal)
                && string.Equals(result.Name, segments[1], StringComparison.Ordinal))
            {
                return true;
            }
        }

        result = default;
        return false;
    }

    bool TryGetRepositoryInfoFromGitRemote(string remote, out (string Remote, string HostUrl, string Owner, string Name) result)
    {
        if (context.Exec("git", "remote get-url " + remote, out var output) != 0)
        {
            result = default;
            return false;
        }

        var url = output.FirstOrDefault();
        if (string.IsNullOrEmpty(url))
        {
            result = default;
            return false;
        }

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException)
        {
            result = default;
            return false;
        }

        var path = uri.AbsolutePath;
        path = path.EndsWith(".git", StringComparison.Ordinal)
            ? path.Substring(1, path.Length - 5)
            : path.Substring(1);

        var segments = path.Split('/');
        if (segments.Length != 2)
        {
            result = default;
            return false;
        }

        result = (remote, $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? null : ":" + uri.Port.ToString())}", segments[0], segments[1]);
        return true;
    }
}

/*
 * Summary : Tells whether a tag exists in the local Git repository.
 * Params  : context - The Cake context.
 *           tag     - The tag to check for.
 * Returns : True if the tag exists; false otherwise.
 */
static bool GitTagExists(this ICakeContext context, string tag) => context.Exec("git", "tag").Any(s => string.Equals(tag, s, StringComparison.Ordinal));

/*
 * Summary : Gets the latest version and the latest stable version in commit history.
 * Params  : context - The Cake context.
 * Returns : A tuple of the latest version and the latest stable version;
 * Remarks : - If no version tag is found in commit history, this method returns a tuple of two nulls.
 *           - If no stable version tag is found in commit history, this method returns a tuple of the latest version and null.
 */
static (SemanticVersion? Latest, SemanticVersion? LatestStable) GitGetLatestVersions(this ICakeContext context)
{
    context.Verbose("Looking for latest stable version tag in Git commit history...");
    var output = context.Exec("git", "log --pretty=format:%D");
    var versions = output.Where(static x => !string.IsNullOrEmpty(x))
                         .SelectMany(static x => x.Split(", "))
                         .Where(static x => x.StartsWith("tag: "))
                         .Select(static x => x.Substring(5))
                         .Select(static x => {
                            SemanticVersion? version = null;
                            var result = SemanticVersion.TryParse(x, out version);
                            return version;
                         })
                         .Where(static x => x != null);

    SemanticVersion? latest = null;
    SemanticVersion? latestStable = null;
    foreach (var version in versions)
    {
        if (latest == null)
        {
            latest = version;
        }

        if (!version.IsPrerelease)
        {
            latestStable = version;
            break;
        }
    }

    return (latest, latestStable);
}

/*
 * Summary : Sets Git user name and email.
 * Params  : context - The Cake context.
 *           name    - The name of the user.
 *           email   - The email address of the user.
 */
static void GitSetUserIdentity(this ICakeContext context, string name, string email)
{
    context.Information($"Setting Git user name to '{name}'...");
    _ = context.Exec(
        "git",
        new ProcessArgumentBuilder()
            .Append("config")
            .Append("user.name")
            .AppendQuoted(name));

    context.Information($"Setting Git user email to '{email}'...");
    _ = context.Exec(
        "git",
        new ProcessArgumentBuilder()
            .Append("config")
            .Append("user.email")
            .AppendQuoted(email));
}
