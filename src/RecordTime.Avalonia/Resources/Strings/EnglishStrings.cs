namespace RecordTime.Avalonia.Resources.Strings;

/// <summary>
/// English Language Pack
/// </summary>
public class EnglishStrings : IStringProvider
{
    public string AppName => "RecordTime";
    public string Settings => "Settings";
    public string Dashboard => "Dashboard";
    public string Reports => "Reports";
    public string Statistics => "Statistics";
    public string About => "About";

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

    public string Apply => "Apply";
    public string Cancel => "Cancel";
    public string OK => "OK";
    public string Save => "Save";
    public string Delete => "Delete";
    public string Add => "Add";
    public string Edit => "Edit";
    public string Export => "Export";
    public string Import => "Import";

    public string Ready => "Ready";
    public string Success => "Success";
    public string Failed => "Failed";
    public string Confirm => "Confirm";
    public string ConfirmDelete => "Are you sure you want to delete? This action cannot be undone.";
}
