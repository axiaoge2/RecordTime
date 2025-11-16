using Avalonia;
using Avalonia.Win32;
using Serilog;
using System;
using System.IO;

namespace RecordTime.Avalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 初始化 Serilog
        InitializeSerilog();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 初始化 Serilog 日志系统
    /// </summary>
    private static void InitializeSerilog()
    {
        // 日志文件存储路径: %LOCALAPPDATA%\RecordTime\logs\
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime",
            "logs");

        // 确保日志目录存在
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "app-.txt");

        Log.Logger = new LoggerConfiguration()
            #if DEBUG
            .MinimumLevel.Debug()
            #else
            .MinimumLevel.Information()
            #endif
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("==========================================================");
        Log.Information("RecordTime 应用程序启动");
        Log.Information("日志路径: {LogDirectory}", logDirectory);
        Log.Information("==========================================================");
    }

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
