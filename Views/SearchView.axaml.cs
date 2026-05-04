using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using 全局文件搜索.ViewModels;

namespace 全局文件搜索.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SearchViewModel viewModel)
        {
            if (viewModel.SearchCommand.CanExecute(null))
            {
                viewModel.SearchCommand.Execute(null);
            }
        }
    }

    private async void OnSelectFolderClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not SearchViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择搜索文件夹",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
        {
            viewModel.SpecificFolderPath = folders[0].Path.LocalPath;
        }
    }

    private void OnResultsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || DataContext is not SearchViewModel viewModel)
        {
            return;
        }

        viewModel.UpdateSelectedResults(listBox.SelectedItems.OfType<Models.SearchResultItem>());
    }
}
