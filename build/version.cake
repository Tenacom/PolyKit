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
 * Summary : Specifies how to advance the project version before publishing a release.
 * Remarks : The values of this enum are sorted in ascending order of importance,
 *           so that they may be compared. For example, Major > Minor.
 */
enum VersionAdvance
{
    /*
     * Summary : Do not touch the version number nor the unstable tag.
     */
    None,

    /*
     * Summary : Add an unstable tag if not present.
     */
    Unstable,

    /*
     * Summary : Remove the unstable tag if present.
     */
    Stable,

    /*
     * Summary : Advance the minor version and add an unstable tag.
     */
    Minor,

    /*
     * Summary : Advance the major version, reset the minor version, and add an unstable tag.
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
     * Summary : Gets an instance of VersionSpec that represents the result of applying the specified version advance
     *           to the current instance.
     * Params  : advance - A VersionAdvance enumeration value.
     *           tag     - If advance is one of Unstable, Minor, and Major, the unstable tag of the returned instance;
     *                     otherwise, this parameter is ignored.
     * Returns : Result  - The result of applying advance to the current instance.
     *           Changed - If Result is equal to the current instance, false; otherwise, true.
     */
    public (VersionSpec Result, bool Changed) Advance(VersionAdvance advance, string tag)
        => advance switch {
            VersionAdvance.Unstable => HasTag ? (this, false) : (Unstable(tag), true),
            VersionAdvance.Stable => HasTag ? (Stable(), true) : (this, false),
            VersionAdvance.Minor => (NextMinor(tag), true),
            VersionAdvance.Major => (NextMajor(tag), true),
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
     * Summary : Applies a version advance to this instance's VersionSpec.
     * Params  : advance - A VersionAdvance enumeration value.
     * Returns : If the VersionSpec property is changed as a result of the version advance, true; otherwise, false.
     * Remarks : - This method does not save the modified version.json file; you will have to call the Save method
     *             if this method returns true.
     */
    public bool AdvanceVersion(VersionAdvance advance)
    {
        (VersionSpec, var changed) = VersionSpec.Advance(advance, FirstUnstableTag);
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
