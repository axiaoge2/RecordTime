namespace RecordTime.Avalonia.Resources.Strings;

/// <summary>
/// English Language Pack
/// </summary>
public class EnglishStrings : IStringProvider
{
    // ============================================
    // Common / Navigation
    // ============================================
    public string AppName => "RecordTime";
    public string AppTitle => "RecordTime - Time Tracking Tool";
    public string Settings => "Settings";
    public string Dashboard => "Dashboard";
    public string Reports => "Reports";
    public string Statistics => "Statistics";
    public string About => "About";

    // Navigation with icons
    public string NavigationDashboard => "📊 Dashboard";
    public string NavigationAppStats => "📱 App Statistics";
    public string NavigationReports => "📈 Reports";
    public string NavigationSettings => "⚙️ Settings";
    public string NavigationAbout => "ℹ️ About";

    // ============================================
    // Dashboard strings
    // ============================================
    public string DashboardTitle => "Today's Overview";
    public string PreviousDayButton => "← Previous Day";
    public string NextDayButton => "Next Day →";
    public string TodayButton => "Today";
    public string TotalActivityTime => "Total Activity Time";
    public string UpdatedAt => "Updated at";
    public string SessionCount => "Session Count";
    public string AppTypeDistribution => "📱 App Type Distribution";
    public string MonitoringStatus => "Monitoring Status";
    public string MonitoringRunning => "Monitoring Running";
    public string MonitoringStopped => "Monitoring Stopped";
    public string StartMonitoring => "Start Monitoring";
    public string StopMonitoring => "Stop Monitoring";
    public string TopAppsTitle => "TOP 10 Apps";
    public string CurrentDisplaying => "Currently displaying: {0}";

    // Empty state
    public string EmptyStateTitle => "Start Tracking Your Time";
    public string EmptyStateDescription => "Click the \"Start Monitoring\" button below, and RecordTime will begin automatically recording your application usage.";
    public string EmptyStateFeature1 => "Automatically track application usage time";
    public string EmptyStateFeature2 => "Intelligently identify activity types (video, gaming, etc.)";
    public string EmptyStateFeature3 => "Data stored locally, protecting your privacy";
    public string EmptyStateHint => "💡 Tip: Monitoring runs in the background without affecting your normal usage";

    // ============================================
    // AppStats strings
    // ============================================
    public string AppStatsTitle => "Application Statistics";
    public string TimeRange => "Time Range";
    public string Today => "Today";
    public string Yesterday => "Yesterday";
    public string ThisWeek => "This Week";
    public string ThisMonth => "This Month";
    public string TotalUsageTime => "Total Usage Time";
    public string CurrentDisplayApps => "Currently Displayed Apps";
    public string TotalAppsCount => "Total: {0} apps";
    public string SearchAppPlaceholder => "🔍 Search application name...";
    public string SelectCategory => "Select Category";
    public string ClearFilter => "Clear Filter";
    public string AppDetailList => "Application Detail List";

    // Table headers
    public string ColumnAppName => "App Name";
    public string ColumnCategory => "Category";
    public string ColumnPercentage => "Percentage";
    public string ColumnUsageTime => "Usage Time";
    public string ColumnUsageCount => "Usage Count";
    public string ColumnLastUsed => "Last Used";

    public string NoAppsRecorded => "No application usage records today";

    // ============================================
    // Report strings
    // ============================================
    public string ReportGenerationTitle => "Report Generation";
    public string SelectDateRange => "📅 Select Date Range";
    public string LastWeek => "Last Week";
    public string LastMonth => "Last Month";
    public string LastThreeMonths => "Last Three Months";
    public string StartDate => "Start Date";
    public string EndDate => "End Date";
    public string ReportPreview => "📊 Report Preview";
    public string StatisticDays => "Statistics Days";
    public string TotalSessions => "Total Sessions";
    public string TotalApps => "Total Apps";

    // AI Analysis
    public string AIAnalysisTitle => "🤖 AI Smart Analysis";
    public string AIEnable => "Enable";
    public string AIDisable => "Disable";
    public string ConfigureAIService => "Configure AI Service";
    public string ConfigurationProfile => "Configuration Profile";
    public string ConfigurationProfileDesc => "Configuration profile (editable name)";
    public string AddNewConfig => "Add New Configuration";
    public string SaveConfig => "Save Current Configuration";
    public string DeleteConfig => "Delete Current Configuration";
    public string APIKey => "API Key";
    public string APIKeyPlaceholder => "Enter your OpenAI API Key";
    public string Model => "Model";
    public string ModelPlaceholder => "gpt-4o-mini";
    public string APIAddress => "API Address (optional, compatible API supported)";
    public string APIAddressPlaceholder => "https://api.openai.com/v1";
    public string TestConnection => "Test Connection";
    public string PrivacyProtection => "🔒 Privacy Protection";
    public string PrivacyProtectionDesc => "Only statistical data is sent (category duration, activity type distribution, etc.), specific application names and window titles are not uploaded";

    // Report generation
    public string GenerateReportTitle => "📝 Generate Report";
    public string GenerateHTMLReport => "🚀 Generate HTML Report";
    public string GenerateReportWithAI => "🤖 Generate Report (with AI Analysis)";
    public string OpenReport => "📄 Open Report";
    public string OpenReportsFolder => "📁 Open Folder";
    public string ReportInfoTitle => "ℹ️ Report Information";
    public string ReportInfoFeature1 => "Contains detailed time usage statistics and visualization charts";
    public string ReportInfoFeature2 => "Reports are saved in the Reports folder under the application directory";
    public string ReportInfoFeature3 => "Can be opened in any modern browser";
    public string ReportInfoFeature4 => "Includes application usage statistics, category statistics, timeline, and AI analysis recommendations";

