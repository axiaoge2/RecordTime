using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RecordTime.Core.Exceptions;
using RecordTime.Avalonia.Views;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 全局异常处理器 - 单例模式
/// </summary>
public class GlobalExceptionHandler
{
    private static GlobalExceptionHandler? _instance;
    private static readonly object _lock = new();

    public static GlobalExceptionHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GlobalExceptionHandler();
                    }
                }
            }
            return _instance;
        }
    }

    private GlobalExceptionHandler()
    {
    }

    /// <summary>
    /// 注册全局异常处理
    /// </summary>
    public void RegisterGlobalHandlers()
    {
        // 处理未捕获的异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 处理 Task 未观察的异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Log.Information("全局异常处理器已注册");
    }

    /// <summary>
    /// 注销全局异常处理
    /// </summary>
    public void UnregisterGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        Log.Information("全局异常处理器已注销");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        Log.Fatal(exception, "发生未处理的致命异常");

        if (exception != null)
        {
            HandleException(exception, isFatal: e.IsTerminating);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Task 中发生未观察的异常");

        // 标记异常已被观察，防止应用崩溃
        e.SetObserved();

        HandleException(e.Exception);
    }

    /// <summary>
    /// 处理异常并显示用户友好的消息
    /// </summary>
    public void HandleException(Exception exception, bool isFatal = false)
    {
        try
        {
            // 记录日志
            if (isFatal)
            {
                Log.Fatal(exception, "致命错误: {Message}", exception.Message);
            }
            else
            {
                Log.Error(exception, "错误: {Message}", exception.Message);
            }

            // 提取用户友好的消息
            string userMessage = GetUserFriendlyMessage(exception);
            string title = isFatal ? "严重错误" : "错误";

            // 在 UI 线程上显示错误对话框
            _ = global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowErrorDialog(title, userMessage, isFatal);
            });
        }
        catch (Exception ex)
        {
            // 最后的防线：如果异常处理本身失败，至少记录日志
            Log.Fatal(ex, "异常处理器本身发生错误");
        }
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            RecordTimeException recordTimeEx => recordTimeEx.UserMessage,

            System.Data.Common.DbException => "数据库操作失败，请稍后重试",

            System.IO.IOException => "文件访问失败，请检查文件是否被占用",

            UnauthorizedAccessException => "权限不足，请以管理员身份运行应用",

            OutOfMemoryException => "内存不足，请关闭其他应用后重试",

            _ => $"应用遇到问题: {exception.Message}\n\n如果问题持续，请联系技术支持"
        };
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private async Task ShowErrorDialog(string title, string message, bool isFatal)
    {
        try
        {
            // 获取主窗口
            if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    // 使用自定义的 ErrorDialog
                    var dialog = new ErrorDialog(
                        title,
                        message,
                        isFatal,
                        subtitle: isFatal ? "应用程序遇到无法恢复的错误" : null);

                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    Log.Warning("无法显示错误对话框: MainWindow 为 null");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "显示错误对话框失败");
        }
    }
}
