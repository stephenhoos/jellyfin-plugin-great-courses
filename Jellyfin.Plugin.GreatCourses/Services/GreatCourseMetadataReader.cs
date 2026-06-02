using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Plugin.GreatCourses.Models;

namespace Jellyfin.Plugin.GreatCourses.Services;

/// <summary>
/// Reads local NFO metadata and infers missing course metadata from Jellyfin-friendly filenames.
/// </summary>
public static partial class GreatCourseMetadataReader
{
    /// <summary>
    /// Reads a course folder's metadata.
    /// </summary>
    public static GreatCourseMetadata? ReadCourse(string? name, string? path)
    {
        var courseDirectory = FindCourseDirectory(path);
        var nfoPath = courseDirectory is null ? null : Path.Combine(courseDirectory, "tvshow.nfo");
        if (nfoPath is not null && File.Exists(nfoPath))
        {
            return ReadCourseNfo(nfoPath);
        }

        if (Plugin.Instance?.Configuration.InferMissingMetadata != true)
        {
            return null;
        }

        var inferredTitle = GreatCourseDetector.NormalizeCourseTitle(courseDirectory ?? name);
        if (string.IsNullOrWhiteSpace(inferredTitle))
        {
            return null;
        }

        return new GreatCourseMetadata
        {
            Title = inferredTitle,
            SortTitle = inferredTitle,
            Overview = "A lecture course from The Great Courses.",
            SourceUrl = BuildSearchUrl(inferredTitle)
        };
    }

    /// <summary>
    /// Reads metadata for a lecture file.
    /// </summary>
    public static GreatCourseLectureMetadata? ReadLecture(string? name, string? path)
    {
        var nfoPath = path is null ? null : Path.ChangeExtension(path, ".nfo");
        if (nfoPath is not null && File.Exists(nfoPath))
        {
            return ReadLectureNfo(nfoPath);
        }

        var source = Path.GetFileNameWithoutExtension(path) ?? name;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var match = EpisodeNameRegex().Match(source);
        if (!match.Success)
        {
            return new GreatCourseLectureMetadata { Title = source };
        }

        return new GreatCourseLectureMetadata
        {
            Title = match.Groups["title"].Value.Trim(),
            SeasonNumber = int.Parse(match.Groups["season"].Value, CultureInfo.InvariantCulture),
            EpisodeNumber = int.Parse(match.Groups["episode"].Value, CultureInfo.InvariantCulture),
            SeriesTitle = GreatCourseDetector.NormalizeCourseTitle(match.Groups["series"].Value)
        };
    }

    private static GreatCourseMetadata? ReadCourseNfo(string nfoPath)
    {
        var document = LoadXml(nfoPath);
        var root = document.Root;
        if (root is null)
        {
            return null;
        }

        var courseDirectory = Path.GetDirectoryName(nfoPath);
        return new GreatCourseMetadata
        {
            Title = Value(root, "title") ?? GreatCourseDetector.NormalizeCourseTitle(courseDirectory),
            SortTitle = Value(root, "sorttitle"),
            Overview = Value(root, "plot"),
            CourseNumber = Value(root, "id") ?? root.Elements("uniqueid").FirstOrDefault()?.Value,
            Instructor = root.Elements("actor").Elements("name").FirstOrDefault()?.Value,
            SourceUrl = Value(root, "source"),
            PosterPath = ResolveImagePath(courseDirectory, root.Elements("thumb").FirstOrDefault(e => (string?)e.Attribute("aspect") == "poster")?.Value),
            LandscapePath = ResolveImagePath(courseDirectory, root.Elements("thumb").FirstOrDefault(e => (string?)e.Attribute("aspect") == "landscape")?.Value)
        };
    }

    private static GreatCourseLectureMetadata? ReadLectureNfo(string nfoPath)
    {
        var document = LoadXml(nfoPath);
        var root = document.Root;
        if (root is null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(nfoPath);
        return new GreatCourseLectureMetadata
        {
            Title = Value(root, "title") ?? Path.GetFileNameWithoutExtension(nfoPath),
            SeasonNumber = IntValue(root, "season"),
            EpisodeNumber = IntValue(root, "episode"),
            SeriesTitle = Value(root, "showtitle"),
            ThumbnailPath = ResolveImagePath(directory, Value(root, "thumb"))
        };
    }

    private static string? FindCourseDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "tvshow.nfo")) || Directory.Exists(Path.Combine(directory, "Season 01")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private static string? Value(XElement root, string name)
        => root.Element(name)?.Value.Trim();

    private static int? IntValue(XElement root, string name)
        => int.TryParse(Value(root, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string? ResolveImagePath(string? directory, string? value)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Path.IsPathRooted(value) ? value : Path.Combine(directory, value);
    }

    private static XDocument LoadXml(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(path, settings);
        return XDocument.Load(reader);
    }

    private static string BuildSearchUrl(string title)
        => "https://www.amazon.com/s?k=" + Uri.EscapeDataString("The Great Courses " + title);

    [GeneratedRegex(@"^(?<series>.+?)\s+-\s+S(?<season>\d{2})E(?<episode>\d{2})\s+-\s+(?<title>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodeNameRegex();
}
