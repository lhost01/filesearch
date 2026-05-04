using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using 全局文件搜索.ViewModels;

namespace 全局文件搜索.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void PickBackground_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { CanOpen: true } storageProvider)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择背景文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                FilePickerFileTypes.ImageAll,
                new FilePickerFileType("视频文件")
                {
                    Patterns = ["*.mp4", "*.webm", "*.avi", "*.mov", "*.mkv", "*.wmv", "*.m4v"],
                    MimeTypes = ["video/*"],
                },
                new FilePickerFileType("Wallpaper Engine 项目")
                {
                    Patterns = ["project.json"],
                    MimeTypes = ["application/json", "text/json"],
                },
                new FilePickerFileType("Wallpaper 包")
                {
                    Patterns = ["*.mpkg"],
                },
            ],
        });

        string? selectedPath = files.FirstOrDefault()?.TryGetLocalPath();
        if (selectedPath is not null && DataContext is SettingsViewModel viewModel)
        {
            viewModel.ApplyBackgroundImage(selectedPath);
        }
    }

    private async void PickWallpaperFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { CanPickFolder: true } storageProvider)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择 Wallpaper Engine 壁纸目录",
            AllowMultiple = false,
        });

        string? selectedPath = folders.FirstOrDefault()?.TryGetLocalPath();
        if (selectedPath is not null && DataContext is SettingsViewModel viewModel)
        {
            viewModel.ApplyBackgroundImage(selectedPath);
        }
    }
}
