using CommunityToolkit.Mvvm.ComponentModel;

namespace 全局文件搜索.Models;

public sealed partial class DashboardParticle : ObservableObject
{
    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _size;

    [ObservableProperty]
    private double _opacity;

    public double VelocityX { get; set; }

    public double VelocityY { get; set; }
}
