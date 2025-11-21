namespace RecordTime.Avalonia.Resources.Strings;

/// <summary>
/// 中文语言包
/// </summary>
public class ChineseStrings : IStringProvider
{
    // ============================================
    // Common / Navigation
    // ============================================
    public string AppName => "RecordTime";
    public string AppTitle => "RecordTime - 时间追踪工具";
    public string Settings => "设置";
    public string Dashboard => "仪表盘";
    public string Reports => "报告";
    public string Statistics => "统计";
    public string About => "关于";

    // Navigation with icons
    public string NavigationDashboard => "📊 仪表盘";
    public string NavigationAppStats => "📱 应用统计";
    public string NavigationReports => "📈 报告";
    public string NavigationSettings => "⚙️ 设置";
    public string NavigationAbout => "ℹ️ 关于";

    // ============================================
    // Dashboard strings
    // ============================================
    public string DashboardTitle => "今日概览";
    public string PreviousDayButton => "← 前一天";
    public string NextDayButton => "后一天 →";
    public string TodayButton => "今天";
    public string TotalActivityTime => "总活动时长";
    public string UpdatedAt => "更新于";
    public string SessionCount => "会话数量";
    public string AppTypeDistribution => "📱 应用类型分布";
    public string MonitoringStatus => "监控状态";
    public string MonitoringRunning => "监控运行中";
    public string MonitoringStopped => "监控未启动";
    public string StartMonitoring => "启动监控";
    public string StopMonitoring => "停止监控";
    public string TopAppsTitle => "TOP 10 应用";
    public string CurrentDisplaying => "当前显示: {0} 个";

    // Empty state
    public string EmptyStateTitle => "开始追踪您的时间";
    public string EmptyStateDescription => "点击下方的\"启动监控\"按钮,RecordTime 将开始自动记录您的应用使用情况。";
    public string EmptyStateFeature1 => "自动追踪应用使用时长";
    public string EmptyStateFeature2 => "智能识别活动类型(视频、游戏等)";
    public string EmptyStateFeature3 => "数据本地存储,保护您的隐私";
    public string EmptyStateHint => "💡 提示:监控在后台运行,不会影响您的正常使用";

    // ============================================
    // AppStats strings
    // ============================================
    public string AppStatsTitle => "应用统计";
    public string TimeRange => "时间范围";
    public string Today => "今日";
    public string Yesterday => "昨日";
    public string ThisWeek => "本周";
    public string ThisMonth => "本月";
    public string TotalUsageTime => "总使用时长";
    public string CurrentDisplayApps => "当前显示应用";
    public string TotalAppsCount => "全部共 {0} 个";
    public string SearchAppPlaceholder => "🔍 搜索应用名称...";
    public string SelectCategory => "选择分类";
    public string ClearFilter => "清除筛选";
    public string AppDetailList => "应用详细列表";

    // Table headers
    public string ColumnAppName => "应用名称";
    public string ColumnCategory => "分类";
    public string ColumnPercentage => "占比";
    public string ColumnUsageTime => "使用时长";
    public string ColumnUsageCount => "使用次数";
    public string ColumnLastUsed => "最后使用";

    public string NoAppsRecorded => "今天还没有应用使用记录";

    // ============================================
    // Report strings
    // ============================================
    public string ReportGenerationTitle => "报告生成";
    public string SelectDateRange => "📅 选择日期范围";
    public string LastWeek => "最近一周";
    public string LastMonth => "最近一月";
    public string LastThreeMonths => "最近三月";
    public string StartDate => "开始日期";
    public string EndDate => "结束日期";
    public string ReportPreview => "📊 报告预览";
    public string StatisticDays => "统计天数";
    public string TotalSessions => "会话总数";
    public string TotalApps => "应用总数";

    // AI Analysis
    public string AIAnalysisTitle => "🤖 AI 智能分析";
    public string AIEnable => "开启";
    public string AIDisable => "关闭";
    public string ConfigureAIService => "配置 AI 服务";
    public string ConfigurationProfile => "配置方案";
    public string ConfigurationProfileDesc => "配置方案(可直接编辑名称)";
    public string AddNewConfig => "添加新配置";
    public string SaveConfig => "保存当前配置";
    public string DeleteConfig => "删除当前配置";
    public string APIKey => "API Key";
    public string APIKeyPlaceholder => "输入您的 OpenAI API Key";
    public string Model => "模型";
    public string ModelPlaceholder => "gpt-4o-mini";
    public string APIAddress => "API 地址(可选,支持兼容 API)";
    public string APIAddressPlaceholder => "https://api.openai.com/v1";
    public string TestConnection => "测试连接";
    public string PrivacyProtection => "🔒 隐私保护";
    public string PrivacyProtectionDesc => "仅发送统计数据(分类时长、活动类型分布等),不上传具体应用名称和窗口标题";

