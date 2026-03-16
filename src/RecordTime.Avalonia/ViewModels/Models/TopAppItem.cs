using CommunityToolkit.Mvvm.ComponentModel;
using RecordTime.Avalonia.Resources.Strings;
using System;

namespace RecordTime.Avalonia.ViewModels;

public partial class TopAppItem : ObservableObject
{
    [ObservableProperty]
    private int _rank;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private global::Avalonia.Media.Imaging.Bitmap? _icon;

    [ObservableProperty]
    private double _percentage;

    public string RankText => $"#{Rank}";
    public string DurationText => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount}{StringResources.Current.UsageCountSuffix}";
    public string PercentageText => $"{Percentage:F1}%";
}
