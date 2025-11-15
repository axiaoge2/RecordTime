using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RecordTime.Core.Services;
using RecordTime.Data;
using RecordTime.Data.Repositories;

namespace RecordTime.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 核心服务
        services.AddSingleton<IWindowMonitor, WindowMonitor>();
        services.AddSingleton<IActivityDetector, ActivityDetector>();

        // 数据服务
        services.AddDbContext<RecordTimeDbContext>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        // UI服务
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();
    }
}
