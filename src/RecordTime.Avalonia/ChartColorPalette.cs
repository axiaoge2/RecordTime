using SkiaSharp;

namespace RecordTime.Avalonia;

/// <summary>
/// Shared color palettes for LiveCharts across all ViewModels.
/// Uses a monochrome grayscale scheme consistent with the editorial design.
/// </summary>
public static class ChartColorPalette
{
    public static readonly SKColor[] Grays5 =
    {
        new(0x1A, 0x1A, 0x1A),
        new(0x4A, 0x4A, 0x4A),
        new(0x7A, 0x7A, 0x7A),
        new(0xA8, 0xA8, 0xA8),
        new(0xD0, 0xCF, 0xCB),
    };

    public static readonly SKColor[] Grays8 =
    {
        new(0x1A, 0x1A, 0x1A),
        new(0x3D, 0x3D, 0x3D),
        new(0x5C, 0x5C, 0x5C),
        new(0x7A, 0x7A, 0x7A),
        new(0x99, 0x99, 0x99),
        new(0xB0, 0xB0, 0xB0),
        new(0xC8, 0xC8, 0xC8),
        new(0xD8, 0xD8, 0xD8),
    };

    public static readonly SKColor TextDark = new(0x1A, 0x1A, 0x1A);
    public static readonly SKColor TextLight = new(0xF5, 0xF4, 0xF0);
    public static readonly SKColor TextMuted = new(0x6A, 0x6A, 0x6A);
    public static readonly SKColor GridLine = new(0xE2, 0xDF, 0xD6);
    public static readonly SKColor Fallback = new(0xB0, 0xB0, 0xB0);
}
