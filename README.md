# Jellyfin Great Courses Plugin

[![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=stephenhoos_jellyfin-plugin-great-courses)](https://sonarcloud.io/summary/new_code?id=stephenhoos_jellyfin-plugin-great-courses)

This plugin is an early Great Courses metadata provider for Jellyfin.

It starts with the folder at `/Volumes/Media/JellyFin/Great Courses`, recognizes video and audio courses stored under that root, reads existing `tvshow.nfo` and episode `.nfo` files, and infers missing course or lecture titles from folders and Jellyfin-style filenames.

## Current behavior

- Treats configured library contents as Great Courses.
- Supports course folders represented as TV series.
- Supports lectures represented as episodes.
- Keeps PDF guidebooks and audiobook course folders eligible for Great Courses metadata and artwork through local NFO and image sidecars.
- Reads course title, overview, course number, instructor, source URL, poster, and landscape image paths from local NFO.
- Reads lecture title, season, episode number, show title, and thumbnail path from local NFO.
- Falls back to sensible educational metadata when no NFO exists yet.

## PDF guidebooks

Jellyfin already has an official Bookshelf plugin for book libraries. Bookshelf supports PDF guidebooks and common audiobook formats, so this plugin does not try to replace it with a separate reader. Great Courses keeps the course identity, NFO metadata, and artwork sidecars next to the media; Bookshelf can be installed from the Jellyfin plugin catalog when a dedicated PDF/book library view is wanted.

## Build

```bash
dotnet build Jellyfin.Plugin.GreatCourses/Jellyfin.Plugin.GreatCourses.csproj -c Release
```

## Local install

Copy the release output into a Jellyfin plugin folder named `Great Courses_0.1.3.0`, then restart Jellyfin.

For example:

```bash
mkdir -p "/path/to/jellyfin/plugins/Great Courses_0.1.3.0"
cp Jellyfin.Plugin.GreatCourses/bin/Release/net9.0/Jellyfin.Plugin.GreatCourses.* "/path/to/jellyfin/plugins/Great Courses_0.1.3.0/"
cp Jellyfin.Plugin.GreatCourses/bin/Release/net9.0/meta.json "/path/to/jellyfin/plugins/Great Courses_0.1.3.0/"
```

## Plugin repository

Jellyfin can use `manifest.json` as a plugin repository manifest once a release zip is published:

```text
https://raw.githubusercontent.com/stephenhoos/jellyfin-plugin-great-courses/main/manifest.json
```

## SonarQube scanning

The repository includes a GitHub Actions workflow at `.github/workflows/sonarqube.yml`.

To enable SonarQube Cloud analysis:

1. Create/import this repository as a SonarQube Cloud project.
2. Add a GitHub repository secret named `SONAR_TOKEN`.
3. Optional: add repository variables if your Sonar keys differ from the defaults:
   - `SONAR_ORGANIZATION` defaults to `stephenhoos`
   - `SONAR_PROJECT_KEY` defaults to `stephenhoos_jellyfin-plugin-great-courses`
   - `SONAR_HOST_URL` defaults to `https://sonarcloud.io`

Until `SONAR_TOKEN` exists, the workflow still builds the plugin and prints setup instructions instead of failing public pull requests.

## Next metadata sources

The provider is structured so additional enrichment can be added without changing Jellyfin-facing code. Good next steps are:

- Add a local curated JSON catalog for courses that do not already have NFO.
- Add a metadata search provider for The Great Courses/Wondrium pages where available.
- Add Amazon enrichment for product descriptions and cover art through an approved API or user-provided metadata export.
