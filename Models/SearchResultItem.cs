using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace 全局文件搜索.Models;

public sealed class SearchResultItem
{
    public SearchResultItem(
        string name,
        string fullPath,
        string directoryPath,
        string extension,
        long sizeBytes,
        DateTime lastWriteTime,
        double score,
        bool isHidden,
        bool isSystem,
        bool isDirectory = false)
    {
        Name = name;
        FullPath = fullPath;
        DirectoryPath = directoryPath;
        Extension = extension;
        SizeBytes = sizeBytes;
        LastWriteTime = lastWriteTime;
        Score = score;
        IsHidden = isHidden;
        IsSystem = isSystem;
        IsDirectory = isDirectory;
    }

    public string Name { get; }

    public string FullPath { get; }

    public string DirectoryPath { get; }

    public string Extension { get; }

    public long SizeBytes { get; }

    public DateTime LastWriteTime { get; }

    public double Score { get; }

    public bool IsHidden { get; }

    public bool IsSystem { get; }

    public bool IsDirectory { get; }

    public string SizeText => FormatBytes(SizeBytes);

    public string ModifiedText => LastWriteTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public string TypeText => IsDirectory ? "文件夹" : "文件";

    public string MatchStrengthText =>
        Score >= 170 ? "高精度命中" :
        Score >= 110 ? "强关联命中" :
        "模糊命中";

    public string DisplayIcon => IsDirectory ? "Dir" : "File";

    public string DisplayIconBrush => IsDirectory ? "#FFE0F0FE" : "#FFFEF3E0";

    public string BadgesText
    {
        get
        {
            var tags = new List<string>();

            if (IsDirectory)
            {
                tags.Add("文件夹");
            }

            if (!string.IsNullOrWhiteSpace(Extension) && !IsDirectory)
            {
                tags.Add(Extension.ToUpperInvariant());
            }

            if (IsHidden)
            {
                tags.Add("隐藏");
            }

            if (IsSystem)
            {
                tags.Add("系统");
            }

            return string.Join("  ·  ", tags);
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
