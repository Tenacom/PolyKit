// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

// Do not use #addin because the assembly is distributed within Cake.Tool
#r NuGet.Versioning

#nullable enable

using NuGet.Versioning;

// ---------------------------------------------------------------------------------------------
// Version management helpers
// ---------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

/*
 * Summary : Specifies how to modify the version specification upon publishing a release.
 */
enum VersionSpecChange
{
    /*
     * Summary : Do not force a version increment; do not modify the unstable tag.
     */
    None,

    /*
     * Summary : Do not force a version increment; add an unstable tag if not present.
     */
    Unstable,

    /*
     * Summary : Do not force a version increment; remove the unstable tag if present.
     */
    Stable,

    /*
     * Summary : Force a minor version increment with respect to the latest stable version; add an unstable tag.
     */
    Minor,

    /*
     * Summary : Force a major version increment and minor version reset with respect to the latest stable version; add an unstable tag.
     */
    Major,
}

/*
 * Summary : Specifies a kind of version increment.
 * Remarks : The values of this enum are sorted in ascending order of importance,
 *           so that they may be compared.
 */
enum VersionIncrement
{
    /*
     * Summary : Represents no version advancement.
     */
    None,

    /*
     * Summary : Represents the increment of minor version.
     */
    Minor,

    /*
     * Summary : Represents the increment of major version and reset of minor version.
     */
    Major,
}

/*
 * Summary : Represents a Major.Minor[-Tag] version as found in version.json.
 */
sealed record VersionSpec
{
    private static readonly Regex VersionSpecRegex = new Regex(
        @"(?-imsx)^v?(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)(-(?<tag>.*))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private VersionSpec(int major, int minor, string tag)
    {
        Major = major;
        Minor = minor;
        Tag = tag;
    }

    /*
     * Summary : Gets the major version.
     */
    public int Major { get; }

    /*
     * Summary : Gets the minor version.
     */
    public int Minor { get; }

    /*
     * Summary : Gets current unstable tag.
     * Value   : The current unstable tag, or the empty string if the current version is stable.
     */
    public string Tag { get; }

    /*
     * Summary : Gets a value indicating whether this instance has an unstable tag.
     */
    public bool HasTag => !string.IsNullOrEmpty(Tag);

    /*
     * Summary : Attempts to parse a VersionSpec from the specified string.
     * Params  : str    - The string to parse.
     *           result - When this method returns true, the parsed VersionSpec.
     * Returns : True is successful; false otherwise.
     */
    public static bool TryParse(string str, [MaybeNullWhen(false)] out VersionSpec result)
    {
        var match = VersionSpecRegex.Match(str);
        if (!match.Success)
        {
            result = null;
            return false;
        }

        result = new(
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            match.Groups["tag"].Value
        );

        return true;
    }

    public override string ToString() => $"{Major}.{Minor}{(HasTag ? "-" + Tag : null)}";

    /*
     * Summary : Gets an instance of VersionSpec that represents the same version as the current instance
     *           and has no unstable tag.
     * Returns : If this instance has no unstable tag, this instance; otherwise, a newly-constructed VersionSpec
     *           that represents the same version as the current instance and has no unstable tag.
     */
    public VersionSpec Stable() => HasTag ? new(Major, Minor, string.Empty) : this;

    /*
     * Summary : Gets an instance of VersionSpec that represents the same version as the current instance
     *           and has the specified unstable tag.
     * Params  : tag - The unstable tag of the returned instance.
     * Returns : If this instance's Tag property is equal to the given tag, this instance; otherwise, a newly-constructed VersionSpec
     *           that represents the same version as the current instance and has the specified unstable tag.
     */
    public VersionSpec Unstable(string tag) => string.Equals(Tag, tag, StringComparison.Ordinal) ? this : new(Major, Minor, tag);

    /*
     * Summary : Gets an instance of VersionSpec that represents the next minor version with respect to the current instance
     *           and has the specified unstable tag.
     * Params  : tag - The unstable tag of the returned instance.
     * Returns : A newly-constructed VersionSpec.
     */
    public VersionSpec NextMinor(string tag) => new(Major, Minor + 1, tag);

    /*
     * Summary : Gets an instance of VersionSpec that represents the next major version with respect to the current instance
     *           and has the specified unstable tag.
     * Params  : tag - The unstable tag of the returned instance.
     * Returns : A newly-constructed VersionSpec.
     */
    public VersionSpec NextMajor(string tag) => new(Major + 1, 0, tag);

    /*
     * Summary : Gets an instance of VersionSpec that represents the result of applying the specified change
     *           to the current instance.
     * Params  : action - An enumeration value representing the kind of change to apply.
     *           tag    - If the returned instance has an unstable tag, the unstable tag of the returned instance;
     *                    otherwise, this parameter is ignored.
     * Returns : Result  - The result of applying action to the current instance.
     *           Changed - If Result is equal to the current instance, false; otherwise, true.
     */
    public (VersionSpec Result, bool Changed) ApplyChange(VersionSpecChange change, string tag)
        => change switch {
            VersionSpecChange.Unstable => HasTag ? (this, false) : (Unstable(tag), true),
            VersionSpecChange.Stable => HasTag ? (Stable(), true) : (this, false),
            VersionSpecChange.Minor => (NextMinor(tag), true),
            VersionSpecChange.Major => (NextMajor(tag), true),
            _ => (this, false),
        };
}

/*
 * Summary : Represents the version.json file, for the purpose of applying version advances.
 */
sealed class VersionFile
{
    private const string VersionJsonPath = "version.json";
    private const string DefaultFirstUnstableTag = "preview";

