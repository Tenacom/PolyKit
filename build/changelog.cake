// Copyright (C) Tenacom and contributors. Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#nullable enable

// ---------------------------------------------------------------------------------------------
// Changelog management helpers
// ---------------------------------------------------------------------------------------------

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using SysFile = System.IO.File;

/*
 * Summary : Checks the changelog for contents in the "Unreleased changes" section.
 * Params  : context       - The Cake context.
 *           changelogPath - The FilePath of the changelog.
 * Returns : If there are any contents (excluding blank lines and sub-section headings)
 *           in the "Unreleased changes" section, true; otherwise, false.
 */
static bool ChangelogHasUnreleasedChanges(this ICakeContext context, FilePath changelogPath)
{
    using (var reader = new StreamReader(changelogPath.FullPath, Encoding.UTF8))
    {
        var sectionHeadingRegex = new Regex(@"^ {0,3}##($|[^#])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var subSectionHeadingRegex = new Regex(@"^ {0,3}###($|[^#])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        string? line;
        do
        {
            line = reader.ReadLine();
        } while (line != null && !sectionHeadingRegex.IsMatch(line));

        Ensure(line != null, $"{changelogPath.GetFilename()} contains no sections.");
        for (; ;)
        {
            line = reader.ReadLine();
            if (line == null || sectionHeadingRegex.IsMatch(line))
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(line) && !subSectionHeadingRegex.IsMatch(line))
            {
                return true;
            }
        }
    }

    return false;
}

/*
 * Summary : Prepares the changelog for a release by moving the contents of the "Unreleased changes" section
 *           to a new section.
 * Params  : context - The Cake context.
 *           data    - Build configuration data.
 */
