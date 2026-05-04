using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using 全局文件搜索.Models;
using 全局文件搜索.Services;

namespace 全局文件搜索.ViewModels;

public sealed partial class SearchViewModel : ViewModelBase
{
    private readonly IFileSearchService _fileSearchService;
    private readonly SearchHistoryService _historyService;
    private CancellationTokenSource? _searchCts;
    private Stopwatch? _searchStopwatch;
    private Guid? _lastHistoryEntryId;
    private List<SearchResultItem> _allResults = [];

    public SearchViewModel(IFileSearchService fileSearchService, SearchHistoryService historyService)
    {
        _fileSearchService = fileSearchService;
        _historyService = historyService;

        AvailableDrives = new ObservableCollection<DriveItem>();
        Results = new ObservableCollection<SearchResultItem>();
        SelectedResults = new ObservableCollection<SearchResultItem>();
        AvailableExtensions = new ObservableCollection<string>();

        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CanStartSearch);
        StopSearchCommand = new RelayCommand(StopSearch, () => IsSearching);
        ClearResultsCommand = new RelayCommand(ClearResults);
        SelectAllDrivesCommand = new RelayCommand(SelectAllDrives);
        InvertDriveSelectionCommand = new RelayCommand(InvertDriveSelection);
        SystemDriveOnlyCommand = new RelayCommand(SelectSystemDriveOnly);
        OpenFileCommand = new RelayCommand(OpenSelectedFile, CanOpenSelectedItem);
        OpenFolderCommand = new RelayCommand(OpenSelectedFolder, CanOpenSelectedItem);
        DeleteFileCommand = new RelayCommand(DeleteSelectedFile, CanOpenSelectedItem);
        ExportResultsCommand = new AsyncRelayCommand(ExportResultsAsync, () => Results.Count > 0);
        ClearExtensionFilterCommand = new RelayCommand(ClearExtensionFilter);
        SaveSelectedToHistoryCommand = new RelayCommand(SaveSelectedToHistory, CanSaveSelectedToHistory);

