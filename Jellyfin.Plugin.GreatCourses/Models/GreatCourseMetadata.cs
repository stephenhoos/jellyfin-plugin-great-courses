namespace Jellyfin.Plugin.GreatCourses.Models;

/// <summary>
/// Normalized metadata for a Great Courses course.
/// </summary>
public sealed class GreatCourseMetadata
{
    public string Title { get; init; } = string.Empty;

    public string? SortTitle { get; init; }

    public string? Overview { get; init; }

    public string? CourseNumber { get; init; }

    public string? Instructor { get; init; }

    public string? SourceUrl { get; init; }

    public string? PosterPath { get; init; }

    public string? LandscapePath { get; init; }
}
