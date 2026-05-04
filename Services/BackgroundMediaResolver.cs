using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using 全局文件搜索.Models;

namespace 全局文件搜索.Services;

public static class BackgroundMediaResolver
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"];
    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".avi", ".mov", ".mkv", ".wmv", ".m4v"];
    private static readonly string[] PreviewBaseNames = ["preview", "scene", "thumbnail"];
    private static readonly string[] PackageExtensions = [".mpkg"];

    public static BackgroundMediaDescriptor Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BackgroundMediaDescriptor.None("未设置背景，当前使用内置渐变背景。");
        }

        string normalizedPath = path.Trim().Trim('"');

        if (Directory.Exists(normalizedPath))
        {
            return ResolveDirectory(normalizedPath);
        }

        if (File.Exists(normalizedPath))
        {
            return ResolveFile(normalizedPath);
        }

        return BackgroundMediaDescriptor.None("背景路径无效，请重新选择图片、视频或 Wallpaper Engine 项目。");
    }

    private static BackgroundMediaDescriptor ResolveDirectory(string directoryPath)
    {
        string projectJsonPath = Path.Combine(directoryPath, "project.json");
        if (File.Exists(projectJsonPath))
        {
            return ResolveWallpaperEngineProject(projectJsonPath, directoryPath);
        }

        string? mediaFile = FindBestMediaFile(directoryPath);
        if (!string.IsNullOrWhiteSpace(mediaFile))
        {
            return CreateDirectMediaDescriptor(directoryPath, mediaFile);
        }

        return BackgroundMediaDescriptor.None("目录中没有找到可用的背景图片或视频。");
    }

    private static BackgroundMediaDescriptor ResolveFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        if (IsImageFile(extension))
        {
            return new(
                BackgroundMediaKind.Image,
                filePath,
                filePath,
                $"图片背景已加载: {Path.GetFileName(filePath)}");
        }

        if (IsVideoFile(extension))
        {
            return new(
                BackgroundMediaKind.Video,
                filePath,
                filePath,
                $"视频背景已加载: {Path.GetFileName(filePath)}");
        }

        if (IsPackageFile(extension))
        {
            return ResolveWallpaperPackage(filePath);
        }

        if (Path.GetFileName(filePath).Equals("project.json", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveWallpaperEngineProject(filePath, Path.GetDirectoryName(filePath)!);
        }

        return BackgroundMediaDescriptor.None("暂不支持该背景文件类型，请选择图片、视频或 Wallpaper Engine 项目。");
    }

    private static BackgroundMediaDescriptor ResolveWallpaperEngineProject(string projectJsonPath, string projectDirectory)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(projectJsonPath));
            JsonElement root = document.RootElement;

            string type = GetString(root, "type")?.Trim().ToLowerInvariant() ?? string.Empty;
            string? fileValue = ResolveProjectPath(projectDirectory, GetString(root, "file"));
            string? previewValue = ResolveProjectPath(projectDirectory, GetString(root, "preview"));

            BackgroundMediaDescriptor? directMatch = TryResolveWallpaperEnginePrimary(type, projectJsonPath, fileValue, previewValue);
            if (directMatch is not null)
            {
                return directMatch;
            }

            string? fallback = FindBestMediaFile(projectDirectory, searchRecursively: false);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                BackgroundMediaDescriptor descriptor = CreateDirectMediaDescriptor(projectJsonPath, fallback, true);
                return descriptor with
                {
                    Status = $"Wallpaper Engine 壁纸已回退到可显示资源: {Path.GetFileName(fallback)}",
                };
            }

            return BackgroundMediaDescriptor.None("该 Wallpaper Engine 壁纸没有可直接显示的图片或视频资源。");
        }
        catch (Exception ex)
        {
            return BackgroundMediaDescriptor.None($"Wallpaper Engine 项目解析失败: {ex.Message}");
        }
    }

    private static BackgroundMediaDescriptor ResolveWallpaperPackage(string packagePath)
    {
        try
        {
            string extractionDirectory = EnsurePackageExtracted(packagePath);
            BackgroundMediaDescriptor descriptor = ResolveDirectory(extractionDirectory);

            if (descriptor.Kind == BackgroundMediaKind.None)
            {
                string? fallback = FindBestMediaFile(extractionDirectory, searchRecursively: true);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    descriptor = CreateDirectMediaDescriptor(packagePath, fallback, true);
                }
                else
                {
                    return BackgroundMediaDescriptor.None(".mpkg 已解包，但没有找到可直接显示的图片或视频资源。");
                }
            }

            return descriptor with
            {
                OriginalPath = packagePath,
                Status = $".mpkg 壁纸包已加载: {Path.GetFileName(packagePath)}",
                IsWallpaperEngineSource = true,
            };
        }
        catch (InvalidDataException)
        {
            return BackgroundMediaDescriptor.None("该 .mpkg 文件不是可直接解包的壁纸包，当前无法解析。");
        }
        catch (Exception ex)
        {
            return BackgroundMediaDescriptor.None($".mpkg 解包失败: {ex.Message}");
        }
    }

    private static BackgroundMediaDescriptor? TryResolveWallpaperEnginePrimary(
        string type,
        string originalPath,
        string? fileValue,
        string? previewValue)
    {
        if (IsExistingVideo(fileValue))
        {
            return new(
                BackgroundMediaKind.Video,
                originalPath,
                fileValue!,
                BuildWallpaperStatus(type, fileValue!, isPreview: false),
                true);
        }

        if (IsExistingVideo(previewValue))
        {
            return new(
                BackgroundMediaKind.Video,
                originalPath,
                previewValue!,
                BuildWallpaperStatus(type, previewValue!, isPreview: true),
                true);
        }

        if (IsExistingImage(previewValue))
        {
            return new(
                BackgroundMediaKind.Image,
                originalPath,
                previewValue!,
                BuildWallpaperStatus(type, previewValue!, isPreview: true),
                true);
        }

        if (IsExistingImage(fileValue))
        {
            return new(
                BackgroundMediaKind.Image,
                originalPath,
                fileValue!,
                BuildWallpaperStatus(type, fileValue!, isPreview: false),
                true);
        }

        return null;
    }

    private static BackgroundMediaDescriptor CreateDirectMediaDescriptor(string originalPath, string resolvedPath, bool isWallpaperEngineSource = false)
    {
        string extension = Path.GetExtension(resolvedPath);
        if (IsVideoFile(extension))
        {
            return new(
                BackgroundMediaKind.Video,
                originalPath,
                resolvedPath,
                $"视频背景已加载: {Path.GetFileName(resolvedPath)}",
                isWallpaperEngineSource);
        }

        return new(
            BackgroundMediaKind.Image,
            originalPath,
            resolvedPath,
            $"图片背景已加载: {Path.GetFileName(resolvedPath)}",
            isWallpaperEngineSource);
    }

    private static string? FindBestMediaFile(string directoryPath, bool searchRecursively = false)
    {
        try
        {
            var files = Directory
                .EnumerateFiles(directoryPath, "*", searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .OrderBy(file => GetPriority(Path.GetFileNameWithoutExtension(file)))
                .ThenBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return files.FirstOrDefault(file => IsVideoFile(Path.GetExtension(file)))
                ?? files.FirstOrDefault(file => IsImageFile(Path.GetExtension(file)));
        }
        catch
        {
            return null;
        }
    }

    private static int GetPriority(string fileNameWithoutExtension)
    {
        string normalizedName = fileNameWithoutExtension.Trim().ToLowerInvariant();

        for (int index = 0; index < PreviewBaseNames.Length; index++)
        {
            if (normalizedName.Contains(PreviewBaseNames[index], StringComparison.Ordinal))
            {
                return index;
            }
        }

        return PreviewBaseNames.Length;
    }

    private static string EnsurePackageExtracted(string packagePath)
    {
        string cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "全局文件搜索",
            "WallpaperCache");

        Directory.CreateDirectory(cacheRoot);

        string signature = BuildPackageSignature(packagePath);
        string extractionDirectory = Path.Combine(cacheRoot, signature);
        string markerFile = Path.Combine(extractionDirectory, ".extract-ok");

        if (Directory.Exists(extractionDirectory) && File.Exists(markerFile))
        {
            return extractionDirectory;
        }

        if (Directory.Exists(extractionDirectory))
        {
            Directory.Delete(extractionDirectory, recursive: true);
        }

        Directory.CreateDirectory(extractionDirectory);
        ZipFile.ExtractToDirectory(packagePath, extractionDirectory);
        File.WriteAllText(markerFile, packagePath);
        return extractionDirectory;
    }

    private static string BuildPackageSignature(string packagePath)
    {
        var fileInfo = new FileInfo(packagePath);
        string raw = $"{packagePath}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash[..16]);
    }

    private static string? ResolveProjectPath(string baseDirectory, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : null;
        }

        string combined = Path.GetFullPath(Path.Combine(baseDirectory, normalized));
        return File.Exists(combined) ? combined : null;
    }

    private static string BuildWallpaperStatus(string type, string path, bool isPreview)
    {
        string wallpaperType = type switch
        {
            "video" => "视频壁纸",
            "scene" => "场景壁纸",
            "web" => "网页壁纸",
            _ => "壁纸项目",
        };

        string suffix = isPreview ? "预览资源" : "主资源";
        return $"Wallpaper Engine {wallpaperType}已加载{suffix}: {Path.GetFileName(path)}";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool IsImageFile(string extension) =>
        ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    private static bool IsVideoFile(string extension) =>
        VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    private static bool IsPackageFile(string extension) =>
        PackageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    private static bool IsExistingImage(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) && IsImageFile(Path.GetExtension(path));

    private static bool IsExistingVideo(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) && IsVideoFile(Path.GetExtension(path));
}
