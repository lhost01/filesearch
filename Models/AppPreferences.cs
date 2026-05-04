namespace 全局文件搜索.Models;

public sealed class AppPreferences
{
    public string BackgroundPath { get; set; } = string.Empty;

    public double BackgroundOpacity { get; set; } = 0.32;

    public bool BackgroundVideoPlaybackEnabled { get; set; } = true;

    public bool BackgroundVideoMuted { get; set; } = true;
}