    private readonly JsonNode _json;

    private VersionFile(FilePath path, JsonNode json, VersionSpec versionSpec, string firstUnstableTag)
    {
        Path = path;
        _json = json;
        VersionSpec = versionSpec;
        FirstUnstableTag = firstUnstableTag;
    }

    /*
     * Summary : Gets the FilePath of the version.json file.
     */
    public FilePath Path { get; }

    /*
     * Summary : Gets a VersionSpec representing the "version" value in the version.json file.
     */
    public VersionSpec VersionSpec { get; private set; }

    /*
     * Summary : Gets the unstable tag to use for version advances.
     * Value   : Either the "release.firstUnstableTag" value read from version.json,
     *           or "preview" as a default value.
     */
    public string FirstUnstableTag { get; private init; }

    /*
     * Summary : Constructs a VersionFile instance by loading the repository's version.json file.
     * Returns : A newly-constructed instance of VersionFile, representing the loaded data.
     */
    public static VersionFile Load()
    {
        var path = new FilePath(VersionJsonPath);
        var json = LoadJsonObject(path);
        var versionStr = GetJsonPropertyValue<string>(json, "version", path + " file");
        Ensure(VersionSpec.TryParse(versionStr, out var versionSpec), $"{VersionJsonPath} contains invalid version specification '{versionStr}'.");
        var firstUnstableTag = DefaultFirstUnstableTag;
        var release = json["release"];
        if (release is not null)
        {
            var firstUnstableTagNode = release["firstUnstableTag"];
            if (firstUnstableTagNode is JsonValue firstUnstableTagValue && firstUnstableTagValue.TryGetValue<string>(out var firstUnstableTagStr) && !string.IsNullOrEmpty(firstUnstableTagStr))
            {
                firstUnstableTag = firstUnstableTagStr;
            }
        }

        return new(path, json, versionSpec, firstUnstableTag);
    }

    /*
     * Summary : Applies a version spec change to this instance.
     * Params  : context - The Cake context.
     *           change  - An enumeration value representing the kind of change to apply.
     * Returns : If the VersionSpec property is actually changed as a result of change, true; otherwise, false.
     * Remarks : - This method does not save the modified version.json file; you will have to call the Save method
     *             if this method returns true.
     */
    public bool ApplyVersionSpecChange(ICakeContext context, VersionSpecChange change)
    {
        var previousVersionSpec = VersionSpec;
        (VersionSpec, var changed) = VersionSpec.ApplyChange(change, FirstUnstableTag);
        if (changed)
        {
            context.Information($"Version spec changed from {previousVersionSpec} to {VersionSpec}.");
        }
        else
        {
            context.Information("Version spec not changed.");
        }

        return changed;
    }

