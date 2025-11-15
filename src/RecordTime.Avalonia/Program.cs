using Avalonia;
using Avalonia.Win32;
using System;

namespace RecordTime.Avalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new Win32PlatformOptions
            {
                // 优先使用硬件加速渲染 (AngleEgl = DirectX/OpenGL, 后备Software)
                RenderingMode = new[] { Win32RenderingMode.AngleEgl, Win32RenderingMode.Software }
            });
}
