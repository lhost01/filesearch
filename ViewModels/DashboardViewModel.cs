using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using 全局文件搜索.Models;
using 全局文件搜索.Services;

namespace 全局文件搜索.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly SearchHistoryService _historyService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _animationTimer;
    private readonly Random _random = new();
    private const double ParticleCanvasWidth = 900;
    private const double ParticleCanvasHeight = 220;

    private static readonly List<string> DailyQuotes =
    [
        "工欲善其事，必先利其器。",
        "代码是写给人看的，顺便给机器运行。",
        "简单是可靠的先决条件。",
        "大道至简，知易行难。",
        "千里之行，始于足下。",
        "君子生非异也，善假于物也。",
        "不积跬步，无以至千里。",
        "天下大事，必作于细。",
        "十年磨一剑，霜刃未曾试。",
        "博观而约取，厚积而薄发。",
        "纸上得来终觉浅，绝知此事要躬行。",
        "学而不思则罔，思而不学则殆。",
        "敏而好学，不耻下问。",
        "温故而知新，可以为师矣。",
        "知之为知之，不知为不知，是知也。",
        "业精于勤，荒于嬉。",
        "路漫漫其修远兮，吾将上下而求索。",
        "志当存高远。",
        "非淡泊无以明志，非宁静无以致远。",
        "行到水穷处，坐看云起时。",
        "山重水复疑无路，柳暗花明又一村。",
        "会当凌绝顶，一览众山小。",
        "长风破浪会有时，直挂云帆济沧海。",
        "不畏浮云遮望眼，自缘身在最高层。",
        "千磨万击还坚劲，任尔东西南北风。",
        "问渠那得清如许，为有源头活水来。",
        "欲穷千里目，更上一层楼。",
        "海内存知己，天涯若比邻。",
        "沉舟侧畔千帆过，病树前头万木春。",
        "莫愁前路无知己，天下谁人不识君。",
    ];

    public DashboardViewModel(SearchHistoryService historyService)
    {
        _historyService = historyService;
        RecentEntries = new ObservableCollection<SearchHistoryEntry>();
        Particles = new ObservableCollection<DashboardParticle>();
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40),
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();

        InitializeParticles();

        RefreshStats();
        DailyQuote = PickDailyQuote();
    }

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    [ObservableProperty]
    private string _currentDate = DateTime.Now.ToString("yyyy年M月d日 dddd");

    [ObservableProperty]
    private int _totalSearchCount;

    [ObservableProperty]
    private string _totalSearchTime = "0 秒";

    [ObservableProperty]
    private string _dailyQuote = string.Empty;

    [ObservableProperty]
    private double _coreRotationAngle;

    [ObservableProperty]
    private double _ringRotationAngle = 180;

    [ObservableProperty]
    private double _counterRingRotationAngle;

    [ObservableProperty]
    private double _coreFloatOffset;

    public ObservableCollection<SearchHistoryEntry> RecentEntries { get; }

    public ObservableCollection<DashboardParticle> Particles { get; }

    public bool HasRecentEntries => RecentEntries.Count > 0;

    public bool HasNoRecentEntries => RecentEntries.Count == 0;

    public void RefreshStats()
    {
        TotalSearchCount = _historyService.GetTotalSearchCount();
        double seconds = _historyService.GetTotalElapsedSeconds();
        TotalSearchTime = FormatTimeSpan(seconds);

        RecentEntries.Clear();
        foreach (SearchHistoryEntry entry in _historyService.LoadRecent(5))
        {
            RecentEntries.Add(entry);
        }

        OnPropertyChanged(nameof(HasRecentEntries));
        OnPropertyChanged(nameof(HasNoRecentEntries));
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("yyyy-MM-dd HH:mm:ss");
        CurrentDate = now.ToString("yyyy年M月d日 dddd");
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        CoreRotationAngle = (CoreRotationAngle + 1.1) % 360;
        RingRotationAngle = (RingRotationAngle + 0.6) % 360;
        CounterRingRotationAngle = (CounterRingRotationAngle - 0.8 + 360) % 360;
        CoreFloatOffset = Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 1.6) * 5;

        foreach (DashboardParticle particle in Particles)
        {
            particle.X += particle.VelocityX;
            particle.Y += particle.VelocityY;

            if (particle.X > ParticleCanvasWidth + 20)
            {
                particle.X = -20;
            }
            else if (particle.X < -20)
            {
                particle.X = ParticleCanvasWidth + 20;
            }

            if (particle.Y > ParticleCanvasHeight + 20)
            {
                particle.Y = -20;
            }
            else if (particle.Y < -20)
            {
                particle.Y = ParticleCanvasHeight + 20;
            }
        }
    }

    private void InitializeParticles()
    {
        Particles.Clear();

        for (int i = 0; i < 22; i++)
        {
            Particles.Add(new DashboardParticle
            {
                X = _random.NextDouble() * ParticleCanvasWidth,
                Y = _random.NextDouble() * ParticleCanvasHeight,
                Size = 4 + _random.NextDouble() * 10,
                Opacity = 0.15 + _random.NextDouble() * 0.45,
                VelocityX = 0.25 + _random.NextDouble() * 0.75,
                VelocityY = -0.1 + _random.NextDouble() * 0.2,
            });
        }
    }

    private static string PickDailyQuote()
    {
        int dayOfYear = DateTime.Now.DayOfYear;
        int index = dayOfYear % DailyQuotes.Count;
        return DailyQuotes[index];
    }

    private static string FormatTimeSpan(double totalSeconds)
    {
        if (totalSeconds < 1)
            return "< 1 秒";

        var ts = TimeSpan.FromSeconds(totalSeconds);

        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays} 天 {ts.Hours} 小时 {ts.Minutes} 分钟 {ts.Seconds} 秒";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours} 小时 {ts.Minutes} 分钟 {ts.Seconds} 秒";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes} 分钟 {ts.Seconds} 秒";
        return $"{ts.Seconds} 秒";
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        _clockTimer.Tick -= OnClockTick;
        _animationTimer.Stop();
        _animationTimer.Tick -= OnAnimationTick;
    }
}
