using System;
using System.Globalization;
using System.IO;

namespace 全局文件搜索.Models;

public sealed class SavedResultSnapshot
{
    public string Name { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string DirectoryPath { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastWriteTime { get; set; }

    public bool IsDirectory { get; set; }

    public string TypeText => IsDirectory ? "文件夹" : "文件";

    public string SizeText => IsDirectory ? "-" : FormatBytes(SizeBytes);

    public string ModifiedText => LastWriteTime == default
        ? "未知"
        : LastWriteTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public string BadgeText
    {
        get
        {
            if (IsDirectory)
            {
                return "文件夹";
            }

            return string.IsNullOrWhiteSpace(Extension)
                ? "文件"
                : Extension.ToUpperInvariant();
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
