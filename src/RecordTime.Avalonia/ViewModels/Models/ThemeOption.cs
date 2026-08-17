using RecordTime.Avalonia.Services;

namespace RecordTime.Avalonia.ViewModels;

public class ThemeOption
{
    public ThemeService.ThemeMode Mode { get; set; }
    public string NameZh { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}
