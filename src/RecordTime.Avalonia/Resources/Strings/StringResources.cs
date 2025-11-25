using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RecordTime.Avalonia.Resources.Strings;

/// <summary>
/// 字符串资源管理器 (支持动态切换语言)
/// </summary>
public class StringResources : INotifyPropertyChanged
{
    private static StringResources? _current;
    private IStringProvider _provider;

    public static StringResources Current => _current ??= new StringResources();

    private StringResources()
    {
        // 默认使用中文
        _provider = new ChineseStrings();
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void SwitchLanguage(string languageCode)
    {
        _provider = languageCode.ToLower() switch
        {
            "zh-cn" or "zh" => new ChineseStrings(),
            "en-us" or "en" => new EnglishStrings(),
            _ => new ChineseStrings()
        };

        // 通知所有属性变更
        OnPropertyChanged(string.Empty);
    }

    // ============================================
    // Common / Navigation
    // ============================================
    public string AppName => _provider.AppName;
    public string AppTitle => _provider.AppTitle;
    public string Settings => _provider.Settings;
    public string Dashboard => _provider.Dashboard;
    public string Reports => _provider.Reports;
    public string Statistics => _provider.Statistics;
    public string About => _provider.About;

    // Navigation with icons
    public string NavigationDashboard => _provider.NavigationDashboard;
    public string NavigationAppStats => _provider.NavigationAppStats;
    public string NavigationReports => _provider.NavigationReports;
    public string NavigationSettings => _provider.NavigationSettings;
    public string NavigationAbout => _provider.NavigationAbout;

    // ============================================
    // Dashboard strings
    // ============================================
    public string DashboardTitle => _provider.DashboardTitle;
    public string PreviousDayButton => _provider.PreviousDayButton;
    public string NextDayButton => _provider.NextDayButton;
    public string TodayButton => _provider.TodayButton;
    public string TotalActivityTime => _provider.TotalActivityTime;
    public string UpdatedAt => _provider.UpdatedAt;
    public string SessionCount => _provider.SessionCount;
    public string AppTypeDistribution => _provider.AppTypeDistribution;
    public string MonitoringStatus => _provider.MonitoringStatus;
    public string MonitoringRunning => _provider.MonitoringRunning;
    public string MonitoringStopped => _provider.MonitoringStopped;
    public string StartMonitoring => _provider.StartMonitoring;
    public string StopMonitoring => _provider.StopMonitoring;
    public string TopAppsTitle => _provider.TopAppsTitle;
    public string CurrentDisplaying => _provider.CurrentDisplaying;

    // Empty state
    public string EmptyStateTitle => _provider.EmptyStateTitle;
    public string EmptyStateDescription => _provider.EmptyStateDescription;
    public string EmptyStateFeature1 => _provider.EmptyStateFeature1;
    public string EmptyStateFeature2 => _provider.EmptyStateFeature2;
    public string EmptyStateFeature3 => _provider.EmptyStateFeature3;
    public string EmptyStateHint => _provider.EmptyStateHint;

    // ============================================
    // AppStats strings
    // ============================================
    public string AppStatsTitle => _provider.AppStatsTitle;
    public string TimeRange => _provider.TimeRange;
    public string Today => _provider.Today;
    public string Yesterday => _provider.Yesterday;
    public string ThisWeek => _provider.ThisWeek;
    public string ThisMonth => _provider.ThisMonth;
    public string TotalUsageTime => _provider.TotalUsageTime;
    public string CurrentDisplayApps => _provider.CurrentDisplayApps;
    public string TotalAppsCount => _provider.TotalAppsCount;
    public string SearchAppPlaceholder => _provider.SearchAppPlaceholder;
    public string SelectCategory => _provider.SelectCategory;
    public string ClearFilter => _provider.ClearFilter;
    public string AppDetailList => _provider.AppDetailList;

    // Table headers
    public string ColumnAppName => _provider.ColumnAppName;
    public string ColumnCategory => _provider.ColumnCategory;
    public string ColumnPercentage => _provider.ColumnPercentage;
    public string ColumnUsageTime => _provider.ColumnUsageTime;
    public string ColumnUsageCount => _provider.ColumnUsageCount;
    public string ColumnLastUsed => _provider.ColumnLastUsed;

    public string NoAppsRecorded => _provider.NoAppsRecorded;

    // Category Distribution and Top 10 section
    public string CategoryDistribution => _provider.CategoryDistribution;
    public string Top10AppsTitle => _provider.Top10AppsTitle;
    public string NoDataAvailable => _provider.NoDataAvailable;

    // AppStats ViewModel
    public string AllCategories => _provider.AllCategories;
    public string LoadDataFailed => _provider.LoadDataFailed;

    // ============================================
    // Report strings
    // ============================================
    public string ReportGenerationTitle => _provider.ReportGenerationTitle;
    public string SelectDateRange => _provider.SelectDateRange;
    public string LastWeek => _provider.LastWeek;
    public string LastMonth => _provider.LastMonth;
    public string LastThreeMonths => _provider.LastThreeMonths;
    public string StartDate => _provider.StartDate;
    public string EndDate => _provider.EndDate;
    public string ReportPreview => _provider.ReportPreview;
    public string StatisticDays => _provider.StatisticDays;
    public string TotalSessions => _provider.TotalSessions;
    public string TotalApps => _provider.TotalApps;

    // AI Analysis
    public string AIAnalysisTitle => _provider.AIAnalysisTitle;
    public string AIEnable => _provider.AIEnable;
    public string AIDisable => _provider.AIDisable;
    public string ConfigureAIService => _provider.ConfigureAIService;
    public string ConfigurationProfile => _provider.ConfigurationProfile;
    public string ConfigurationProfileDesc => _provider.ConfigurationProfileDesc;
    public string AddNewConfig => _provider.AddNewConfig;
    public string SaveConfig => _provider.SaveConfig;
    public string DeleteConfig => _provider.DeleteConfig;
    public string APIKey => _provider.APIKey;
    public string APIKeyPlaceholder => _provider.APIKeyPlaceholder;
    public string Model => _provider.Model;
    public string ModelPlaceholder => _provider.ModelPlaceholder;
    public string APIAddress => _provider.APIAddress;
    public string APIAddressPlaceholder => _provider.APIAddressPlaceholder;
    public string TestConnection => _provider.TestConnection;
    public string PrivacyProtection => _provider.PrivacyProtection;
    public string PrivacyProtectionDesc => _provider.PrivacyProtectionDesc;

    // Report generation
    public string GenerateReportTitle => _provider.GenerateReportTitle;
    public string GenerateHTMLReport => _provider.GenerateHTMLReport;
    public string GenerateReportWithAI => _provider.GenerateReportWithAI;
    public string OpenReport => _provider.OpenReport;
    public string OpenReportsFolder => _provider.OpenReportsFolder;
    public string ReportInfoTitle => _provider.ReportInfoTitle;
    public string ReportInfoFeature1 => _provider.ReportInfoFeature1;
    public string ReportInfoFeature2 => _provider.ReportInfoFeature2;
    public string ReportInfoFeature3 => _provider.ReportInfoFeature3;
    public string ReportInfoFeature4 => _provider.ReportInfoFeature4;

    // Report ViewModel
    public string ReportReadyStatus => _provider.ReportReadyStatus;
    public string AINotConfigured => _provider.AINotConfigured;
    public string AINotConfiguredNeedKey => _provider.AINotConfiguredNeedKey;
    public string AIConfigured => _provider.AIConfigured;
    public string DefaultConfigName => _provider.DefaultConfigName;
    public string NoDataInRange => _provider.NoDataInRange;
    public string RecordsAndApps => _provider.RecordsAndApps;
    public string GeneratingReport => _provider.GeneratingReport;
    public string PleaseWait => _provider.PleaseWait;
    public string ReportSuccessWithAI => _provider.ReportSuccessWithAI;
    public string ProductivityScore => _provider.ProductivityScore;
    public string FileLabel => _provider.FileLabel;
    public string FileNameLabel => _provider.FileNameLabel;
    public string GenerateFailed => _provider.GenerateFailed;
    public string PerformingAIAnalysis => _provider.PerformingAIAnalysis;
    public string PleaseEnterAPIKey => _provider.PleaseEnterAPIKey;
    public string TestingConnection => _provider.TestingConnection;
    public string AIConnectionSuccess => _provider.AIConnectionSuccess;
    public string ConnectionFailed => _provider.ConnectionFailed;
    public string TestError => _provider.TestError;
    public string ConfigNameExists => _provider.ConfigNameExists;
    public string ConfigSaved => _provider.ConfigSaved;
    public string SaveFailed => _provider.SaveFailed;
    public string CannotDeleteLastConfig => _provider.CannotDeleteLastConfig;
    public string ConfigDeleted => _provider.ConfigDeleted;
    public string DeleteFailed => _provider.DeleteFailed;
    public string NewConfigPattern => _provider.NewConfigPattern;
    public string NewConfigCreated => _provider.NewConfigCreated;
    public string CreateFailed => _provider.CreateFailed;
    public string RenameNeedsDialog => _provider.RenameNeedsDialog;
    public string RenameFailed => _provider.RenameFailed;
    public string ReportFileNotExists => _provider.ReportFileNotExists;
    public string ReportOpenedInBrowser => _provider.ReportOpenedInBrowser;
    public string OpenFailed => _provider.OpenFailed;
    public string ReportFolderNotExists => _provider.ReportFolderNotExists;
    public string ReportFolderOpened => _provider.ReportFolderOpened;

    // Chart strings
    public string WeeklyTrendChartTitle => _provider.WeeklyTrendChartTitle;
    public string ActivityDistributionChartTitle => _provider.ActivityDistributionChartTitle;
    public string DailyUsageSeriesName => _provider.DailyUsageSeriesName;
    public string HoursAxisLabel => _provider.HoursAxisLabel;

    // Activity type names
    public string ActivityTypeVideo => _provider.ActivityTypeVideo;
    public string ActivityTypeGaming => _provider.ActivityTypeGaming;
    public string ActivityTypeActiveTyping => _provider.ActivityTypeActiveTyping;
    public string ActivityTypePassiveBrowsing => _provider.ActivityTypePassiveBrowsing;
    public string ActivityTypeIdle => _provider.ActivityTypeIdle;

    // ============================================
    // About strings
    // ============================================
    public string AboutTitle => _provider.AboutTitle;
    public string VersionPrefix => _provider.VersionPrefix;
    public string AppDescription => _provider.AppDescription;
    public string AppDescriptionTitle => _provider.AppDescriptionTitle;
    public string PrivacyPromise => _provider.PrivacyPromise;
    public string PrivacyPromiseDesc => _provider.PrivacyPromiseDesc;
    public string CoreFeatures => _provider.CoreFeatures;
    public string CoreFeaturesSubtitle => _provider.CoreFeaturesSubtitle;
    public string DataSecurity => _provider.DataSecurity;
    public string DataSecuritySubtitle => _provider.DataSecuritySubtitle;
    public string UserExperience => _provider.UserExperience;
    public string UserExperienceSubtitle => _provider.UserExperienceSubtitle;
    public string TechStack => _provider.TechStack;
    public string Links => _provider.Links;
    public string GitHubRepo => _provider.GitHubRepo;
    public string OpenSourceLicense => _provider.OpenSourceLicense;
    public string MadeWithLove => _provider.MadeWithLove;

    // ============================================
    // Settings page (existing)
    // ============================================
    public string GeneralSettings => _provider.GeneralSettings;
    public string MonitoringSettings => _provider.MonitoringSettings;
    public string PrivacySettings => _provider.PrivacySettings;
    public string DataManagement => _provider.DataManagement;
    public string AdvancedSettings => _provider.AdvancedSettings;

    public string AutoStart => _provider.AutoStart;
    public string AutoStartDesc => _provider.AutoStartDesc;
    public string MinimizeToTray => _provider.MinimizeToTray;
    public string MinimizeToTrayDesc => _provider.MinimizeToTrayDesc;
    public string ShowNotifications => _provider.ShowNotifications;
    public string ShowNotificationsDesc => _provider.ShowNotificationsDesc;
    public string Language => _provider.Language;
    public string LanguageDesc => _provider.LanguageDesc;

    public string IdleTimeout => _provider.IdleTimeout;
    public string IdleTimeoutDesc => _provider.IdleTimeoutDesc;
    public string RecordWindowTitles => _provider.RecordWindowTitles;
    public string RecordWindowTitlesDesc => _provider.RecordWindowTitlesDesc;
    public string ExcludedApps => _provider.ExcludedApps;
    public string ExcludedAppsDesc => _provider.ExcludedAppsDesc;

    public string DatabasePath => _provider.DatabasePath;
    public string DatabaseSize => _provider.DatabaseSize;
    public string TotalRecords => _provider.TotalRecords;
    public string OpenDatabaseFolder => _provider.OpenDatabaseFolder;
    public string BackupDatabase => _provider.BackupDatabase;
    public string ClearOldData => _provider.ClearOldData;
    public string ClearOldDataWarning => _provider.ClearOldDataWarning;

    public string OpenLogsFolder => _provider.OpenLogsFolder;
    public string Version => _provider.Version;

    // Settings About section
    public string SettingsDescription => _provider.SettingsDescription;
    public string SettingsFeatures => _provider.SettingsFeatures;
    public string SettingsCopyright => _provider.SettingsCopyright;
    public string LogsDescription => _provider.LogsDescription;
    public string TotalRecordsFormat => _provider.TotalRecordsFormat;

    // Settings ViewModel
    public string SettingsLoadSuccess => _provider.SettingsLoadSuccess;
    public string SettingsLoadFailed => _provider.SettingsLoadFailed;
    public string LanguageSwitchedToChinese => _provider.LanguageSwitchedToChinese;
    public string LanguageSwitchedToEnglish => _provider.LanguageSwitchedToEnglish;
    public string AutoStartEnabled => _provider.AutoStartEnabled;
    public string AutoStartDisabled => _provider.AutoStartDisabled;
    public string SettingsSaved => _provider.SettingsSaved;
    public string IdleTimeoutSet => _provider.IdleTimeoutSet;
    public string DatabaseFolderOpened => _provider.DatabaseFolderOpened;
    public string BackingUpDatabase => _provider.BackingUpDatabase;
    public string DatabaseFileNotExists => _provider.DatabaseFileNotExists;
    public string BackupSuccess => _provider.BackupSuccess;
    public string BackupFailed => _provider.BackupFailed;
    public string ClearingOldData => _provider.ClearingOldData;
    public string OldDataCleared => _provider.OldDataCleared;
    public string ClearFailed => _provider.ClearFailed;
    public string LogsFolderOpened => _provider.LogsFolderOpened;

    // About Page Feature Lists
    public string CoreFeature1 => _provider.CoreFeature1;
    public string CoreFeature2 => _provider.CoreFeature2;
    public string CoreFeature3 => _provider.CoreFeature3;
    public string CoreFeature4 => _provider.CoreFeature4;

    public string SecurityFeature1 => _provider.SecurityFeature1;
    public string SecurityFeature2 => _provider.SecurityFeature2;
    public string SecurityFeature3 => _provider.SecurityFeature3;
    public string SecurityFeature4 => _provider.SecurityFeature4;

    public string UXFeature1 => _provider.UXFeature1;
    public string UXFeature2 => _provider.UXFeature2;
    public string UXFeature3 => _provider.UXFeature3;
    public string UXFeature4 => _provider.UXFeature4;

    // ============================================
    // Common Buttons
    // ============================================
    public string Apply => _provider.Apply;
    public string Cancel => _provider.Cancel;
    public string OK => _provider.OK;
    public string Save => _provider.Save;
    public string Delete => _provider.Delete;
    public string Add => _provider.Add;
    public string Edit => _provider.Edit;
    public string Export => _provider.Export;
    public string Import => _provider.Import;

    // ============================================
    // Status Messages
    // ============================================
    public string Ready => _provider.Ready;
    public string Success => _provider.Success;
    public string Failed => _provider.Failed;
    public string Confirm => _provider.Confirm;
    public string ConfirmDelete => _provider.ConfirmDelete;

    // ============================================
    // Error Dialog
    // ============================================
    public string Error => _provider.Error;
    public string ViewLogs => _provider.ViewLogs;

    // ============================================
    // Main Window / Dashboard
    // ============================================
    public string MonitoringNotStarted => _provider.MonitoringNotStarted;
    public string ShowHistoricalData => _provider.ShowHistoricalData;
    public string RealTimeData => _provider.RealTimeData;
    public string StartMonitoringFailed => _provider.StartMonitoringFailed;
    public string NoData => _provider.NoData;
    public string DateFormatPattern => _provider.DateFormatPattern;
    public string UsageCountSuffix => _provider.UsageCountSuffix;
    public string StopFailedPrefix => _provider.StopFailedPrefix;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 字符串提供接口
/// </summary>
public interface IStringProvider
{
    // ============================================
    // Common / Navigation
    // ============================================
    string AppName { get; }
    string AppTitle { get; }
    string Settings { get; }
    string Dashboard { get; }
    string Reports { get; }
    string Statistics { get; }
    string About { get; }

    // Navigation with icons
    string NavigationDashboard { get; }
    string NavigationAppStats { get; }
    string NavigationReports { get; }
    string NavigationSettings { get; }
    string NavigationAbout { get; }

    // ============================================
    // Dashboard strings
    // ============================================
    string DashboardTitle { get; }
    string PreviousDayButton { get; }
    string NextDayButton { get; }
    string TodayButton { get; }
    string TotalActivityTime { get; }
    string UpdatedAt { get; }
    string SessionCount { get; }
    string AppTypeDistribution { get; }
    string MonitoringStatus { get; }
    string MonitoringRunning { get; }
    string MonitoringStopped { get; }
    string StartMonitoring { get; }
    string StopMonitoring { get; }
    string TopAppsTitle { get; }
    string CurrentDisplaying { get; }

    // Empty state
    string EmptyStateTitle { get; }
    string EmptyStateDescription { get; }
    string EmptyStateFeature1 { get; }
    string EmptyStateFeature2 { get; }
    string EmptyStateFeature3 { get; }
    string EmptyStateHint { get; }

    // ============================================
    // AppStats strings
    // ============================================
    string AppStatsTitle { get; }
    string TimeRange { get; }
    string Today { get; }
    string Yesterday { get; }
    string ThisWeek { get; }
    string ThisMonth { get; }
    string TotalUsageTime { get; }
    string CurrentDisplayApps { get; }
    string TotalAppsCount { get; }
    string SearchAppPlaceholder { get; }
    string SelectCategory { get; }
    string ClearFilter { get; }
    string AppDetailList { get; }

    // Table headers
    string ColumnAppName { get; }
    string ColumnCategory { get; }
    string ColumnPercentage { get; }
    string ColumnUsageTime { get; }
    string ColumnUsageCount { get; }
    string ColumnLastUsed { get; }

    string NoAppsRecorded { get; }

    // Category Distribution and Top 10 section
    string CategoryDistribution { get; }
    string Top10AppsTitle { get; }
    string NoDataAvailable { get; }

    // AppStats ViewModel
    string AllCategories { get; }
    string LoadDataFailed { get; }

    // ============================================
    // Report strings
    // ============================================
    string ReportGenerationTitle { get; }
    string SelectDateRange { get; }
    string LastWeek { get; }
    string LastMonth { get; }
    string LastThreeMonths { get; }
    string StartDate { get; }
    string EndDate { get; }
    string ReportPreview { get; }
    string StatisticDays { get; }
    string TotalSessions { get; }
    string TotalApps { get; }

    // AI Analysis
    string AIAnalysisTitle { get; }
    string AIEnable { get; }
    string AIDisable { get; }
    string ConfigureAIService { get; }
    string ConfigurationProfile { get; }
    string ConfigurationProfileDesc { get; }
    string AddNewConfig { get; }
    string SaveConfig { get; }
    string DeleteConfig { get; }
    string APIKey { get; }
    string APIKeyPlaceholder { get; }
    string Model { get; }
    string ModelPlaceholder { get; }
    string APIAddress { get; }
    string APIAddressPlaceholder { get; }
    string TestConnection { get; }
    string PrivacyProtection { get; }
    string PrivacyProtectionDesc { get; }

    // Report generation
    string GenerateReportTitle { get; }
    string GenerateHTMLReport { get; }
    string GenerateReportWithAI { get; }
    string OpenReport { get; }
    string OpenReportsFolder { get; }
    string ReportInfoTitle { get; }
    string ReportInfoFeature1 { get; }
    string ReportInfoFeature2 { get; }
    string ReportInfoFeature3 { get; }
    string ReportInfoFeature4 { get; }

    // Report ViewModel
    string ReportReadyStatus { get; }
    string AINotConfigured { get; }
    string AINotConfiguredNeedKey { get; }
    string AIConfigured { get; }
    string DefaultConfigName { get; }
    string NoDataInRange { get; }
    string RecordsAndApps { get; }
    string GeneratingReport { get; }
    string PleaseWait { get; }
    string ReportSuccessWithAI { get; }
    string ProductivityScore { get; }
    string FileLabel { get; }
    string FileNameLabel { get; }
    string GenerateFailed { get; }
    string PerformingAIAnalysis { get; }
    string PleaseEnterAPIKey { get; }
    string TestingConnection { get; }
    string AIConnectionSuccess { get; }
    string ConnectionFailed { get; }
    string TestError { get; }
    string ConfigNameExists { get; }
    string ConfigSaved { get; }
    string SaveFailed { get; }
    string CannotDeleteLastConfig { get; }
    string ConfigDeleted { get; }
    string DeleteFailed { get; }
    string NewConfigPattern { get; }
    string NewConfigCreated { get; }
    string CreateFailed { get; }
    string RenameNeedsDialog { get; }
    string RenameFailed { get; }
    string ReportFileNotExists { get; }
    string ReportOpenedInBrowser { get; }
    string OpenFailed { get; }
    string ReportFolderNotExists { get; }
    string ReportFolderOpened { get; }

    // Chart strings
    string WeeklyTrendChartTitle { get; }
    string ActivityDistributionChartTitle { get; }
    string DailyUsageSeriesName { get; }
    string HoursAxisLabel { get; }

    // Activity type names
    string ActivityTypeVideo { get; }
    string ActivityTypeGaming { get; }
    string ActivityTypeActiveTyping { get; }
    string ActivityTypePassiveBrowsing { get; }
    string ActivityTypeIdle { get; }

    // ============================================
    // About strings
    // ============================================
    string AboutTitle { get; }
    string VersionPrefix { get; }
    string AppDescription { get; }
    string AppDescriptionTitle { get; }
    string PrivacyPromise { get; }
    string PrivacyPromiseDesc { get; }
    string CoreFeatures { get; }
    string CoreFeaturesSubtitle { get; }
    string DataSecurity { get; }
    string DataSecuritySubtitle { get; }
    string UserExperience { get; }
    string UserExperienceSubtitle { get; }
    string TechStack { get; }
    string Links { get; }
    string GitHubRepo { get; }
    string OpenSourceLicense { get; }
    string MadeWithLove { get; }

    // ============================================
    // Settings page (existing)
    // ============================================
    string GeneralSettings { get; }
    string MonitoringSettings { get; }
    string PrivacySettings { get; }
    string DataManagement { get; }
    string AdvancedSettings { get; }

    string AutoStart { get; }
    string AutoStartDesc { get; }
    string MinimizeToTray { get; }
    string MinimizeToTrayDesc { get; }
    string ShowNotifications { get; }
    string ShowNotificationsDesc { get; }
    string Language { get; }
    string LanguageDesc { get; }

    string IdleTimeout { get; }
    string IdleTimeoutDesc { get; }
    string RecordWindowTitles { get; }
    string RecordWindowTitlesDesc { get; }
    string ExcludedApps { get; }
    string ExcludedAppsDesc { get; }

    string DatabasePath { get; }
    string DatabaseSize { get; }
    string TotalRecords { get; }
    string OpenDatabaseFolder { get; }
    string BackupDatabase { get; }
    string ClearOldData { get; }
    string ClearOldDataWarning { get; }

    string OpenLogsFolder { get; }
    string Version { get; }

    // Settings About section
    string SettingsDescription { get; }
    string SettingsFeatures { get; }
    string SettingsCopyright { get; }
    string LogsDescription { get; }
    string TotalRecordsFormat { get; }

    // Settings ViewModel
    string SettingsLoadSuccess { get; }
    string SettingsLoadFailed { get; }
    string LanguageSwitchedToChinese { get; }
    string LanguageSwitchedToEnglish { get; }
    string AutoStartEnabled { get; }
    string AutoStartDisabled { get; }
    string SettingsSaved { get; }
    string IdleTimeoutSet { get; }
    string DatabaseFolderOpened { get; }
    string BackingUpDatabase { get; }
    string DatabaseFileNotExists { get; }
    string BackupSuccess { get; }
    string BackupFailed { get; }
    string ClearingOldData { get; }
    string OldDataCleared { get; }
    string ClearFailed { get; }
    string LogsFolderOpened { get; }

    // About Page Feature Lists
    string CoreFeature1 { get; }
    string CoreFeature2 { get; }
    string CoreFeature3 { get; }
    string CoreFeature4 { get; }

    string SecurityFeature1 { get; }
    string SecurityFeature2 { get; }
    string SecurityFeature3 { get; }
    string SecurityFeature4 { get; }

    string UXFeature1 { get; }
    string UXFeature2 { get; }
    string UXFeature3 { get; }
    string UXFeature4 { get; }

    // ============================================
    // Common Buttons
    // ============================================
    string Apply { get; }
    string Cancel { get; }
    string OK { get; }
    string Save { get; }
    string Delete { get; }
    string Add { get; }
    string Edit { get; }
    string Export { get; }
    string Import { get; }

    // ============================================
    // Status Messages
    // ============================================
    string Ready { get; }
    string Success { get; }
    string Failed { get; }
    string Confirm { get; }
    string ConfirmDelete { get; }

    // ============================================
    // Error Dialog
    // ============================================
    string Error { get; }
    string ViewLogs { get; }

    // ============================================
    // Main Window / Dashboard
    // ============================================
    string MonitoringNotStarted { get; }
    string ShowHistoricalData { get; }
    string RealTimeData { get; }
    string StartMonitoringFailed { get; }
    string NoData { get; }
    string DateFormatPattern { get; }
    string UsageCountSuffix { get; }
    string StopFailedPrefix { get; }
}
