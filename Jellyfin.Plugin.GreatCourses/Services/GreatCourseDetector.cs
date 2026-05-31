using System.Globalization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.GreatCourses.Services;

/// <summary>
/// Decides whether Jellyfin items belong to the configured Great Courses library.
/// </summary>
public sealed partial class GreatCourseDetector
{
    /// <summary>
    /// Returns true when the path or name looks like a Great Courses item.
    /// </summary>
    public bool IsGreatCourse(string? name, string? path)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        if (!string.IsNullOrWhiteSpace(path) && IsInsideConfiguredLibrary(path, configuration.LibraryPath))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(name) && GreatCoursesNameRegex().IsMatch(name))
        {
            return true;
        }

        if (configuration.DetectFromNfoFiles && !string.IsNullOrWhiteSpace(path))
        {
            var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
            while (!string.IsNullOrWhiteSpace(directory))
            {
                if (File.Exists(Path.Combine(directory, "tvshow.nfo")))
                {
                    return true;
                }

                directory = Path.GetDirectoryName(directory);
            }
        }

        return false;
    }

    /// <summary>
    /// Normalizes folder and item names for matching against catalog data.
    /// </summary>
    public static string NormalizeCourseTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var name = Path.GetFileNameWithoutExtension(value.Trim());
        name = GreatCoursesPrefixRegex().Replace(name, string.Empty);
        name = EpisodeSuffixRegex().Replace(name, string.Empty);
        name = Regex.Replace(name, @"\s+", " ");
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Trim().ToLowerInvariant());
    }

    private static bool IsInsideConfiguredLibrary(string path, string libraryPath)
    {
        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(libraryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\b(the\s+)?great\s+courses?\b", RegexOptions.IgnoreCase)]
    private static partial Regex GreatCoursesNameRegex();

    [GeneratedRegex(@"^(the\s+)?great\s+courses?\s*[-:]?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex GreatCoursesPrefixRegex();

    [GeneratedRegex(@"\s+-\s+S\d{2}E\d{2}.*$", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodeSuffixRegex();
}
