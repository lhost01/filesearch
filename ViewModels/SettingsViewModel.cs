using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using 全局文件搜索.Models;
using 全局文件搜索.Services;

namespace 全局文件搜索.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase, IDisposable
{
    private Bitmap? _backgroundBitmap;
    private BackgroundMediaDescriptor _currentBackground = BackgroundMediaDescriptor.None("未设置背景，当前使用内置渐变背景。");
    private readonly AppPreferencesService _preferencesService;
    private bool _isRestoringPreferences;

    public SettingsViewModel()
    {
        _preferencesService = new AppPreferencesService();
        BackgroundStatus = _currentBackground.Status;
        ClearBackgroundImageCommand = new RelayCommand(ClearBackgroundImage);
        RestorePreferences();
    }

    public IRelayCommand ClearBackgroundImageCommand { get; }

    [ObservableProperty]
    private string backgroundImagePath = string.Empty;

    [ObservableProperty]
    private IImage? backgroundImage;

    [ObservableProperty]
    private string backgroundVideoPath = string.Empty;

    [ObservableProperty]
    private string backgroundStatus = string.Empty;

    [ObservableProperty]
    private double backgroundImageOpacity = 0.32;

    [ObservableProperty]
    private bool backgroundVideoPlaybackEnabled = true;

    [ObservableProperty]
    private bool backgroundVideoMuted = true;

    public bool HasBackgroundImage => BackgroundImage is not null;

    public bool HasBackgroundVideo => !string.IsNullOrWhiteSpace(BackgroundVideoPath);

    public bool CanControlBackgroundVideo => HasBackgroundVideo;

    public void ApplyBackgroundImage(string? filePath)
    {
        BackgroundImagePath = filePath?.Trim() ?? string.Empty;
    }

    partial void OnBackgroundImagePathChanged(string value)
    {
        LoadBackgroundMedia(value);
    }

    partial void OnBackgroundImageChanged(IImage? value)
    {
        OnPropertyChanged(nameof(HasBackgroundImage));
    }

    partial void OnBackgroundVideoPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasBackgroundVideo));
        OnPropertyChanged(nameof(CanControlBackgroundVideo));
        SavePreferences();
    }

    partial void OnBackgroundImageOpacityChanged(double value)
    {
        SavePreferences();
    }

    partial void OnBackgroundVideoPlaybackEnabledChanged(bool value)
    {
        SavePreferences();
    }

    partial void OnBackgroundVideoMutedChanged(bool value)
    {
        SavePreferences();
    }

    private void ClearBackgroundImage()
    {
        BackgroundImagePath = string.Empty;
    }

    private void RestorePreferences()
    {
        _isRestoringPreferences = true;

        try
        {
            AppPreferences preferences = _preferencesService.Load();
            BackgroundImageOpacity = preferences.BackgroundOpacity;
            BackgroundVideoPlaybackEnabled = preferences.BackgroundVideoPlaybackEnabled;
            BackgroundVideoMuted = preferences.BackgroundVideoMuted;
            BackgroundImagePath = preferences.BackgroundPath;
        }
        finally
        {
            _isRestoringPreferences = false;
        }

        SavePreferences();
    }

    private void SavePreferences()
    {
        if (_isRestoringPreferences)
        {
            return;
        }

        _preferencesService.Save(new AppPreferences
        {
            BackgroundPath = BackgroundImagePath,
            BackgroundOpacity = BackgroundImageOpacity,
            BackgroundVideoPlaybackEnabled = BackgroundVideoPlaybackEnabled,
            BackgroundVideoMuted = BackgroundVideoMuted,
        });
    }

    private void LoadBackgroundMedia(string filePath)
    {
        _backgroundBitmap?.Dispose();
        _backgroundBitmap = null;
        BackgroundImage = null;
        BackgroundVideoPath = string.Empty;

        _currentBackground = BackgroundMediaResolver.Resolve(filePath);
        BackgroundStatus = _currentBackground.Status;

        try
        {
            switch (_currentBackground.Kind)
            {
                case BackgroundMediaKind.None:
                    return;
                case BackgroundMediaKind.Image:
                    _backgroundBitmap = new Bitmap(_currentBackground.ResolvedPath);
                    BackgroundImage = _backgroundBitmap;
                    return;
                case BackgroundMediaKind.Video:
                    BackgroundVideoPath = _currentBackground.ResolvedPath;
                    return;
                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            BackgroundStatus = $"背景资源加载失败: {ex.Message}";
        }

        SavePreferences();
    }

    public void Dispose()
    {
        _backgroundBitmap?.Dispose();
    }
}