static void PrepareChangelogForRelease(this ICakeContext context, BuildData data)
{
    context.Information("Updating changelog...");
    var encoding = new UTF8Encoding(false, true);
    var sb = new StringBuilder();
    using (var reader = new StreamReader(data.ChangelogPath.FullPath, encoding))
    using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
    {
        // Using a StringWriter instead of a StringBuilder allows for a custom line separator
        // Under Windows, a StringBuilder would only use "\r\n" as a line separator, which would be wrong in this case
        writer.NewLine = "\n";
        var sectionHeadingRegex = new Regex(@"^ {0,3}##($|[^#])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var subSectionHeadingRegex = new Regex(@"^ {0,3}###($|[^#])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var subSections = new List<(string Header, List<string> Lines)>();
        subSections.Add(("", new List<string>()));
        var subSectionIndex = 0;

        const int ReadingFileHeader = 0;
        const int ReadingUnreleasedChangesSection = 1;
        const int ReadingRemainderOfFile = 2;
        const int ReadingDone = 3;
        var state = ReadingFileHeader;
        while (state != ReadingDone)
        {
            var line = reader.ReadLine();
            switch (state)
            {
                case ReadingFileHeader:
                    Ensure(line != null, $"{data.ChangelogPath.GetFilename()} contains no sections.");

                    // Copy everything up to an including the first section heading (which we assume is "Unreleased changes")
                    writer.WriteLine(line);
                    if (sectionHeadingRegex.IsMatch(line))
                    {
                        state = ReadingUnreleasedChangesSection;
                    }

                    break;
                case ReadingUnreleasedChangesSection:
                    if (line == null)
                    {
                        // The changelog only contains the "Unreleased changes" section;
                        // this happens when no release has been published yet
                        WriteNewSections(true);
                        state = ReadingDone;
                        break;
                    }

                    if (sectionHeadingRegex.IsMatch(line))
                    {
                        // Reached header of next section
                        WriteNewSections(false);
                        writer.WriteLine(line);
                        state = ReadingRemainderOfFile;
                        break;
                    }

                    if (subSectionHeadingRegex.IsMatch(line))
                    {
                        subSections.Add((line, new List<string>()));
                        ++subSectionIndex;
                        break;
                    }

                    subSections[subSectionIndex].Lines.Add(line);
                    break;
                case ReadingRemainderOfFile:
                    if (line == null)
                    {
                        state = ReadingDone;
                        break;
                    }

                    writer.WriteLine(line);
                    break;
                default:
                    Fail($"Internal error: reading state corrupted ({state}).");
                    throw null;
            }
        }

        void WriteNewSections(bool atEndOfFile)
        {
            // Create empty sub-sections in new "Unreleased changes" section
            foreach (var subSection in subSections.Skip(1))
            {
                writer.WriteLine(string.Empty);
                writer.WriteLine(subSection.Header);
            }

            // Write header of new release section
            writer.WriteLine(string.Empty);
            writer.WriteLine("## " + MakeChangelogSectionTitle(data));

            var newSectionLines = CollectNewSectionLines();
            var newSectionCount = newSectionLines.Count;
            if (atEndOfFile)
            {
                // If there is no other section after the new release,
                // we don't want extra blank lines at EOF
                while (newSectionCount > 0 && string.IsNullOrEmpty(newSectionLines[newSectionCount - 1]))
                {
                    --newSectionCount;
                }
            }

            foreach (var newSectionLine in newSectionLines.Take(newSectionCount))
            {
                writer.WriteLine(newSectionLine);
            }
        }

        List<string> CollectNewSectionLines()
        {
            var result = new List<string>(subSections[0].Lines);

            // Copy only sub-sections that have actual content
            foreach (var subSection in subSections.Skip(1).Where(s => s.Lines.Any(l => !string.IsNullOrWhiteSpace(l))))
            {
                result.Add(subSection.Header);
                foreach (var contentLine in subSection.Lines)
                {
                    result.Add(contentLine);
                }
            }

            return result;
        }
    }

    SysFile.WriteAllText(data.ChangelogPath.FullPath, sb.ToString(), encoding);
}

/*
 * Summary : Updates the heading of the first section of the changelog after the "Unreleased changes" section
 *           to reflect a change in the released version.
 * Params  : context - The Cake context.
 *           data    - Build configuratiohn data.
 */
static void UpdateChangelogNewSectionTitle(this ICakeContext context, BuildData data)
{
    context.Information("Updating changelog's new release section title...");
    var encoding = new UTF8Encoding(false, true);
    var sb = new StringBuilder();
    using (var reader = new StreamReader(data.ChangelogPath.FullPath, encoding))
    using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
    {
        // Using a StringWriter instead of a StringBuilder allows for a custom line separator
        // Under Windows, a StringBuilder would only use "\r\n" as a line separator, which would be wrong in this case
        writer.NewLine = "\n";
        var sectionHeadingRegex = new Regex(@"^ {0,3}##($|[^#])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        const int ReadingFileHeader = 0;
        const int ReadingUnreleasedChangesSection = 1;
        const int ReadingRemainderOfFile = 2;
        const int ReadingDone = 3;
        var state = ReadingFileHeader;
        while (state != ReadingDone)
        {
            var line = reader.ReadLine();
            switch (state)
            {
                case ReadingFileHeader:
                    Ensure(line != null, $"{data.ChangelogPath.GetFilename()} contains no sections.");
                    writer.WriteLine(line);
                    if (sectionHeadingRegex.IsMatch(line))
                    {
                        state = ReadingUnreleasedChangesSection;
                    }

                    break;
                case ReadingUnreleasedChangesSection:
                    Ensure(line != null, $"{data.ChangelogPath.GetFilename()} contains only one section.");
                    if (sectionHeadingRegex.IsMatch(line))
                    {
                        // Replace header of second section
                        writer.WriteLine("## " + MakeChangelogSectionTitle(data));
                        state = ReadingRemainderOfFile;
                        break;
                    }

                    writer.WriteLine(line);
                    break;
                case ReadingRemainderOfFile:
                    if (line == null)
                    {
                        state = ReadingDone;
                        break;
                    }

                    writer.WriteLine(line);
                    break;
                default:
                    Fail($"Internal error: reading state corrupted ({state}).");
                    throw null;
            }
        }
    }

    SysFile.WriteAllText(data.ChangelogPath.FullPath, sb.ToString(), encoding);
}

static string MakeChangelogSectionTitle(BuildData data)
{
    return $"[{data.Version}](https://github.com/{data.RepositoryOwner}/{data.RepositoryName}/releases/tag/{data.Version}) ({DateTime.Now:yyyy-MM-dd})";
}
