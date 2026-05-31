namespace Jellyfin.Plugin.GreatCourses.Models;

/// <summary>
/// Normalized metadata for a course lecture.
/// </summary>
public sealed class GreatCourseLectureMetadata
{
    public string Title { get; init; } = string.Empty;

    public int? SeasonNumber { get; init; }

    public int? EpisodeNumber { get; init; }

    public string? SeriesTitle { get; init; }

    public string? ThumbnailPath { get; init; }
}
