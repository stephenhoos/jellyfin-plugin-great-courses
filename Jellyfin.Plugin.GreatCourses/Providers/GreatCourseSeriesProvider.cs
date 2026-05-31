using Jellyfin.Plugin.GreatCourses.Services;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Net.Http;

namespace Jellyfin.Plugin.GreatCourses.Providers;

/// <summary>
/// Provides course-level metadata for Great Courses folders represented as TV series.
/// </summary>
public sealed class GreatCourseSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
{
    private readonly GreatCourseDetector _detector;
    private readonly GreatCourseMetadataReader _metadataReader;

    public GreatCourseSeriesProvider(GreatCourseDetector detector, GreatCourseMetadataReader metadataReader)
    {
        _detector = detector;
        _metadataReader = metadataReader;
    }

    public string Name => "Great Courses";

    public Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        if (!_detector.IsGreatCourse(info.Name, info.Path))
        {
            return Task.FromResult(new MetadataResult<Series>());
        }

        var metadata = _metadataReader.ReadCourse(info.Name, info.Path);
        if (metadata is null)
        {
            return Task.FromResult(new MetadataResult<Series>());
        }

        var item = new Series
        {
            Name = metadata.Title,
            OriginalTitle = metadata.Title,
            SortName = metadata.SortTitle,
            Overview = metadata.Overview,
            Studios = new[] { "The Great Courses" },
            Genres = new[] { "Education" }
        };

        if (!string.IsNullOrWhiteSpace(metadata.CourseNumber))
        {
            item.ProviderIds["thegreatcourses"] = metadata.CourseNumber;
        }

        return Task.FromResult(new MetadataResult<Series>
        {
            HasMetadata = true,
            Item = item
        });
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
    {
        if (!_detector.IsGreatCourse(searchInfo.Name, searchInfo.Path))
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        var metadata = _metadataReader.ReadCourse(searchInfo.Name, searchInfo.Path);
        if (metadata is null)
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        var result = new RemoteSearchResult
        {
            Name = metadata.Title,
            SearchProviderName = Name,
            Overview = metadata.Overview,
            ImageUrl = metadata.PosterPath
        };

        if (!string.IsNullOrWhiteSpace(metadata.CourseNumber))
        {
            result.ProviderIds["thegreatcourses"] = metadata.CourseNumber;
        }

        return Task.FromResult<IEnumerable<RemoteSearchResult>>(new[] { result });
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Great Courses currently relies on Jellyfin local image discovery.");
    }
}