    // ============================================
    // About strings
    // ============================================
    public string AboutTitle => "ℹ️ About RecordTime";
    public string VersionPrefix => "Version";
    public string AppDescription => "RecordTime is a simple and elegant Windows desktop application time tracking tool. It automatically records your application usage, helping you gain deeper insights into time allocation and improve work efficiency. All data is stored locally, fully protecting your privacy.";
    public string AppDescriptionTitle => "📝 Application Description";
    public string PrivacyPromise => "Privacy Promise";
    public string PrivacyPromiseDesc => "RecordTime uses a 100% local storage architecture, with all time tracking data saved only on your device. Without your authorization, no data will be uploaded to the cloud or third-party servers. The AI analysis feature is optional and will only send anonymized statistical data when you actively enable it.";
    public string CoreFeatures => "✨ Core Features";
    public string CoreFeaturesSubtitle => "Deeply mining time data value to assist efficient time management";
    public string DataSecurity => "🔐 Data Security";
    public string DataSecuritySubtitle => "Privacy-first, data integrity-prioritized design philosophy";
    public string UserExperience => "🎨 User Experience";
    public string UserExperienceSubtitle => "Perfect fusion of ultimate performance and modern design";
    public string TechStack => "🛠️ Technology Stack";
    public string Links => "🔗 Links";
    public string GitHubRepo => "📦 GitHub Repository";
    public string OpenSourceLicense => "📄 Open Source License";
    public string MadeWithLove => "Made with ❤️ for productivity";

    // ============================================
    // Settings page (existing)
    // ============================================
    public string GeneralSettings => "General";
    public string MonitoringSettings => "Monitoring";
    public string PrivacySettings => "Privacy & Security";
    public string DataManagement => "Data Management";
    public string AdvancedSettings => "Advanced";

    public string AutoStart => "Start with Windows";
    public string AutoStartDesc => "Launch the app automatically when Windows starts";
    public string MinimizeToTray => "Minimize to Tray";
    public string MinimizeToTrayDesc => "Minimize to system tray instead of closing";
    public string ShowNotifications => "Show Notifications";
    public string ShowNotificationsDesc => "Display activity tracking and statistics notifications";
    public string Language => "Language";
    public string LanguageDesc => "Select interface language";

    public string IdleTimeout => "Idle Timeout";
    public string IdleTimeoutDesc => "Minutes of inactivity before considered idle";
    public string RecordWindowTitles => "Record Window Titles";
    public string RecordWindowTitlesDesc => "Record application window titles (encrypted hash)";
    public string ExcludedApps => "Excluded Applications";
    public string ExcludedAppsDesc => "These apps will not be monitored or recorded";

    public string DatabasePath => "Database Path";
    public string DatabaseSize => "Database Size";
    public string TotalRecords => "{0} records total";
    public string OpenDatabaseFolder => "Open Database Folder";
    public string BackupDatabase => "Backup Database";
    public string ClearOldData => "Clear Old Data";
    public string ClearOldDataWarning => "💡 Note: This will delete all records older than 30 days";

    public string OpenLogsFolder => "Open Logs Folder";
    public string Version => "Version";

    // Settings About section
    public string SettingsDescription => "RecordTime is a Windows desktop time tracking tool";
    public string SettingsFeatures => "Features: Real-time monitoring, data statistics, HTML reports, AI analysis";
    public string SettingsCopyright => "© 2025 RecordTime. All rights reserved.";
    public string LogsDescription => "Log files can be used for troubleshooting and diagnostics";
    public string TotalRecordsFormat => "{0} records total";

    // ============================================
    // About Page Feature Lists
    // ============================================
    // Core Features
    public string CoreFeature1 => "🎯 Smart Activity Detection - Automatically identify video, gaming, active input, passive browsing and other activity types";
    public string CoreFeature2 => "📊 Visual Statistics Analysis - Real-time charts showing application usage distribution and time trends";
    public string CoreFeature3 => "🤖 AI Smart Insights - Optional AI analysis provides personalized time management recommendations";
    public string CoreFeature4 => "📈 Professional Report Generation - Support exporting detailed HTML format reports, viewable in browsers";

    // Security Features
    public string SecurityFeature1 => "🔒 Local-first Storage - All data stored on your device, never uploaded to cloud";
    public string SecurityFeature2 => "🔐 Privacy Data Encryption - Window titles stored using SHA256 hash encryption";
    public string SecurityFeature3 => "💾 Heartbeat Protection - Auto-save every 30 seconds, preventing data loss from crashes";
    public string SecurityFeature4 => "🛡️ Auto-repair Mechanism - Detects and repairs abnormal sessions on startup, ensuring data integrity";

    // UX Features
    public string UXFeature1 => "🚀 Lightweight Operation - Low resource usage, silent background monitoring without affecting normal use";
    public string UXFeature2 => "🎨 Modern Interface - Apple-style design, clean and beautiful user interface";
    public string UXFeature3 => "⚡ High Performance Optimization - 500ms window polling, accurately captures app switching";
    public string UXFeature4 => "🔧 Flexible Configuration - Support multiple AI configurations, custom naming, state persistence";

    // ============================================
    // Common Buttons
    // ============================================
    public string Apply => "Apply";
    public string Cancel => "Cancel";
    public string OK => "OK";
    public string Save => "Save";
    public string Delete => "Delete";
    public string Add => "Add";
    public string Edit => "Edit";
    public string Export => "Export";
    public string Import => "Import";

    // ============================================
    // Status Messages
    // ============================================
    public string Ready => "Ready";
    public string Success => "Success";
    public string Failed => "Failed";
    public string Confirm => "Confirm";
    public string ConfirmDelete => "Are you sure you want to delete? This action cannot be undone.";
}
