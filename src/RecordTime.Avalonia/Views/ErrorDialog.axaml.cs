using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;

namespace RecordTime.Avalonia.Views;

public partial class ErrorDialog : Window
{
    private readonly bool _isFatal;

    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialog(string title, string message, bool isFatal = false, string? subtitle = null) : this()
    {
        _isFatal = isFatal;

        // 设置标题
        TitleText.Text = title;

        // 设置副标题
        if (!string.IsNullOrEmpty(subtitle))
        {
            SubtitleText.Text = subtitle;
            SubtitleText.IsVisible = true;
        }
        else
        {
            SubtitleText.IsVisible = false;
        }

        // 设置消息内容
        MessageText.Text = message;

        // 如果是致命错误,更改按钮文本
        if (isFatal)
        {
            OkButton.Content = "退出应用";
            ViewLogsButton.IsVisible = true;
        }

        // 设置窗口标题
        this.Title = isFatal ? "严重错误" : "错误";
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (_isFatal)
        {
            // 关闭日志
            Log.CloseAndFlush();

            // 退出应用
            Environment.Exit(1);
        }
        else
        {
            Close();
        }
    }

    private void OnViewLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // 获取日志文件夹路径
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "logs");

            if (Directory.Exists(logFolder))
            {
                // 打开日志文件夹
                Process.Start(new ProcessStartInfo
                {
                    FileName = logFolder,
                    UseShellExecute = true
                });
            }
            else
            {
                Log.Warning("日志文件夹不存在: {LogFolder}", logFolder);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开日志文件夹失败");
        }
    }
}
