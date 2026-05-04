# Global File Search

[中文](README.md)

A desktop global file search tool built with `Avalonia UI` + `.NET 8`, designed for quickly retrieving files and folders from local disks or specified directories. It also provides search history, result saving, background personalization, and dashboard statistics.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![Avalonia](https://img.shields.io/badge/Avalonia-12.0-8B44AC.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

## Preview

>
> ```
> ├── Dashboard: cumulative search count, time spent, recent records, and particle effects
> ├── Search: enter keywords, select drives, real-time scan progress and hit results
> ├── History: browse past search sessions and manually saved result snapshots
> └── Settings: configure background image/video, transparency, and playback options
> ```

## Introduction

This project targets the Windows desktop scenario, providing a modern, multi-page, visual local file retrieval application.  
In addition to global keyword search, it also supports:

- Multi-drive and specified-directory search
- Toggles for hidden files, system files, and folders
- Exact and fuzzy matching
- Search result export
- Search history and statistics
- Saving one or more search results to history for later review
- Configurable background image / background video
- Dashboard statistics with lightweight animation effects

## Features

### 1. File Search

- Search by file name or keyword across the system
- Select one or more drives as the search scope
- Specify a folder as the search root
- Include or exclude:
  - System files
  - Hidden files
  - Folders
- Search modes:
  - Exact search
  - Fuzzy search
  - Exact + fuzzy mixed search
- Real-time display during search:
  - Scanned item count
  - Live hit count
  - Current scan location
  - Elapsed time
- Support stopping the search midway and recording the time spent

### 2. Search Result Management

- Highlight selected items in the result list
- Open files directly
- Open the containing directory
- Delete selected items
- Filter results by file extension
- Export results as a log file
- Multi-select results and save them to the corresponding history entry

### 3. Search History

- Automatically record each search session
- History entries include:
  - Search time
  - Search scope
  - Search term
  - Scanned count
  - Hit count
  - Time spent
  - Search status (completed / stopped)
- View manually saved files or folders within a history entry
- Open saved items or their locations directly from the history page
- Clear all history records

### 4. Dashboard

- Display cumulative search count
- Display cumulative search time
- Display recent search records
- Display current time and date
- Lightweight particle animations and pseudo-3D tech-style visual effects

### 5. Personalization Settings

- Background image support
- Background video support
- Background transparency adjustment
- Control background video playback and mute status
- Local persistence of user settings

## Tech Stack

### Core Framework

- `.NET 8`
- `Avalonia 12`
- `Avalonia.Desktop`
- `Avalonia.Themes.Fluent`

### MVVM & State Management

- `CommunityToolkit.Mvvm`

### Media Capabilities

- `LibVLCSharp.Avalonia`
- `VideoLAN.LibVLC.Windows`

### Data Storage

- `System.Text.Json`
- Local JSON files for persisting search history and user preferences

## Project Structure

```text
全局文件搜索/
├─ .github/
│  └─ workflows/
│     └─ dotnet.yml        GitHub Actions CI configuration
├─ Assets/                 Icons and resource files
├─ Models/                 Data models
├─ Services/               Core service layer
├─ ViewModels/             View model layer
├─ Views/                  Avalonia views
├─ App.axaml               Application-level styles and resources
├─ App.axaml.cs            Application entry initialization
├─ Program.cs              Program startup entry
├─ ViewLocator.cs          ViewModel-to-View binding locator
├─ 全局文件搜索.csproj      Project configuration
├─ README.md               Chinese documentation
├─ README_EN.md            English documentation
└─ LICENSE                 MIT License
```

## Module Overview

### `Views/`

The UI layer responsible for page layout and interactive display. Main pages include:

- `MainWindow`: Main window and navigation container
- `DashboardView`: Dashboard
- `SearchView`: File search page
- `HistoryView`: Search history page
- `SettingsView`: Settings page

### `ViewModels/`

The core business interaction layer connecting the UI and services:

- `MainWindowViewModel`
  - Manages page navigation
  - Holds child page ViewModels
- `SearchViewModel`
  - Search flow control
  - Search status updates
  - Result filtering / exporting / saving to history
- `HistoryViewModel`
  - Search history reading
  - Saved item display and opening
- `DashboardViewModel`
  - Statistics aggregation
  - Recent search display
  - Particle animations and dynamic effects
- `SettingsViewModel`
  - Background resource loading
  - Preference persistence

### `Services/`

The service layer responsible for actual feature implementation:

- `FileSearchService`
  - Core search engine
  - Disk and directory traversal
  - Search result generation
  - Scan progress and time statistics
- `SearchHistoryService`
  - Search history read/write
  - Recent record retrieval
  - Save search result snapshots
- `AppPreferencesService`
  - User settings read/write
- `BackgroundMediaResolver`
  - Background resource type resolution (image / video / none)

### `Models/`

Data carrier objects, mainly including:

- `SearchResultItem`: Single search result
- `SearchHistoryEntry`: Single search history entry
- `SavedResultSnapshot`: Saved result snapshot in history
- `AppPreferences`: Local settings
- `DriveItem`: Drive item
- `BackgroundMediaDescriptor`: Background media descriptor

## Architecture

The project adopts the classic `MVVM` architecture:

1. `View`
   - Responsible for UI layout and binding
   - Does not contain complex business logic
2. `ViewModel`
   - Responsible for state management, commands, and interaction flow
   - Transforms service layer results into bindable data
3. `Service`
   - Responsible for core capabilities such as search, history, settings, and background resolution
4. `Model`
   - Describes business data structures

Advantages of this design:

- Clear structure
- Separation of page responsibilities
- Easy to extend
- Easy to maintain and refactor

## Search Flow Overview

1. User enters a keyword on the search page
2. Selects a drive or specified directory
3. `SearchViewModel` assembles the `SearchRequest`
4. `FileSearchService` performs traversal and matching
5. Updates scan status in real time through progress callbacks
6. Records history after search completion or stop
7. User can save selected search results to that history entry
8. History page allows reviewing these saved items

## Local Data Storage

The application saves search history and preference configurations in the local user directory for continued use on next launch:

- Search history: `LocalApplicationData/全局文件搜索/search_history.json`
- App settings: Preference files under `LocalApplicationData/全局文件搜索/`

## Runtime Environment

### Development Environment

- Windows
- .NET 8 SDK

### Install Dependencies

```bash
dotnet restore
```

### Run the Project

```bash
dotnet run
```

### Build the Project

```bash
dotnet build
```

### Publish the Project

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## Use Cases

- Quickly locate local files
- Multi-drive file retrieval
- Temporarily organize and filter search results
- Keep frequently used search result snapshots for later access
- Practice example for `Avalonia + MVVM` desktop applications

## Future Enhancements

- Add file content search
- Add search result sorting options
- Add favorites / tag system
- Add batch operations
- Add richer preview capabilities
- Add cross-platform adaptation optimization

## License

This project is open-sourced under the [MIT](LICENSE) License.
