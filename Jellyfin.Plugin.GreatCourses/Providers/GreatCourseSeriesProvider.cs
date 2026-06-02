using Jellyfin.Plugin.GreatCourses.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Net.Http;

namespace Jellyfin.Plugin.GreatCourses.Providers;

/// <summary>
/// Provides course-level metadata for Great Courses folders represented as TV series.
/// </summary>
public sealed class GreatCourseSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IRemoteImageProvider
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public string Name => "Great Courses";

    public bool Supports(BaseItem item)
        => item is Series && GreatCourseDetector.IsGreatCourse(item.Name, item.Path);

    public Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        if (!GreatCourseDetector.IsGreatCourse(info.Name, info.Path))
        {
            return Task.FromResult(new MetadataResult<Series>());
        }

        var metadata = GreatCourseMetadataReader.ReadCourse(info.Name, info.Path);
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
        if (!GreatCourseDetector.IsGreatCourse(searchInfo.Name, searchInfo.Path))
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        var metadata = GreatCourseMetadataReader.ReadCourse(searchInfo.Name, searchInfo.Path);
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

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        => item is Series ? new[] { ImageType.Primary, ImageType.Thumb, ImageType.Backdrop } : Array.Empty<ImageType>();

    public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not Series || !GreatCourseDetector.IsGreatCourse(item.Name, item.Path))
        {
            return Task.FromResult(Enumerable.Empty<RemoteImageInfo>());
        }

        var metadata = GreatCourseMetadataReader.ReadCourse(item.Name, item.Path);
        if (metadata is null)
        {
            return Task.FromResult(Enumerable.Empty<RemoteImageInfo>());
        }

        var images = new List<RemoteImageInfo>();
        AddImage(images, metadata.PosterPath, ImageType.Primary);
        AddImage(images, metadata.LandscapePath, ImageType.Thumb);
        AddImage(images, metadata.LandscapePath, ImageType.Backdrop);
        return Task.FromResult<IEnumerable<RemoteImageInfo>>(images);
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        if (!IsAllowedLocalImage(url))
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StreamContent(File.OpenRead(url))
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(url));
        return Task.FromResult(response);
    }

    private void AddImage(List<RemoteImageInfo> images, string? url, ImageType type)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        images.Add(new RemoteImageInfo
        {
            ProviderName = Name,
            Url = url,
            ThumbnailUrl = url,
            Type = type,
            Language = "en"
        });
    }

    private static string GetMimeType(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return "image/webp";
        }

        return "image/jpeg";
    }

    private static bool IsAllowedLocalImage(string path)
    {
        if (!ImageExtensions.Contains(Path.GetExtension(path)) || !File.Exists(path))
        {
            return false;
        }

        var libraryPath = Plugin.Instance?.Configuration.LibraryPath;
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
}
