using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LibVLCSharp.Shared;
using 全局文件搜索.ViewModels;

namespace 全局文件搜索.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        private LibVLC? _libVlc;
        private MediaPlayer? _backgroundMediaPlayer;
        private Media? _backgroundMedia;
        private string _currentBackgroundVideoPath = string.Empty;
        private bool _isOpened;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        protected override void OnOpened(EventArgs e)
        {
            _isOpened = true;
            UpdateBackgroundVideo();
            base.OnOpened(e);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            DetachViewModel();
            DisposeBackgroundVideo();

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SettingsPage.Dispose();
            }

            base.OnUnloaded(e);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            DetachViewModel();

            _viewModel = DataContext as MainWindowViewModel;
            if (_viewModel is not null)
            {
                _viewModel.SettingsPage.PropertyChanged += OnSettingsPagePropertyChanged;
            }

            UpdateBackgroundVideo();
        }

        private void OnSettingsPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.BackgroundVideoPath)
                || e.PropertyName == nameof(SettingsViewModel.BackgroundVideoPlaybackEnabled)
                || e.PropertyName == nameof(SettingsViewModel.BackgroundVideoMuted))
            {
                UpdateBackgroundVideo();
            }
        }

        private void UpdateBackgroundVideo()
        {
            if (!_isOpened || _viewModel is null)
            {
                return;
            }

            string videoPath = _viewModel.SettingsPage.BackgroundVideoPath;
            if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            {
                StopBackgroundVideo();
                return;
            }

            string fullPath = Path.GetFullPath(videoPath);

            EnsureBackgroundVideoPlayer();
            if (!string.Equals(_currentBackgroundVideoPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                StopBackgroundVideo();
                _backgroundMedia = new Media(_libVlc!, new Uri(fullPath), ":input-repeat=-1", ":no-audio");
                _backgroundMediaPlayer!.Media = _backgroundMedia;
                _currentBackgroundVideoPath = fullPath;
            }

            ApplyBackgroundVideoOptions();
        }

        private void EnsureBackgroundVideoPlayer()
        {
            if (_backgroundMediaPlayer is not null)
            {
                return;
            }

            Core.Initialize();
            _libVlc = new LibVLC("--quiet");
            _backgroundMediaPlayer = new MediaPlayer(_libVlc)
            {
                Mute = true,
                Volume = 0,
            };

            BackgroundVideoView.MediaPlayer = _backgroundMediaPlayer;
        }

        private void ApplyBackgroundVideoOptions()
        {
            if (_backgroundMediaPlayer is null || _viewModel is null)
            {
                return;
            }

            _backgroundMediaPlayer.Mute = _viewModel.SettingsPage.BackgroundVideoMuted;
            _backgroundMediaPlayer.Volume = _viewModel.SettingsPage.BackgroundVideoMuted ? 0 : 100;

            if (_backgroundMedia is null)
            {
                return;
            }

            if (_viewModel.SettingsPage.BackgroundVideoPlaybackEnabled)
            {
                _backgroundMediaPlayer.Play();
                return;
            }

            if (_backgroundMediaPlayer.State != VLCState.Paused && _backgroundMediaPlayer.IsPlaying)
            {
                _backgroundMediaPlayer.Pause();
            }
        }

        private void StopBackgroundVideo()
        {
            _currentBackgroundVideoPath = string.Empty;

            if (_backgroundMediaPlayer?.IsPlaying == true)
            {
                _backgroundMediaPlayer.Stop();
            }

            _backgroundMedia?.Dispose();
            _backgroundMedia = null;
        }

        private void DisposeBackgroundVideo()
        {
            StopBackgroundVideo();
            BackgroundVideoView.MediaPlayer = null;
            _backgroundMediaPlayer?.Dispose();
            _backgroundMediaPlayer = null;
            _libVlc?.Dispose();
            _libVlc = null;
        }

        private void DetachViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.SettingsPage.PropertyChanged -= OnSettingsPagePropertyChanged;
            _viewModel = null;
        }
    }
}
