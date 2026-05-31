using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GreatCourses;

/// <summary>
/// Plugin settings persisted by Jellyfin.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the folder where Great Courses are stored.
    /// </summary>
    public string LibraryPath { get; set; } = "/Volumes/Media/JellyFin/Great Courses";

    /// <summary>
    /// Gets or sets a value indicating whether folders without "Great Courses" in the name can be detected from nearby metadata.
    /// </summary>
    public bool DetectFromNfoFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unknown course folders should still be tagged as educational Great Courses.
    /// </summary>
    public bool InferMissingMetadata { get; set; } = true;
}
