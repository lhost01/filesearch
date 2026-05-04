using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using 全局文件搜索.Models;

namespace 全局文件搜索.Services;

public enum SearchMode
{
    Exact,
    Fuzzy,
    Both,
}

public sealed class SearchRequest
{
    public required string Query { get; init; }

    public required IReadOnlyList<string> Roots { get; init; }

    public bool IncludeSystemFiles { get; init; } = true;

    public bool IncludeHiddenFiles { get; init; } = true;

    public bool IncludeFolders { get; init; } = true;

    public string? SpecificFolder { get; init; }

    public SearchMode SearchMode { get; init; } = SearchMode.Both;
}

public sealed class SearchProgressUpdate
{
    public long ScannedFiles { get; init; }

    public int MatchedFiles { get; init; }

    public string? CurrentLocation { get; init; }

    public SearchResultItem? NewResult { get; init; }
}

public sealed class SearchExecutionResult
{
    public required IReadOnlyList<SearchResultItem> Results { get; init; }

    public required long ScannedFiles { get; init; }

    public required TimeSpan Elapsed { get; init; }

    public string? LastVisitedLocation { get; init; }
}

public interface IFileSearchService
{
    Task<SearchExecutionResult> SearchAsync(
        SearchRequest request,
        IProgress<SearchProgressUpdate>? progress,
        CancellationToken cancellationToken);
}

