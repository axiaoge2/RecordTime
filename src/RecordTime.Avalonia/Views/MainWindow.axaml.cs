using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RecordTime.Avalonia.ViewModels;
using System;

namespace RecordTime.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // 托盘图标功能已移至 App.axaml.cs 统一管理
    }
}
