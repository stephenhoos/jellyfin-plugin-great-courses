using Jellyfin.Plugin.GreatCourses.Services;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using System.Net.Http;

namespace Jellyfin.Plugin.GreatCourses.Providers;

/// <summary>
/// Provides lecture-level metadata for Great Courses episodes.
/// </summary>
public sealed class GreatCourseEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
{
    private readonly GreatCourseDetector _detector;
    private readonly GreatCourseMetadataReader _metadataReader;

    public GreatCourseEpisodeProvider(GreatCourseDetector detector, GreatCourseMetadataReader metadataReader)
    {
        _detector = detector;
        _metadataReader = metadataReader;
    }

    public string Name => "Great Courses";

    public Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        if (!_detector.IsGreatCourse(info.Name, info.Path))
        {
            return Task.FromResult(new MetadataResult<Episode>());
        }

        var metadata = _metadataReader.ReadLecture(info.Name, info.Path);
        if (metadata is null)
        {
            return Task.FromResult(new MetadataResult<Episode>());
        }

        return Task.FromResult(new MetadataResult<Episode>
        {
            HasMetadata = true,
            Item = new Episode
            {
                Name = metadata.Title,
                IndexNumber = metadata.EpisodeNumber,
                ParentIndexNumber = metadata.SeasonNumber
            }
        });
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        if (!_detector.IsGreatCourse(searchInfo.Name, searchInfo.Path))
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        var metadata = _metadataReader.ReadLecture(searchInfo.Name, searchInfo.Path);
        if (metadata is null)
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        return Task.FromResult<IEnumerable<RemoteSearchResult>>(new[]
        {
            new RemoteSearchResult
            {
                Name = metadata.Title,
                SearchProviderName = Name,
                ImageUrl = metadata.ThumbnailPath
            }
        });
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Great Courses currently relies on Jellyfin local image discovery.");
    }
}
