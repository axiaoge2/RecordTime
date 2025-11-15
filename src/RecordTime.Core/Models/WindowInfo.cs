namespace RecordTime.Core.Models;

/// <summary>
/// 窗口信息
/// </summary>
public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public bool IsFullscreen { get; set; }
    public bool IsFocused { get; set; }
}
