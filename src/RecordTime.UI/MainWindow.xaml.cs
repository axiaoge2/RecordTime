using Microsoft.UI.Xaml;

namespace RecordTime.UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // 设置窗口大小
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        // 设置标题栏
        this.Title = "RecordTime - 时间追踪工具";
    }
}
