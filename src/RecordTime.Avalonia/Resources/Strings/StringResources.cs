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

    // 常规
    public string AppName => _provider.AppName;
    public string Settings => _provider.Settings;
    public string Dashboard => _provider.Dashboard;
    public string Reports => _provider.Reports;
    public string Statistics => _provider.Statistics;
    public string About => _provider.About;

    // 设置页面
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

    // 按钮
    public string Apply => _provider.Apply;
    public string Cancel => _provider.Cancel;
    public string OK => _provider.OK;
    public string Save => _provider.Save;
    public string Delete => _provider.Delete;
    public string Add => _provider.Add;
    public string Edit => _provider.Edit;
    public string Export => _provider.Export;
    public string Import => _provider.Import;

    // 消息
    public string Ready => _provider.Ready;
    public string Success => _provider.Success;
    public string Failed => _provider.Failed;
    public string Confirm => _provider.Confirm;
    public string ConfirmDelete => _provider.ConfirmDelete;

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
    string AppName { get; }
    string Settings { get; }
    string Dashboard { get; }
    string Reports { get; }
    string Statistics { get; }
    string About { get; }

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

    string Apply { get; }
    string Cancel { get; }
    string OK { get; }
    string Save { get; }
    string Delete { get; }
    string Add { get; }
    string Edit { get; }
    string Export { get; }
    string Import { get; }

    string Ready { get; }
    string Success { get; }
    string Failed { get; }
    string Confirm { get; }
    string ConfirmDelete { get; }
}
