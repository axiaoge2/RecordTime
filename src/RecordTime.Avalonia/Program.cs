using Avalonia;
using Avalonia.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace RecordTime.Avalonia;

sealed class Program
{
    // 单实例互斥锁
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "RecordTime_SingleInstance_Mutex";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 初始化 Serilog（需要先初始化日志以便记录单实例检查）
        InitializeSerilog();

        // 单实例检测：如果已有实例运行，则尝试激活它并退出
        if (!EnsureSingleInstance())
        {
            Log.Warning("检测到已有 RecordTime 实例在运行，尝试终止旧进程...");

            // 尝试终止旧进程并等待
            if (TryTerminateExistingInstance())
            {
                Log.Information("旧进程已终止，继续启动新实例");
                // 重新尝试获取互斥锁
                if (!EnsureSingleInstance())
                {
                    Log.Error("无法获取单实例锁，退出");
                    return;
                }
            }
            else
            {
                Log.Error("无法终止旧进程，退出");
                return;
            }
        }

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
            // 释放单实例锁
            ReleaseSingleInstanceMutex();
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 确保单实例运行
    /// </summary>
    /// <returns>如果是第一个实例返回 true，否则返回 false</returns>
    private static bool EnsureSingleInstance()
    {
        try
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // 已有实例在运行
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }

            Log.Debug("单实例锁已获取");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取单实例锁时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 尝试终止已存在的实例
    /// </summary>
    /// <returns>如果成功终止返回 true</returns>
    private static bool TryTerminateExistingInstance()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var existingProcesses = Process.GetProcessesByName("RecordTime")
                .Where(p => p.Id != currentProcess.Id)
                .ToArray();

            if (existingProcesses.Length == 0)
            {
                Log.Debug("没有发现其他 RecordTime 进程");
                return true;
            }

            Log.Information("发现 {Count} 个旧 RecordTime 进程，正在终止...", existingProcesses.Length);

            foreach (var process in existingProcesses)
            {
                try
                {
                    Log.Debug("正在终止进程 PID={ProcessId}", process.Id);
                    process.Kill();

                    // 等待进程退出（最多5秒）
                    if (process.WaitForExit(5000))
                    {
                        Log.Debug("进程 PID={ProcessId} 已退出", process.Id);
                    }
                    else
                    {
                        Log.Warning("进程 PID={ProcessId} 未能在超时内退出", process.Id);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "终止进程 PID={ProcessId} 时发生错误", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }

            // 额外等待一小段时间，确保资源完全释放
            Thread.Sleep(500);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "终止已存在实例时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 释放单实例互斥锁
    /// </summary>
    private static void ReleaseSingleInstanceMutex()
    {
        try
        {
            if (_singleInstanceMutex != null)
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                Log.Debug("单实例锁已释放");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "释放单实例锁时发生错误");
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
