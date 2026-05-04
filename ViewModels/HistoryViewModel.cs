using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using 全局文件搜索.Models;
using 全局文件搜索.Services;

namespace 全局文件搜索.ViewModels;

public sealed partial class HistoryViewModel : ViewModelBase
{
    private readonly SearchHistoryService _historyService;

    public HistoryViewModel(SearchHistoryService historyService)
    {
        _historyService = historyService;
        Entries = new ObservableCollection<SearchHistoryEntry>();
        SavedResults = new ObservableCollection<SavedResultSnapshot>();
        ClearHistoryCommand = new RelayCommand(ClearHistory);
        OpenSavedItemCommand = new RelayCommand(OpenSavedItem, CanOpenSavedItem);
        OpenSavedItemFolderCommand = new RelayCommand(OpenSavedItemFolder, CanOpenSavedItem);

        Entries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsNotEmpty));
        };

        SavedResults.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSavedResults));
            OnPropertyChanged(nameof(HasNoSavedResults));
            OpenSavedItemCommand.NotifyCanExecuteChanged();
            OpenSavedItemFolderCommand.NotifyCanExecuteChanged();
        };

        RefreshHistory();
    }

    public ObservableCollection<SearchHistoryEntry> Entries { get; }

    public ObservableCollection<SavedResultSnapshot> SavedResults { get; }

    public IRelayCommand ClearHistoryCommand { get; }

    public IRelayCommand OpenSavedItemCommand { get; }

    public IRelayCommand OpenSavedItemFolderCommand { get; }

    public bool IsEmpty => Entries.Count == 0;

    public bool IsNotEmpty => Entries.Count > 0;

    public bool HasSavedResults => SavedResults.Count > 0;

    public bool HasNoSavedResults => SavedResults.Count == 0;

    [ObservableProperty]
    private SearchHistoryEntry? _selectedEntry;

    [ObservableProperty]
    private SavedResultSnapshot? _selectedSavedResult;

    [ObservableProperty]
    private string _statusText = string.Empty;

    partial void OnSelectedEntryChanged(SearchHistoryEntry? value)
    {
        SavedResults.Clear();
        SelectedSavedResult = null;

        if (value is not null)
        {
            foreach (SavedResultSnapshot item in value.SavedResults)
            {
                SavedResults.Add(item);
            }

            SelectedSavedResult = SavedResults.Count > 0 ? SavedResults[0] : null;

            StatusText = $"范围: {value.ScopeSummary}  |  "
                       + $"状态: {value.StatusText}  |  "
                       + $"结果: {value.ResultCount} 条  |  "
                       + $"扫描: {value.ScannedFiles:N0} 文件  |  "
                       + $"耗时: {value.ElapsedSeconds:0.0} 秒  |  "
                       + $"时间: {value.Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
        else
        {
            StatusText = Entries.Count > 0 ? $"共 {Entries.Count} 条搜索记录" : "暂无搜索记录";
        }
    }

    public void RefreshHistory()
    {
        Entries.Clear();
        foreach (var entry in _historyService.LoadAll())
        {
            Entries.Add(entry);
        }

        StatusText = Entries.Count > 0
            ? $"共 {Entries.Count} 条搜索记录"
            : "暂无搜索记录";

        SelectedEntry = Entries.Count > 0 ? Entries[0] : null;
    }

    private void ClearHistory()
    {
        Entries.Clear();
        SavedResults.Clear();
        SelectedEntry = null;
        SelectedSavedResult = null;
        StatusText = "搜索记录已清空";
        _historyService.ClearAll();
    }

    partial void OnSelectedSavedResultChanged(SavedResultSnapshot? value)
    {
        OpenSavedItemCommand.NotifyCanExecuteChanged();
        OpenSavedItemFolderCommand.NotifyCanExecuteChanged();
    }

    private bool CanOpenSavedItem()
    {
        return SelectedSavedResult is not null;
    }

    private void OpenSavedItem()
    {
        if (SelectedSavedResult is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SelectedSavedResult.FullPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            StatusText = $"无法打开保存项: {ex.Message}";
        }
    }

    private void OpenSavedItemFolder()
    {
        if (SelectedSavedResult is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SelectedSavedResult.FullPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            StatusText = $"无法打开所在位置: {ex.Message}";
        }
    }
}
