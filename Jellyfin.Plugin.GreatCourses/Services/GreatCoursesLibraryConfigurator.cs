using System.Xml.Linq;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GreatCourses.Services;

/// <summary>
/// Keeps the configured Great Courses library wired to this plugin.
/// </summary>
public sealed class GreatCoursesLibraryConfigurator : IHostedService
{
    private const string GreatCoursesProviderName = "Great Courses";
    private const string StringElementName = "string";
    private const string TypeOptionsElementName = "TypeOptions";

    private static readonly string[] CollectionMarkers =
    {
        "books.collection",
        "homevideos.collection",
        "movies.collection",
        "music.collection",
        "tvshows.collection"
    };

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<GreatCoursesLibraryConfigurator> _logger;

    public GreatCoursesLibraryConfigurator(
        IApplicationPaths applicationPaths,
        ILogger<GreatCoursesLibraryConfigurator> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration;
        if (configuration is null || !configuration.ManageJellyfinLibrary)
        {
            return Task.CompletedTask;
        }

        try
        {
            ConfigureLibrary(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to configure the Great Courses library");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ConfigureLibrary(PluginConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.LibraryPath) || !Directory.Exists(configuration.LibraryPath))
        {
            _logger.LogWarning("Great Courses library path does not exist: {LibraryPath}", configuration.LibraryPath);
            return;
        }

        var libraryName = string.IsNullOrWhiteSpace(configuration.LibraryName)
            ? GreatCoursesProviderName
            : configuration.LibraryName.Trim();
        var libraryDirectory = Path.Combine(_applicationPaths.ProgramDataPath, "root", "default", libraryName);
        Directory.CreateDirectory(libraryDirectory);

        File.WriteAllText(Path.Combine(libraryDirectory, libraryName + ".mblink"), configuration.LibraryPath);

        foreach (var marker in CollectionMarkers)
        {
            var markerPath = Path.Combine(libraryDirectory, marker);
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }

        File.WriteAllText(Path.Combine(libraryDirectory, "tvshows.collection"), string.Empty);

        var optionsPath = Path.Combine(libraryDirectory, "options.xml");
        var document = File.Exists(optionsPath) ? XDocument.Load(optionsPath) : CreateDefaultOptions();
        ApplyLibraryOptions(document, configuration);
        document.Save(optionsPath);

        InstallDefaultLibraryImage(libraryDirectory, _logger);
    }

    private static XDocument CreateDefaultOptions()
        => new(
            new XElement(
                "LibraryOptions",
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XElement("Enabled", true),
                new XElement("EnableRealtimeMonitor", true),
                new XElement("PathInfos"),
                new XElement("PreferredMetadataLanguage", "en"),
                new XElement("MetadataCountryCode", "US"),
                new XElement(TypeOptionsElementName)));

    private static void ApplyLibraryOptions(XDocument document, PluginConfiguration configuration)
    {
        var root = document.Root ?? throw new InvalidOperationException("Library options XML is empty.");
        SetElement(root, "PreferredMetadataLanguage", "en");
        SetElement(root, "MetadataCountryCode", "US");

        var pathInfos = GetOrAdd(root, "PathInfos");
        pathInfos.RemoveNodes();
        pathInfos.Add(new XElement("MediaPathInfo", new XElement("Path", configuration.LibraryPath)));

        var typeOptions = GetOrAdd(root, TypeOptionsElementName);
        ConfigureType(typeOptions, "Series", useGreatCourses: true, configuration.UseOnlyGreatCoursesMetadata);
        ConfigureType(typeOptions, "Episode", useGreatCourses: true, configuration.UseOnlyGreatCoursesMetadata);
        ConfigureType(typeOptions, "Season", useGreatCourses: false, configuration.UseOnlyGreatCoursesMetadata);
        ConfigureType(typeOptions, "Movie", useGreatCourses: false, configuration.UseOnlyGreatCoursesMetadata);
    }

    private static void ConfigureType(XElement typeOptionsRoot, string type, bool useGreatCourses, bool metadataOnly)
    {
        var typeElement = typeOptionsRoot.Elements(TypeOptionsElementName)
            .FirstOrDefault(element => string.Equals(element.Element("Type")?.Value, type, StringComparison.OrdinalIgnoreCase));

        if (typeElement is null)
        {
            typeElement = new XElement(TypeOptionsElementName, new XElement("Type", type));
            typeOptionsRoot.Add(typeElement);
        }

        var metadataFetchers = GetOrAdd(typeElement, "MetadataFetchers");
        var metadataFetcherOrder = GetOrAdd(typeElement, "MetadataFetcherOrder");
        metadataFetchers.RemoveNodes();
        metadataFetcherOrder.RemoveNodes();

        if (useGreatCourses)
        {
            metadataFetchers.Add(new XElement(StringElementName, GreatCoursesProviderName));
            metadataFetcherOrder.Add(new XElement(StringElementName, GreatCoursesProviderName));
        }

        if (metadataOnly)
        {
            var imageFetchers = GetOrAdd(typeElement, "ImageFetchers");
            var imageFetcherOrder = GetOrAdd(typeElement, "ImageFetcherOrder");
            imageFetchers.RemoveNodes();
            imageFetcherOrder.RemoveNodes();

            if (useGreatCourses)
            {
                imageFetchers.Add(new XElement(StringElementName, GreatCoursesProviderName));
                imageFetcherOrder.Add(new XElement(StringElementName, GreatCoursesProviderName));
            }
        }
    }

    private static void InstallDefaultLibraryImage(string libraryDirectory, ILogger logger)
    {
        var existingArtwork = new[]
        {
            "folder.jpg",
            "folder.jpeg",
            "folder.png",
            "poster.jpg",
            "poster.jpeg",
            "poster.png"
        };

        if (existingArtwork.Any(fileName => File.Exists(Path.Combine(libraryDirectory, fileName))))
        {
            return;
        }

        var assembly = typeof(GreatCoursesLibraryConfigurator).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("Assets.great-courses-library.png", StringComparison.Ordinal));
        if (resourceName is null)
        {
            logger.LogWarning("Great Courses default library image resource was not found");
            return;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            logger.LogWarning("Great Courses default library image resource could not be opened: {ResourceName}", resourceName);
            return;
        }

        var folderImage = Path.Combine(libraryDirectory, "folder.png");
        using (var fileStream = File.Create(folderImage))
        {
            stream.CopyTo(fileStream);
        }

        File.Copy(folderImage, Path.Combine(libraryDirectory, "poster.png"), overwrite: false);
        logger.LogInformation("Installed default Great Courses library image at {ImagePath}", folderImage);
    }

    private static XElement GetOrAdd(XElement root, string name)
    {
        var element = root.Element(name);
        if (element is not null)
        {
            return element;
        }

        element = new XElement(name);
        root.Add(element);
        return element;
    }

    private static void SetElement(XElement root, string name, string value)
    {
        var element = GetOrAdd(root, name);
        element.Value = value;
    }
}
