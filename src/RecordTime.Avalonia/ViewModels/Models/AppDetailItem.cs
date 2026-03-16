using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RecordTime.Avalonia.Resources.Strings;
using System;

namespace RecordTime.Avalonia.ViewModels;

public partial class AppDetailItem : ObservableObject
{
    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private DateTime _firstUsed;

    [ObservableProperty]
    private DateTime _lastUsed;

    [ObservableProperty]
    private double _totalPercentage;

    [ObservableProperty]
    private Bitmap? _icon;

    public string DurationText => $"{(int)TotalDuration.TotalHours:D2}:{TotalDuration.Minutes:D2}:{TotalDuration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount}{StringResources.Current.UsageCountSuffix}";
    public string PercentageText => $"{TotalPercentage:F1}%";
    public string FirstUsedText => FirstUsed.ToString("HH:mm:ss");
    public string LastUsedText => LastUsed.ToString("HH:mm:ss");
}
