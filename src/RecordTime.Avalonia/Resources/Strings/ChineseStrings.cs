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

    // Category Distribution and Top 10 section
    public string CategoryDistribution => "应用类型分布";
    public string Top10AppsTitle => "Top 10 应用";
    public string NoDataAvailable => "暂无数据";

    // ============================================
    // Report strings
    // ============================================
    public string ReportGenerationTitle => "报告生成";
    public string SelectDateRange => "📅 选择日期范围";
    public string LastWeek => "最近一周";
    public string LastMonth => "最近一月";
    public string LastThreeMonths => "最近三月";
    public string CustomDateRange => "自定义";
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

    // Settings About section
    public string SettingsDescription => "RecordTime 是一款 Windows 桌面时间追踪工具";
    public string SettingsFeatures => "功能:实时监控、数据统计、HTML 报告、AI 分析";
    public string SettingsCopyright => "© 2025 RecordTime. All rights reserved.";
    public string LogsDescription => "日志文件可用于故障排除和诊断";
    public string TotalRecordsFormat => "共 {0} 条记录";

    // ============================================
    // About Page Feature Lists
    // ============================================
    // Core Features
    public string CoreFeature1 => "🎯 智能活动检测 - 自动识别视频、游戏、主动输入、被动浏览等活动类型";
    public string CoreFeature2 => "📊 可视化统计分析 - 实时图表展示应用使用分布和时间趋势";
    public string CoreFeature3 => "🤖 AI 智能洞察 - 可选的 AI 分析提供个性化时间管理建议";
    public string CoreFeature4 => "📈 专业报告生成 - 支持导出详细的 HTML 格式报告,可在浏览器查看";

    // Security Features
    public string SecurityFeature1 => "🔒 本地优先存储 - 所有数据存储在您的设备上,永不上传云端";
    public string SecurityFeature2 => "🔐 隐私数据加密 - 窗口标题使用 SHA256 哈希加密存储";
    public string SecurityFeature3 => "💾 心跳机制保护 - 每30秒自动保存,防止崩溃导致数据丢失";
    public string SecurityFeature4 => "🛡️ 自动修复机制 - 启动时检测并修复异常会话,确保数据完整性";

    // UX Features
    public string UXFeature1 => "🚀 轻量级运行 - 低资源占用,后台静默监控不影响正常使用";
    public string UXFeature2 => "🎨 现代化界面 - Apple 风格设计,简洁美观的用户界面";
    public string UXFeature3 => "⚡ 高性能优化 - 500ms 窗口轮询,准确捕捉应用切换";
    public string UXFeature4 => "🔧 灵活配置 - 支持多 AI 配置、自定义命名、状态持久化";

    // ============================================
    // Error Dialog
    // ============================================
    public string Error => "错误";
    public string ViewLogs => "查看日志";

    // ============================================
    // Main Window / Dashboard
    // ============================================
    public string MonitoringNotStarted => "监控未启动";
    public string ShowHistoricalData => "显示历史数据";
    public string RealTimeData => "实时数据";
    public string StartMonitoringFailed => "启动监控失败";
    public string NoData => "暂无数据";
    public string DateFormatPattern => "yyyy年MM月dd日";
    public string UsageCountSuffix => " 次";
    public string StopFailedPrefix => "停止失败: ";

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

    // ============================================
    // AppStats ViewModel
    // ============================================
    public string AllCategories => "全部分类";
    public string LoadDataFailed => "加载数据失败";

    // ============================================
    // Report ViewModel
    // ============================================
    public string ReportReadyStatus => "准备生成报告";
    public string AINotConfigured => "AI 分析未配置";
    public string AINotConfiguredNeedKey => "AI 分析未配置(需要 API Key)";
    public string AIConfigured => "AI 已配置";
    public string DefaultConfigName => "OpenAI 官方";
    public string NoDataInRange => "所选时间范围内没有数据";
    public string RecordsAndApps => "共有 {0} 条记录,涉及 {1} 个应用";
    public string GeneratingReport => "正在生成报告...";
    public string PleaseWait => "请稍候,这可能需要几秒钟";
    public string ReportSuccessWithAI => "✓ 报告生成成功(含 AI 分析)";
    public string ProductivityScore => "效率评分";
    public string FileLabel => "文件";
    public string FileNameLabel => "文件名";
    public string GenerateFailed => "✗ 生成失败";
    public string PerformingAIAnalysis => "正在进行 AI 分析...";
    public string PleaseEnterAPIKey => "⚠️ 请先输入 API Key";
    public string TestingConnection => "🔄 正在测试连接...";
    public string AIConnectionSuccess => "✅ AI 连接成功!配置已验证";
    public string ConnectionFailed => "❌ 连接失败";
    public string TestError => "❌ 测试出错";
    public string ConfigNameExists => "❌ 配置名称 '{0}' 已存在或无效";
    public string ConfigSaved => "✅ 配置已保存";
    public string SaveFailed => "❌ 保存失败";
    public string CannotDeleteLastConfig => "✗ 无法删除最后一个配置";
    public string ConfigDeleted => "✓ 配置已删除";
    public string DeleteFailed => "✗ 删除失败";
    public string NewConfigPattern => "配置 {0}";
    public string NewConfigCreated => "✓ 新配置已创建";
    public string CreateFailed => "✗ 创建失败";
    public string RenameNeedsDialog => "重命名功能需要对话框支持";
    public string RenameFailed => "✗ 重命名失败";
    public string ReportFileNotExists => "报告文件不存在";
    public string ReportOpenedInBrowser => "已在浏览器中打开报告";
    public string OpenFailed => "打开失败";
    public string ReportFolderNotExists => "报告文件夹不存在";
    public string ReportFolderOpened => "已打开报告文件夹";

    // Chart strings
    public string WeeklyTrendChartTitle => "7天使用趋势";
    public string ActivityDistributionChartTitle => "活动类型分布";
    public string DailyUsageSeriesName => "每日使用时长";
    public string HoursAxisLabel => "小时";

    // Activity type names
    public string ActivityTypeVideo => "视频娱乐";
    public string ActivityTypeGaming => "游戏";
    public string ActivityTypeActiveTyping => "主动输入";
    public string ActivityTypePassiveBrowsing => "被动浏览";
    public string ActivityTypeIdle => "空闲";

    // ============================================
    // Settings ViewModel
    // ============================================
    public string SettingsLoadSuccess => "设置加载成功";
    public string SettingsLoadFailed => "加载设置失败";
    public string LanguageSwitchedToChinese => "语言已切换为简体中文";
    public string LanguageSwitchedToEnglish => "Language switched to English";
    public string AutoStartEnabled => "已启用开机自启动";
    public string AutoStartDisabled => "已禁用开机自启动";
    public string SettingsSaved => "设置已保存";
    public string IdleTimeoutSet => "空闲超时已设置为 {0} 分钟";
    public string DatabaseFolderOpened => "已打开数据库文件夹";
    public string BackingUpDatabase => "正在备份数据库...";
    public string DatabaseFileNotExists => "数据库文件不存在";
    public string BackupSuccess => "备份成功";
    public string BackupFailed => "备份失败";
    public string ClearingOldData => "正在清除旧数据...";
    public string OldDataCleared => "已清除 {0} 条旧数据";
    public string ClearFailed => "清除失败";
    public string LogsFolderOpened => "已打开日志文件夹";

    // ============================================
    // Time Budget (Phase 4)
    // ============================================
    public string NavigationTimeBudget => "🎯 时间目标";
    public string TimeBudgetTitle => "时间目标管理";
    public string RefreshButton => "刷新";
    public string AddBudgetButton => "添加目标";

    // AI Suggestions
    public string AISuggestionsTitle => "🤖 智能建议";
    public string AISuggestionsDesc => "基于您的使用习惯,我们为您推荐以下时间目标";
    public string SuggestedTimeLabel => "建议时长: ";
    public string AcceptButton => "接受";
    public string IgnoreButton => "忽略";
    public string NoSuggestionsTitle => "生成个性化建议";
    public string NoSuggestionsDesc => "分析您的历史使用数据,为您推荐合适的时间目标";
    public string GenerateSuggestionsButton => "生成建议";

    // Budget List
    public string MyBudgetsTitle => "我的时间目标";
    public string NoBudgetsTitle => "还没有设置时间目标";
    public string NoBudgetsDesc => "点击上方\"添加目标\"按钮创建您的第一个时间目标";
    public string TargetLabel => "目标: ";
    public string ReminderAtLabel => "提醒于 ";
    public string DisabledLabel => "已禁用";
    public string EditButton => "编辑";
    public string EnableButton => "启用";
    public string DisableButton => "禁用";
    public string DeleteButton => "删除";

    // Add/Edit Form
    public string AddBudgetTitle => "添加时间目标";
    public string EditBudgetTitle => "编辑时间目标";
    public string BudgetNameLabel => "目标名称";
    public string BudgetNamePlaceholder => "例如: 每日编程时间";
    public string BudgetTypeLabel => "目标类型";
    public string MaximumBudget => "上限目标";
    public string MinimumBudget => "下限目标";
    public string BudgetTypeMaxDesc => "控制某应用或分类的使用时间不超过设定值";
    public string BudgetTypeMinDesc => "确保某应用或分类的使用时间不低于设定值";
    public string TargetTypeLabel => "应用于";
    public string TargetTypeApp => "单个应用";
    public string TargetTypeCategory => "应用分类";
    public string SelectAppLabel => "选择应用";
    public string SelectCategoryLabel => "选择分类";
    public string TargetDurationLabel => "目标时长";
    public string HoursLabel => "小时";
    public string MinutesLabel => "分钟";
    public string ReminderEnabledLabel => "启用提醒";
    public string ReminderEnabledDesc => "当达到目标百分比时发送通知提醒";
    public string ReminderThresholdLabel => "提醒阈值: ";
    public string CancelButton => "取消";
    public string SaveButton => "保存";
}
