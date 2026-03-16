using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RecordTime.Avalonia.ViewModels;

public partial class CategoryStatItem : ObservableObject
{
    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private double _percentage;

    public string DurationText => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    public string PercentageText => $"{Percentage:F1}%";
}
