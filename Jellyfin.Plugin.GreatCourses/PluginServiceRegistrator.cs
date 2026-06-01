using Jellyfin.Plugin.GreatCourses.Providers;
using Jellyfin.Plugin.GreatCourses.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.GreatCourses;

/// <summary>
/// Registers plugin services with Jellyfin.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<GreatCourseSeriesProvider>();
        serviceCollection.AddSingleton<GreatCourseEpisodeProvider>();
        serviceCollection.AddSingleton<IRemoteMetadataProvider<MediaBrowser.Controller.Entities.TV.Series, SeriesInfo>>(provider => provider.GetRequiredService<GreatCourseSeriesProvider>());
        serviceCollection.AddSingleton<IRemoteMetadataProvider<MediaBrowser.Controller.Entities.TV.Episode, EpisodeInfo>>(provider => provider.GetRequiredService<GreatCourseEpisodeProvider>());
        serviceCollection.AddSingleton<IRemoteImageProvider>(provider => provider.GetRequiredService<GreatCourseSeriesProvider>());
        serviceCollection.AddSingleton<IMetadataProvider>(provider => provider.GetRequiredService<GreatCourseSeriesProvider>());
        serviceCollection.AddSingleton<IMetadataProvider>(provider => provider.GetRequiredService<GreatCourseEpisodeProvider>());
        serviceCollection.AddHostedService<GreatCoursesLibraryConfigurator>();
    }
}