        Results.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ResultSummary));
            OnPropertyChanged(nameof(IsResultEmpty));
            OnPropertyChanged(nameof(SelectedResultsSummary));
            OpenFileCommand.NotifyCanExecuteChanged();
            OpenFolderCommand.NotifyCanExecuteChanged();
            DeleteFileCommand.NotifyCanExecuteChanged();
            ExportResultsCommand.NotifyCanExecuteChanged();
            SaveSelectedToHistoryCommand.NotifyCanExecuteChanged();
        };

        SelectedResults.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SelectedResultsSummary));
            SaveSelectedToHistoryCommand.NotifyCanExecuteChanged();
        };

        LoadDrives();
        StatusText = "选择磁盘后输入关键词，即可开始全局搜索。";
    }

    public ObservableCollection<DriveItem> AvailableDrives { get; }

    public ObservableCollection<SearchResultItem> Results { get; }

    public ObservableCollection<SearchResultItem> SelectedResults { get; }

    public ObservableCollection<string> AvailableExtensions { get; }

    public IAsyncRelayCommand SearchCommand { get; }

    public IRelayCommand StopSearchCommand { get; }

    public IRelayCommand ClearResultsCommand { get; }

    public IRelayCommand SelectAllDrivesCommand { get; }

    public IRelayCommand InvertDriveSelectionCommand { get; }

    public IRelayCommand SystemDriveOnlyCommand { get; }

    public IRelayCommand OpenFileCommand { get; }

    public IRelayCommand OpenFolderCommand { get; }

    public IRelayCommand DeleteFileCommand { get; }

    public IAsyncRelayCommand ExportResultsCommand { get; }

    public IRelayCommand ClearExtensionFilterCommand { get; }

    public IRelayCommand SaveSelectedToHistoryCommand { get; }

    private string _query = string.Empty;

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                SearchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool _isSearching;

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (SetProperty(ref _isSearching, value))
            {
                SearchCommand.NotifyCanExecuteChanged();
                StopSearchCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    private string _statusText = string.Empty;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private string _lastScannedPath = "尚未开始扫描。";

    public string LastScannedPath
    {
        get => _lastScannedPath;
        private set => SetProperty(ref _lastScannedPath, value);
    }

    private long _scannedFiles;

    public long ScannedFiles
    {
        get => _scannedFiles;
        private set
        {
            if (SetProperty(ref _scannedFiles, value))
            {
                OnPropertyChanged(nameof(ResultSummary));
            }
        }
    }

    private TimeSpan _lastElapsed = TimeSpan.Zero;

    public string ResultSummary =>
        IsSearching
            ? $"已扫描 {ScannedFiles:N0} 个文件，实时命中 {Results.Count:N0} 条，已耗时 {GetElapsedSeconds():0.0} 秒。"
            : $"当前共有 {Results.Count:N0} 条结果，最近一次扫描耗时 {_lastElapsed.TotalSeconds:0.0} 秒。";

    public bool IsResultEmpty => Results.Count == 0;

    public string SelectedResultsSummary =>
        SelectedResults.Count == 0
            ? "未选择要保存的结果"
            : $"已选中 {SelectedResults.Count} 项，可保存到本次搜索历史";

    public string SelectedDriveSummary
    {
        get
        {
            var selectedDrives = AvailableDrives.Where(item => item.IsSelected).ToArray();
            return selectedDrives.Length == 0
                ? "未选择磁盘"
                : $"已选择 {selectedDrives.Length} 个磁盘: {string.Join("  ", selectedDrives.Select(item => item.Root))}";
        }
    }

    private SearchResultItem? _selectedResult;

    public SearchResultItem? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (SetProperty(ref _selectedResult, value))
            {
                OpenFileCommand.NotifyCanExecuteChanged();
                OpenFolderCommand.NotifyCanExecuteChanged();
                DeleteFileCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedFileInfo));
            }
        }
    }

    public string SelectedFileInfo
    {
        get
        {
            if (SelectedResult is null)
                return "未选中任何文件";

            var info = SelectedResult;
            string type = info.IsDirectory ? "文件夹" : "文件";
            return $"{type}: {info.Name}\n" +
                   $"路径: {info.FullPath}\n" +
                   $"大小: {info.SizeText}\n" +
                   $"修改时间: {info.ModifiedText}\n" +
                   $"匹配度: {info.MatchStrengthText}\n" +
                   $"{(info.IsHidden ? "隐藏文件" : "")}{(info.IsSystem ? " 系统文件" : "")}";
        }
    }

    private bool _useExactSearch = true;

    public bool UseExactSearch
    {
        get => _useExactSearch;
        set => SetProperty(ref _useExactSearch, value);
    }

    private bool _useFuzzySearch = true;

    public bool UseFuzzySearch
    {
        get => _useFuzzySearch;
        set => SetProperty(ref _useFuzzySearch, value);
    }

    private bool _includeSystemFiles = true;

    public bool IncludeSystemFiles
    {
        get => _includeSystemFiles;
        set => SetProperty(ref _includeSystemFiles, value);
    }

    private bool _includeHiddenFiles = true;

    public bool IncludeHiddenFiles
    {
        get => _includeHiddenFiles;
        set => SetProperty(ref _includeHiddenFiles, value);
    }

    private bool _includeFolders = true;

    public bool IncludeFolders
    {
        get => _includeFolders;
        set => SetProperty(ref _includeFolders, value);
    }

    private string _specificFolderPath = string.Empty;

    public string SpecificFolderPath
    {
        get => _specificFolderPath;
        set => SetProperty(ref _specificFolderPath, value);
    }

    private string _extensionFilter = string.Empty;

    public string ExtensionFilter
    {
        get => _extensionFilter;
        set
        {
            if (SetProperty(ref _extensionFilter, value))
            {
                ApplyExtensionFilter();
                OnPropertyChanged(nameof(HasActiveFilter));
            }
        }
    }

    public bool HasActiveFilter => !string.IsNullOrEmpty(ExtensionFilter);

    private void ClearExtensionFilter()
    {
        ExtensionFilter = string.Empty;
    }

    private void ApplyExtensionFilter()
    {
        Results.Clear();
        SelectedResult = null;

        var filtered = string.IsNullOrEmpty(ExtensionFilter)
            ? _allResults
            : _allResults.Where(r =>
                r.Extension.Equals(ExtensionFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in filtered)
        {
            Results.Add(item);
        }

        SelectedResult = Results.FirstOrDefault();

        StatusText = string.IsNullOrEmpty(ExtensionFilter)
            ? $"显示全部 {Results.Count:N0} 条结果。"
            : $"按后缀 \"{ExtensionFilter}\" 过滤，显示 {Results.Count:N0} 条结果。";
    }

    private void RefreshAvailableExtensions()
    {
        AvailableExtensions.Clear();

        var extensions = _allResults
            .Where(r => !r.IsDirectory && !string.IsNullOrEmpty(r.Extension))
            .Select(r => r.Extension.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e, StringComparer.OrdinalIgnoreCase);

        foreach (var ext in extensions)
        {
            AvailableExtensions.Add(ext);
        }
    }

    private void LoadDrives()
    {
        string systemDriveRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? string.Empty;

        foreach (DriveInfo drive in DriveInfo.GetDrives().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.Name
                    : $"{drive.Name} {drive.VolumeLabel}";

                string displayName = $"{label}  ·  {FormatBytes(drive.TotalSize)}  ·  {drive.DriveType}";
                bool isSelected = drive.Name.Equals(systemDriveRoot, StringComparison.OrdinalIgnoreCase);

                var driveItem = new DriveItem(drive.Name, displayName, drive.TotalSize, drive.DriveType, true, isSelected);
                driveItem.PropertyChanged += OnDriveItemPropertyChanged;
                AvailableDrives.Add(driveItem);
            }
            catch
            {
            }
        }

        if (AvailableDrives.Count > 0 && AvailableDrives.All(item => !item.IsSelected))
        {
            AvailableDrives[0].IsSelected = true;
        }

        OnPropertyChanged(nameof(SelectedDriveSummary));
    }

    private void OnDriveItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DriveItem.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedDriveSummary));
            SearchCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStartSearch()
    {
        return !IsSearching
            && !string.IsNullOrWhiteSpace(Query)
            && (AvailableDrives.Any(item => item.IsSelected) || !string.IsNullOrWhiteSpace(SpecificFolderPath));
    }

    private SearchMode GetCurrentSearchMode()
    {
        if (UseExactSearch && UseFuzzySearch)
        {
            return SearchMode.Both;
        }

        if (UseExactSearch)
        {
            return SearchMode.Exact;
        }

        if (UseFuzzySearch)
        {
            return SearchMode.Fuzzy;
        }

        return SearchMode.Both;
    }

    private async Task StartSearchAsync()
    {
        string trimmedQuery = Query.Trim();

        string[] selectedRoots = AvailableDrives
            .Where(item => item.IsSelected)
            .Select(item => item.Root)
            .ToArray();

        if (selectedRoots.Length == 0 && string.IsNullOrWhiteSpace(SpecificFolderPath))
        {
            StatusText = "请至少选择一个磁盘或指定搜索文件夹。";
            return;
        }

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        IsSearching = true;
        Results.Clear();
        _allResults.Clear();
        AvailableExtensions.Clear();
        ExtensionFilter = string.Empty;
        SelectedResult = null;
        SelectedResults.Clear();
        ScannedFiles = 0;
        _lastElapsed = TimeSpan.Zero;
        LastScannedPath = "正在初始化扫描...";
        _searchStopwatch = Stopwatch.StartNew();
        _lastHistoryEntryId = null;

        string filterInfo = string.Join(", ",
            new[]
            {
                IncludeHiddenFiles ? null : "不搜索隐藏文件",
                IncludeSystemFiles ? null : "不搜索系统文件",
                IncludeFolders ? null : "不搜索文件夹",
            }.Where(x => x is not null));

        StatusText = string.IsNullOrWhiteSpace(filterInfo)
            ? "正在扫描文件..."
            : $"正在扫描文件 ({filterInfo})。";

        var progress = new Progress<SearchProgressUpdate>(update =>
        {
            ScannedFiles = update.ScannedFiles;

            if (!string.IsNullOrWhiteSpace(update.CurrentLocation))
            {
                LastScannedPath = update.CurrentLocation!;
            }

            if (update.NewResult is not null)
            {
                Results.Add(update.NewResult);
                _allResults.Add(update.NewResult);
            }

            StatusText = $"扫描中: 已处理 {update.ScannedFiles:N0} 个文件/文件夹，命中 {Results.Count:N0} 条。";
        });

        DateTime searchStartTime = DateTime.Now;
        string[] searchScopes = GetSearchScopes(selectedRoots, SpecificFolderPath);

        try
        {
            SearchExecutionResult executionResult = await _fileSearchService.SearchAsync(
                new SearchRequest
                {
                    Query = trimmedQuery,
                    Roots = selectedRoots,
                    IncludeSystemFiles = IncludeSystemFiles,
                    IncludeHiddenFiles = IncludeHiddenFiles,
                    IncludeFolders = IncludeFolders,
                    SpecificFolder = string.IsNullOrWhiteSpace(SpecificFolderPath) ? null : SpecificFolderPath.Trim(),
                    SearchMode = GetCurrentSearchMode(),
                },
                progress,
                _searchCts.Token);

            _lastElapsed = executionResult.Elapsed;
            ScannedFiles = executionResult.ScannedFiles;
            LastScannedPath = executionResult.LastVisitedLocation ?? LastScannedPath;

            ReplaceResults(executionResult.Results);

            StatusText = executionResult.Results.Count == 0
                ? $"扫描完成，共检查 {executionResult.ScannedFiles:N0} 个条目，没有找到匹配项。"
                : $"扫描完成，共检查 {executionResult.ScannedFiles:N0} 个条目，找到 {executionResult.Results.Count:N0} 条结果。";

            _lastHistoryEntryId = RecordSearchHistory(trimmedQuery, executionResult.Results.Count, executionResult.ScannedFiles,
                executionResult.Elapsed.TotalSeconds, searchScopes, searchStartTime, isCanceled: false);
        }
        catch (OperationCanceledException)
        {
            _lastElapsed = _searchStopwatch?.Elapsed ?? DateTime.Now - searchStartTime;
            StatusText = $"搜索已停止，已耗时 {_lastElapsed.TotalSeconds:0.0} 秒，当前保留 {Results.Count:N0} 条已命中结果。";
            _lastHistoryEntryId = RecordSearchHistory(trimmedQuery, Results.Count, ScannedFiles,
                _lastElapsed.TotalSeconds, searchScopes, searchStartTime, isCanceled: true);
        }
        catch (Exception ex)
        {
            StatusText = $"搜索失败: {ex.Message}";
        }
        finally
        {
            _searchStopwatch?.Stop();
            _searchStopwatch = null;
            IsSearching = false;
            RefreshAvailableExtensions();
            OnPropertyChanged(nameof(ResultSummary));
            SaveSelectedToHistoryCommand.NotifyCanExecuteChanged();
        }
    }

    private Guid RecordSearchHistory(string query, int resultCount, long scannedFiles, double elapsedSeconds,
        string[] roots, DateTime searchTime, bool isCanceled)
    {
        Guid entryId = Guid.NewGuid();

        try
        {
            var entry = new SearchHistoryEntry
            {
                Id = entryId,
                Timestamp = searchTime,
                Query = query,
                ResultCount = resultCount,
                ScannedFiles = scannedFiles,
                ElapsedSeconds = elapsedSeconds,
                SearchedDrives = roots.Select(r => r.TrimEnd(Path.DirectorySeparatorChar)).ToList(),
                IsCanceled = isCanceled,
            };
            _historyService.AddEntry(entry);
        }
        catch
        {
        }

        return entryId;
    }

    private void ReplaceResults(System.Collections.Generic.IReadOnlyList<SearchResultItem> sortedResults)
    {
        _allResults = sortedResults.ToList();
        Results.Clear();

        foreach (SearchResultItem item in sortedResults)
        {
            Results.Add(item);
        }

        SelectedResult = Results.FirstOrDefault();
    }

    private void StopSearch()
    {
        _searchCts?.Cancel();
    }

    private void ClearResults()
    {
        StopSearch();
        Results.Clear();
        _allResults.Clear();
        AvailableExtensions.Clear();
        ExtensionFilter = string.Empty;
        SelectedResult = null;
        SelectedResults.Clear();
        ScannedFiles = 0;
        _lastElapsed = TimeSpan.Zero;
        LastScannedPath = "结果已清空。";
        StatusText = "已清空当前搜索结果。";
    }

    public void UpdateSelectedResults(IEnumerable<SearchResultItem> selectedItems)
    {
        SelectedResults.Clear();

        foreach (SearchResultItem item in selectedItems)
        {
            SelectedResults.Add(item);
        }

        if (SelectedResults.Count > 0)
        {
            SelectedResult = SelectedResults[0];
        }
    }

    private bool CanSaveSelectedToHistory()
    {
        return !IsSearching && _lastHistoryEntryId.HasValue && SelectedResults.Count > 0;
    }

    private void SaveSelectedToHistory()
    {
        if (!_lastHistoryEntryId.HasValue)
        {
            StatusText = "当前没有可保存的搜索历史。";
            return;
        }

        var snapshots = SelectedResults.Select(item => new SavedResultSnapshot
        {
            Name = item.Name,
            FullPath = item.FullPath,
            DirectoryPath = item.DirectoryPath,
            Extension = item.Extension,
            SizeBytes = item.SizeBytes,
            LastWriteTime = item.LastWriteTime,
            IsDirectory = item.IsDirectory,
        }).ToList();

        bool saved = _historyService.AddSavedResults(_lastHistoryEntryId.Value, snapshots);

        StatusText = saved
            ? $"已将 {snapshots.Count} 项结果保存到本次搜索历史。"
            : "保存失败，未找到对应的搜索历史。";
    }

    private double GetElapsedSeconds()
    {
        return (_searchStopwatch?.Elapsed ?? _lastElapsed).TotalSeconds;
    }

    private static string[] GetSearchScopes(string[] selectedRoots, string specificFolderPath)
    {
        if (!string.IsNullOrWhiteSpace(specificFolderPath))
        {
            return [specificFolderPath.Trim()];
        }

        return selectedRoots;
    }

    private void SelectAllDrives()
    {
        foreach (DriveItem drive in AvailableDrives)
        {
            drive.IsSelected = true;
        }
    }

    private void InvertDriveSelection()
    {
        foreach (DriveItem drive in AvailableDrives)
        {
            drive.IsSelected = !drive.IsSelected;
        }
    }

    private void SelectSystemDriveOnly()
    {
        string systemDriveRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? string.Empty;

        foreach (DriveItem drive in AvailableDrives)
        {
            drive.IsSelected = drive.Root.Equals(systemDriveRoot, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool CanOpenSelectedItem() => SelectedResult is not null;

    private void OpenSelectedFile()
    {
        if (SelectedResult is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SelectedResult.FullPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            StatusText = $"无法打开: {ex.Message}";
        }
    }

    private void OpenSelectedFolder()
    {
        if (SelectedResult is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SelectedResult.FullPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            StatusText = $"无法打开所在位置: {ex.Message}";
        }
    }

    private void DeleteSelectedFile()
    {
        if (SelectedResult is null)
        {
            return;
        }

        try
        {
            if (SelectedResult.IsDirectory)
            {
                Directory.Delete(SelectedResult.FullPath, false);
            }
            else
            {
                File.Delete(SelectedResult.FullPath);
            }

            string deletedName = SelectedResult.Name;
            _allResults.Remove(SelectedResult);
            Results.Remove(SelectedResult);
            SelectedResult = Results.FirstOrDefault();
            StatusText = $"已删除: {deletedName}";
        }
        catch (Exception ex)
        {
            StatusText = $"无法删除: {ex.Message}";
        }
    }

    private async Task ExportResultsAsync()
    {
        if (Results.Count == 0)
        {
            StatusText = "没有可导出的结果。";
            return;
        }

        try
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(
                Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null);

            if (topLevel?.StorageProvider is not { CanSave: true })
            {
                // Fallback: save to desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fallbackFile = Path.Combine(desktopPath,
                    $"搜索日志_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                ExportToFile(fallbackFile);
                StatusText = $"已导出到: {fallbackFile}";
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "导出搜索结果",
                DefaultExtension = ".log",
                FileTypeChoices =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("日志文件")
                    {
                        Patterns = ["*.log"],
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("文本文件")
                    {
                        Patterns = ["*.txt"],
                    },
                ],
                SuggestedFileName = $"搜索日志_{DateTime.Now:yyyyMMdd_HHmmss}.log",
            });

            if (file is not null)
            {
                ExportToFile(file.Path.LocalPath);
                StatusText = $"已导出 {Results.Count:N0} 条结果到日志文件。";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败: {ex.Message}";
        }
    }

    private void ExportToFile(string filePath)
    {
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("========================================");
        writer.WriteLine("  全局文件搜索 - 搜索结果日志");
        writer.WriteLine("========================================");
        writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"搜索关键词: {Query}");
        writer.WriteLine($"结果数量: {Results.Count}");
        writer.WriteLine($"扫描文件数: {ScannedFiles:N0}");
        writer.WriteLine($"搜索耗时: {_lastElapsed.TotalSeconds:0.0} 秒");
        writer.WriteLine($"文件类型过滤: {(string.IsNullOrEmpty(ExtensionFilter) ? "无" : ExtensionFilter)}");
        writer.WriteLine("========================================");
        writer.WriteLine();

        int index = 1;
        foreach (var item in Results)
        {
            writer.WriteLine($"[{index}] {item.Name}");
            writer.WriteLine($"    路径: {item.FullPath}");
            writer.WriteLine($"    大小: {item.SizeText}");
            writer.WriteLine($"    修改时间: {item.ModifiedText}");
            writer.WriteLine($"    类型: {item.TypeText}");
            writer.WriteLine($"    匹配度: {item.MatchStrengthText}");
            if (item.IsHidden) writer.WriteLine("    属性: 隐藏");
            if (item.IsSystem) writer.WriteLine("    属性: 系统");
            writer.WriteLine();
            index++;
        }

        writer.WriteLine("========================================");
        writer.WriteLine($"  共 {Results.Count} 条结果");
        writer.WriteLine("========================================");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int index = 0;

        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value:0.##} {units[index]}";
    }
}
