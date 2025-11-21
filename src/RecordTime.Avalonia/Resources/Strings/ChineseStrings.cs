namespace RecordTime.Avalonia.Resources.Strings;

/// <summary>
/// 中文语言包
/// </summary>
public class ChineseStrings : IStringProvider
{
    public string AppName => "RecordTime";
    public string Settings => "设置";
    public string Dashboard => "仪表盘";
    public string Reports => "报告";
    public string Statistics => "统计";
    public string About => "关于";

    public string GeneralSettings => "常规设置";
    public string MonitoringSettings => "监控行为";
    public string PrivacySettings => "隐私与安全";
    public string DataManagement => "数据管理";
    public string AdvancedSettings => "高级设置";

    public string AutoStart => "开机自动启动";
    public string AutoStartDesc => "程序将在 Windows 启动时自动运行";
    public string MinimizeToTray => "最小化到系统托盘";
    public string MinimizeToTrayDesc => "关闭窗口时最小化到系统托盘而非退出";
    public string ShowNotifications => "显示通知";
    public string ShowNotificationsDesc => "显示活动追踪和统计通知";
    public string Language => "语言";
    public string LanguageDesc => "选择界面显示语言";

    public string IdleTimeout => "空闲超时时间";
    public string IdleTimeoutDesc => "无操作多久后视为空闲（分钟）";
    public string RecordWindowTitles => "记录窗口标题";
    public string RecordWindowTitlesDesc => "记录应用窗口标题（已加密哈希）";
    public string ExcludedApps => "排除监控的应用";
    public string ExcludedAppsDesc => "这些应用将不会被监控和记录";

    public string DatabasePath => "数据库路径";
    public string DatabaseSize => "数据库大小";
    public string TotalRecords => "共 {0} 条记录";
    public string OpenDatabaseFolder => "打开数据库文件夹";
    public string BackupDatabase => "备份数据库";
    public string ClearOldData => "清除旧数据";
    public string ClearOldDataWarning => "💡 提示：清除旧数据将删除 30 天前的所有记录";

    public string OpenLogsFolder => "打开日志文件夹";
    public string Version => "版本";

    public string Apply => "应用";
    public string Cancel => "取消";
    public string OK => "确定";
    public string Save => "保存";
    public string Delete => "删除";
    public string Add => "添加";
    public string Edit => "编辑";
    public string Export => "导出";
    public string Import => "导入";

    public string Ready => "就绪";
    public string Success => "成功";
    public string Failed => "失败";
    public string Confirm => "确认";
    public string ConfirmDelete => "确定要删除吗？此操作无法撤销。";
}
