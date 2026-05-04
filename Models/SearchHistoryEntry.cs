using System;
using System.Collections.Generic;

namespace 全局文件搜索.Models;

public sealed class SearchHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public long ScannedFiles { get; set; }
    public double ElapsedSeconds { get; set; }
    public List<string> SearchedDrives { get; set; } = [];
    public bool IsCanceled { get; set; }
    public List<SavedResultSnapshot> SavedResults { get; set; } = [];

    public string StatusText => IsCanceled ? "已停止" : "已完成";

    public string ScopeSummary =>
        SearchedDrives.Count == 0
            ? "未记录搜索范围"
            : string.Join("  ·  ", SearchedDrives);

    public string ScopeWithQuerySummary =>
        string.IsNullOrWhiteSpace(Query)
            ? ScopeSummary
            : $"{ScopeSummary}  ·  {Query}";
}
