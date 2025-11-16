using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using RecordTime.Avalonia.ViewModels;
using RecordTime.Avalonia.Views;
using RecordTime.Avalonia.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;

namespace RecordTime.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 配置 LiveCharts 主题
        LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .AddDefaultMappers()
                .AddLightTheme()
        );

        // 注册全局异常处理
        SetupExceptionHandling();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    /// <summary>
    /// 设置全局异常处理
    /// </summary>
    private void SetupExceptionHandling()
    {
        // 使用统一的全局异常处理器
        GlobalExceptionHandler.Instance.RegisterGlobalHandlers();
    }
}