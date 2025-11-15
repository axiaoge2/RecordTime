using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RecordTime.Avalonia.ViewModels;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace RecordTime.Avalonia.Views;

[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    private NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();

        // Handle window closing
        Closing += OnWindowClosing;
    }

    [SupportedOSPlatform("windows")]
    private void InitializeTrayIcon()
    {
        try
        {
            // Create tray icon
            _trayIcon = new NotifyIcon
            {
                Text = "RecordTime - 时间追踪工具",
                Visible = true
            };

            // Load icon (using default application icon)
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "avalonia-logo.ico");
            if (System.IO.File.Exists(iconPath))
            {
                _trayIcon.Icon = new Icon(iconPath);
            }

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(showItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Single click to toggle window visibility
            _trayIcon.Click += (s, e) =>
            {
                if (e is MouseEventArgs mouseEvent && mouseEvent.Button == MouseButtons.Left)
                {
                    ToggleWindow();
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化系统托盘失败: {ex.Message}");
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Check if we should minimize to tray
        var viewModel = DataContext as MainWindowViewModel;

        // For now, always minimize to tray instead of closing
        e.Cancel = true;
        Hide();

        // Show notification
        if (_trayIcon != null)
        {
            _trayIcon.BalloonTipTitle = "RecordTime";
            _trayIcon.BalloonTipText = "应用已最小化到系统托盘";
            _trayIcon.ShowBalloonTip(2000);
        }
    }

    private void ToggleWindow()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        // Dispose tray icon
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        // Dispose ViewModel
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }

        // Close application
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}