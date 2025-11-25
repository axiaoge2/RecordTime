using Avalonia.Controls;
using Avalonia.Input;

namespace RecordTime.Avalonia.Views;

public partial class ReportView : UserControl
{
    public ReportView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 确保图表不拦截滚轮事件，让外层 ScrollViewer 可以正常滚动
    /// </summary>
    private void Chart_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // 不处理事件，让其继续冒泡到 ScrollViewer
        e.Handled = false;
    }
}
