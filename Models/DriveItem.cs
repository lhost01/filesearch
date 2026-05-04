using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace 全局文件搜索.Models;

public partial class DriveItem : ObservableObject
{
    public DriveItem(string root, string displayName, long? totalSizeBytes, DriveType driveType, bool isReady, bool isSelected)
    {
        Root = root;
        DisplayName = displayName;
        TotalSizeBytes = totalSizeBytes;
        DriveType = driveType;
        IsReady = isReady;
        this.isSelected = isSelected;
    }

    public string Root { get; }

    public string DisplayName { get; }

    public long? TotalSizeBytes { get; }

    public DriveType DriveType { get; }

    public bool IsReady { get; }

    [ObservableProperty]
    private bool isSelected;
}
