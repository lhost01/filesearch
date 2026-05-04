namespace 全局文件搜索.Models;

public enum BackgroundMediaKind
{
    None,
    Image,
    Video,
}

public sealed record BackgroundMediaDescriptor(
    BackgroundMediaKind Kind,
    string OriginalPath,
    string ResolvedPath,
    string Status,
    bool IsWallpaperEngineSource = false)
{
    public static BackgroundMediaDescriptor None(string status) =>
        new(BackgroundMediaKind.None, string.Empty, string.Empty, status);
}
