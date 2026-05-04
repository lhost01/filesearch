using CommunityToolkit.Mvvm.ComponentModel;
using 全局文件搜索.Services;

namespace 全局文件搜索.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        var searchService = new FileSearchService();
        var historyService = new SearchHistoryService();

        DashboardPage = new DashboardViewModel(historyService);
        SearchPage = new SearchViewModel(searchService, historyService);
        HistoryPage = new HistoryViewModel(historyService);
        SettingsPage = new SettingsViewModel();
        currentPage = DashboardPage;
    }

    public DashboardViewModel DashboardPage { get; }

    public SearchViewModel SearchPage { get; }

    public HistoryViewModel HistoryPage { get; }

    public SettingsViewModel SettingsPage { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    public bool IsDashboardPageSelected
    {
        get => ReferenceEquals(CurrentPage, DashboardPage);
        set
        {
            if (value && !ReferenceEquals(CurrentPage, DashboardPage))
            {
                CurrentPage = DashboardPage;
            }
        }
    }

    public bool IsSearchPageSelected
    {
        get => ReferenceEquals(CurrentPage, SearchPage);
        set
        {
            if (value && !ReferenceEquals(CurrentPage, SearchPage))
            {
                CurrentPage = SearchPage;
            }
        }
    }

    public bool IsHistoryPageSelected
    {
        get => ReferenceEquals(CurrentPage, HistoryPage);
        set
        {
            if (value && !ReferenceEquals(CurrentPage, HistoryPage))
            {
                CurrentPage = HistoryPage;
                HistoryPage.RefreshHistory();
            }
        }
    }

    public bool IsSettingsPageSelected
    {
        get => ReferenceEquals(CurrentPage, SettingsPage);
        set
        {
            if (value && !ReferenceEquals(CurrentPage, SettingsPage))
            {
                CurrentPage = SettingsPage;
            }
        }
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsDashboardPageSelected));
        OnPropertyChanged(nameof(IsSearchPageSelected));
        OnPropertyChanged(nameof(IsHistoryPageSelected));
        OnPropertyChanged(nameof(IsSettingsPageSelected));

        // Refresh dashboard stats every time user navigates to it
        if (ReferenceEquals(value, DashboardPage))
        {
            DashboardPage.RefreshStats();
        }
    }
}