public sealed class FileSearchService : IFileSearchService
{
    public Task<SearchExecutionResult> SearchAsync(
        SearchRequest request,
        IProgress<SearchProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SearchCore(request, progress, cancellationToken), cancellationToken);
    }

    private static EnumerationOptions CreateEnumerationOptions(SearchRequest request)
    {
        FileAttributes attributesToSkip = 0;

        if (!request.IncludeHiddenFiles)
        {
            attributesToSkip |= FileAttributes.Hidden;
        }

        if (!request.IncludeSystemFiles)
        {
            attributesToSkip |= FileAttributes.System;
        }

        return new EnumerationOptions
        {
            AttributesToSkip = attributesToSkip,
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };
    }

    private static SearchExecutionResult SearchCore(
        SearchRequest request,
        IProgress<SearchProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<SearchResultItem>(capacity: 512);
        long scannedFiles = 0;
        string? lastVisitedLocation = null;
        var enumOptions = CreateEnumerationOptions(request);

        string query = request.Query.Trim();
        string normalizedQuery = query.ToLowerInvariant();

        IEnumerable<string> roots;

        if (!string.IsNullOrWhiteSpace(request.SpecificFolder) && Directory.Exists(request.SpecificFolder))
        {
            roots = [request.SpecificFolder];
        }
        else
        {
            roots = request.Roots.Where(Directory.Exists);
        }

        foreach (string root in roots)
        {
            SearchRoot(
                root,
                normalizedQuery,
                results,
                progress,
                cancellationToken,
                ref scannedFiles,
                ref lastVisitedLocation,
                enumOptions,
                request.IncludeFolders,
                request.SearchMode);
        }

        stopwatch.Stop();

        var sortedResults = results
            .OrderByDescending(item => item.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SearchExecutionResult
        {
            Results = sortedResults,
            ScannedFiles = scannedFiles,
            Elapsed = stopwatch.Elapsed,
            LastVisitedLocation = lastVisitedLocation,
        };
    }

    private static void SearchRoot(
        string root,
        string normalizedQuery,
        ICollection<SearchResultItem> results,
        IProgress<SearchProgressUpdate>? progress,
        CancellationToken cancellationToken,
        ref long scannedFiles,
        ref string? lastVisitedLocation,
        EnumerationOptions enumOptions,
        bool includeFolders,
        SearchMode searchMode)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(root);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string currentDirectory = pendingDirectories.Pop();

            IEnumerable<string> entries;

            try
            {
                entries = Directory.EnumerateFileSystemEntries(currentDirectory, "*", enumOptions);
            }
            catch
            {
                continue;
            }

            using IEnumerator<string> enumerator = entries.GetEnumerator();

            while (true)
            {
                string entry;

                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    entry = enumerator.Current;
                }
                catch
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                lastVisitedLocation = entry;

                FileAttributes attributes;

                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch
                {
                    continue;
                }

                bool isDirectory = (attributes & FileAttributes.Directory) != 0;

                if (isDirectory)
                {
                    if ((attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        pendingDirectories.Push(entry);
                    }

                    if (!includeFolders)
                    {
                        continue;
                    }
                }

                scannedFiles++;

                if (TryCreateResult(entry, attributes, normalizedQuery, out SearchResultItem? foundResult, searchMode, isDirectory)
                    && foundResult is not null)
                {
                    SearchResultItem result = foundResult;
                    results.Add(result);
                    progress?.Report(new SearchProgressUpdate
                    {
                        ScannedFiles = scannedFiles,
                        MatchedFiles = results.Count,
                        CurrentLocation = lastVisitedLocation,
                        NewResult = result,
                    });
                }
                else if (scannedFiles % 250 == 0)
                {
                    progress?.Report(new SearchProgressUpdate
                    {
                        ScannedFiles = scannedFiles,
                        MatchedFiles = results.Count,
                        CurrentLocation = lastVisitedLocation,
                    });
                }
            }
        }
    }

    private static bool TryCreateResult(
        string fullPath,
        FileAttributes attributes,
        string normalizedQuery,
        out SearchResultItem? result,
        SearchMode searchMode,
        bool isDirectory = false)
    {
        result = null;

        string name = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string directoryPath = isDirectory ? fullPath : (Path.GetDirectoryName(fullPath) ?? string.Empty);
        string extension = isDirectory ? string.Empty : Path.GetExtension(fullPath);
        string normalizedName = name.ToLowerInvariant();
        string normalizedPath = fullPath.ToLowerInvariant();

        double score = CalculateScore(normalizedQuery, normalizedName, normalizedPath, searchMode);
        if (score <= 0)
        {
            return false;
        }

        long sizeBytes = 0;
        DateTime lastWriteTime = DateTime.MinValue;

        if (!isDirectory)
        {
            try
            {
                var info = new FileInfo(fullPath);
                sizeBytes = info.Exists ? info.Length : 0;
                lastWriteTime = info.Exists ? info.LastWriteTime : DateTime.MinValue;
            }
            catch
            {
            }
        }
        else
        {
            try
            {
                var dirInfo = new DirectoryInfo(fullPath);
                lastWriteTime = dirInfo.Exists ? dirInfo.LastWriteTime : DateTime.MinValue;
            }
            catch
            {
            }
        }

        result = new SearchResultItem(
            name,
            fullPath,
            directoryPath,
            extension,
            sizeBytes,
            lastWriteTime,
            score,
            (attributes & FileAttributes.Hidden) != 0,
            (attributes & FileAttributes.System) != 0,
            isDirectory);

        return true;
    }

    private static double CalculateScore(
        string normalizedQuery,
        string normalizedName,
        string normalizedPath,
        SearchMode searchMode)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return 0;
        }

        double exactNameScore = 0;

        if (normalizedName.Equals(normalizedQuery, StringComparison.Ordinal))
        {
            exactNameScore = 200;
        }
        else if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            int offset = normalizedName.IndexOf(normalizedQuery, StringComparison.Ordinal);
            exactNameScore = 150 - Math.Min(offset, 50);
        }

        if (searchMode == SearchMode.Exact)
        {
            return exactNameScore;
        }

        double fuzzyScore = 0;

        if (exactNameScore > 0)
        {
            fuzzyScore = Math.Max(fuzzyScore, exactNameScore + 30);
        }

        if (normalizedPath.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            fuzzyScore = Math.Max(fuzzyScore, 80);
        }

        double nameFuzzy = GetFuzzyScore(normalizedName, normalizedQuery);
        if (nameFuzzy > 0)
        {
            fuzzyScore = Math.Max(fuzzyScore, 15 + nameFuzzy);
        }

        double pathFuzzy = GetFuzzyScore(normalizedPath, normalizedQuery);
        if (pathFuzzy > 0)
        {
            fuzzyScore = Math.Max(fuzzyScore, pathFuzzy);
        }

        if (searchMode == SearchMode.Fuzzy)
        {
            return fuzzyScore;
        }

        return Math.Max(exactNameScore, fuzzyScore);
    }

    private static double GetFuzzyScore(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
        {
            return 0;
        }

        int sourceIndex = 0;
        int matchedCount = 0;
        int consecutiveBonus = 0;
        int firstMatchIndex = -1;

        foreach (char queryChar in query)
        {
            bool matched = false;

            while (sourceIndex < source.Length)
            {
                if (source[sourceIndex] == queryChar)
                {
                    if (firstMatchIndex < 0)
                    {
                        firstMatchIndex = sourceIndex;
                    }

                    matchedCount++;
                    consecutiveBonus++;
                    sourceIndex++;
                    matched = true;
                    break;
                }

                consecutiveBonus = 0;
                sourceIndex++;
            }

            if (!matched)
            {
                return 0;
            }
        }

        return (matchedCount * 6) + (consecutiveBonus * 2) + Math.Max(0, 20 - firstMatchIndex);
    }
}