    // Report generation
    public string GenerateReportTitle => "📝 生成报告";
    public string GenerateHTMLReport => "🚀 生成 HTML 报告";
    public string GenerateReportWithAI => "🤖 生成报告(含 AI 分析)";
    public string OpenReport => "📄 打开报告";
    public string OpenReportsFolder => "📁 打开文件夹";
    public string ReportInfoTitle => "ℹ️ 报告说明";
    public string ReportInfoFeature1 => "包含详细的时间使用统计和可视化图表";
    public string ReportInfoFeature2 => "报告保存在应用程序目录下的 Reports 文件夹";
    public string ReportInfoFeature3 => "可在任何现代浏览器中打开查看";
    public string ReportInfoFeature4 => "包含应用使用统计、分类统计、时间线和 AI 分析建议";

    // ============================================
    // About strings
    // ============================================
    public string AboutTitle => "ℹ️ 关于 RecordTime";
    public string VersionPrefix => "版本";
    public string AppDescription => "RecordTime 是一款简洁、优雅的 Windows 桌面应用时间追踪工具。它能够自动记录您的应用使用情况,帮助您深入了解时间分配,提高工作效率。所有数据本地存储,充分保护您的隐私。";
    public string AppDescriptionTitle => "📝 应用简介";
    public string PrivacyPromise => "隐私承诺";
    public string PrivacyPromiseDesc => "RecordTime 采用 100% 本地存储架构,所有时间追踪数据仅保存在您的设备上。未经您授权,绝不上传任何数据至云端或第三方服务器。AI 分析功能为可选项,仅在您主动开启时才会发送匿名化的统计数据。";
    public string CoreFeatures => "✨ 核心功能";
    public string CoreFeaturesSubtitle => "深度挖掘时间数据价值,助力高效时间管理";
    public string DataSecurity => "🔐 数据安全";
    public string DataSecuritySubtitle => "隐私至上,数据完整性优先的设计理念";
    public string UserExperience => "🎨 用户体验";
    public string UserExperienceSubtitle => "极致性能与现代设计的完美融合";
    public string TechStack => "🛠️ 技术栈";
    public string Links => "🔗 链接";
    public string GitHubRepo => "📦 GitHub 仓库";
    public string OpenSourceLicense => "📄 开源许可";
    public string MadeWithLove => "Made with ❤️ for productivity";

    // ============================================
    // Settings page (existing)
    // ============================================
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
    public string IdleTimeoutDesc => "无操作多久后视为空闲(分钟)";
    public string RecordWindowTitles => "记录窗口标题";
    public string RecordWindowTitlesDesc => "记录应用窗口标题(已加密哈希)";
    public string ExcludedApps => "排除监控的应用";
    public string ExcludedAppsDesc => "这些应用将不会被监控和记录";

    public string DatabasePath => "数据库路径";
    public string DatabaseSize => "数据库大小";
    public string TotalRecords => "共 {0} 条记录";
    public string OpenDatabaseFolder => "打开数据库文件夹";
    public string BackupDatabase => "备份数据库";
    public string ClearOldData => "清除旧数据";
    public string ClearOldDataWarning => "💡 提示:清除旧数据将删除 30 天前的所有记录";

    public string OpenLogsFolder => "打开日志文件夹";
    public string Version => "版本";

    // ============================================
    // Common Buttons
    // ============================================
    public string Apply => "应用";
    public string Cancel => "取消";
    public string OK => "确定";
    public string Save => "保存";
    public string Delete => "删除";
    public string Add => "添加";
    public string Edit => "编辑";
    public string Export => "导出";
    public string Import => "导入";

    // ============================================
    // Status Messages
    // ============================================
    public string Ready => "就绪";
    public string Success => "成功";
    public string Failed => "失败";
    public string Confirm => "确认";
    public string ConfirmDelete => "确定要删除吗?此操作无法撤销。";
}