    /*
     * Summary : Saves the version.json file, possibly with a modified VersionSpec, back to the repository.
     */
    public void Save()
    {
        _json["version"] = JsonValue.Create(VersionSpec.ToString());
        SaveJson(_json, Path);
    }
}

/*
 * Summary : Computes the VersionSpecChange to apply upon release.
 * Params  : context         - The Cake context.
 *           currentVersion  - The current version as computed by NBGV
 *           requestedChange - The version spec change requested by the user
 *           checkPublicApi  - If true, account for changes in public API files.
 * Returns : The actual change to apply .
 */
static VersionSpecChange ComputeVersionSpecChange(
    this ICakeContext context,
    SemanticVersion currentVersion,
    VersionSpecChange requestedChange,
    bool checkPublicApi)
{
    // Throw if versions are messed up
    var (latestVersion, latestStableVersion) = context.GitGetLatestVersions();
    context.Information($"Latest version is {latestVersion?.ToString() ?? "(none)"}");
    context.Information($"Latest stable version is {latestStableVersion?.ToString() ?? "(none)"}");
    Ensure(
        VersionComparer.Compare(currentVersion, latestStableVersion, VersionComparison.Version) > 0,
        $"Versioning anomaly detected: current version ({currentVersion}) is not higher than than latest stable version ({latestStableVersion?.ToString() ?? "none"}).");
    Ensure(
        VersionComparer.Compare(currentVersion, latestStableVersion, VersionComparison.Version) > 0,
        $"Versioning anomaly detected: latest version ({latestVersion?.ToString() ?? "none"}) is not higher than than latest stable version ({latestStableVersion?.ToString() ?? "none"}).");
    Ensure(
        VersionComparer.Compare(currentVersion, latestVersion, VersionComparison.Version) > 0,
        $"Versioning anomaly detected: current version ({currentVersion}) is not higher than than last stable version ({latestStableVersion?.ToString() ?? "none"}).");

    // Determine how we are currently already incrementing version
    var currentVersionIncrement = latestStableVersion == null ? VersionIncrement.Major
                                : currentVersion.Major > latestStableVersion.Major ? VersionIncrement.Major
                                : currentVersion.Minor > latestStableVersion.Minor ? VersionIncrement.Minor
                                : VersionIncrement.None;
    context.Information($"Current version increment: {currentVersionIncrement}");

    // Determine the kind of change in public API
    var publicApiChangeKind = checkPublicApi ? context.GetPublicApiChangeKind() : ApiChangeKind.None;
    context.Information($"Public API change kind: {publicApiChangeKind}{(checkPublicApi ? null : " (not checked)")}");

    // Determine the version increment required by SemVer rules
    var isInitialDevelopmentPhase = latestStableVersion == null || latestStableVersion.Major == 0;
    var semanticVersionIncrement = publicApiChangeKind switch {
        ApiChangeKind.Breaking => isInitialDevelopmentPhase ? VersionIncrement.Minor : VersionIncrement.Major,
        ApiChangeKind.Additive => isInitialDevelopmentPhase ? VersionIncrement.None : VersionIncrement.Minor,
        _ => VersionIncrement.None,
    };
    context.Information($"Required version increment according to Semantic Versioning rules: {semanticVersionIncrement}");

    // Determine the requested version increment, if any.
    context.Information($"Requested version spec change: {requestedChange}");
    var requestedVersionIncrement = requestedChange switch {
        VersionSpecChange.Major => VersionIncrement.Major,
        VersionSpecChange.Minor => VersionIncrement.Minor,
        _ => VersionIncrement.None,
    };
    context.Information($"Requested version increment: {requestedVersionIncrement}.");

    // Adjust requested version increment to follow SemVer rules
    if (semanticVersionIncrement > requestedVersionIncrement)
    {
        requestedVersionIncrement = semanticVersionIncrement;
    }

    // Determine the kind of version increment actually required
    var actualVersionIncrement = requestedVersionIncrement > currentVersionIncrement ? requestedVersionIncrement : VersionIncrement.None;
    context.Information($"Required version increment with respect to current version: {actualVersionIncrement}");

    // Determine the actual version spec change to apply:
    //   - forget any increment-related change (already accounted for via requestedVersionIncrement)
    //   - set the change to the required increment if any, otherwise leave it as is (None, Unstable, Stable)
    var actualChange = requestedChange switch {
        VersionSpecChange.Major or VersionSpecChange.Minor => VersionSpecChange.None,
        _ => requestedChange,
    };
    actualChange = actualVersionIncrement switch {
        VersionIncrement.Major => VersionSpecChange.Major,
        VersionIncrement.Minor => VersionSpecChange.Minor,
        _ => actualChange,
    };
    context.Information($"Actual version spec change: {actualChange}.");

    return actualChange;
}
