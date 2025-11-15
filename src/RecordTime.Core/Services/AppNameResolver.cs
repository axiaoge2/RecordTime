using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RecordTime.Core.Services;

/// <summary>
/// 应用名称解析器 - 将进程名转换为友好的显示名称
/// </summary>
public class AppNameResolver
{
    private static readonly Dictionary<string, string> _knownApps = new(StringComparer.OrdinalIgnoreCase)
    {
        // 浏览器
        ["chrome"] = "Google Chrome",
        ["msedge"] = "Microsoft Edge",
        ["MicrosoftEdge"] = "Microsoft Edge",
        ["firefox"] = "Mozilla Firefox",
        ["brave"] = "Brave Browser",
        ["opera"] = "Opera",
        ["iexplore"] = "Internet Explorer",

        // 开发工具
        ["devenv"] = "Visual Studio",
        ["Code"] = "Visual Studio Code",
        ["rider64"] = "JetBrains Rider",
        ["idea64"] = "IntelliJ IDEA",
        ["pycharm64"] = "PyCharm",
        ["webstorm64"] = "WebStorm",
        ["sublime_text"] = "Sublime Text",
        ["notepad++"] = "Notepad++",

        // 办公软件
        ["WINWORD"] = "Microsoft Word",
        ["EXCEL"] = "Microsoft Excel",
        ["POWERPNT"] = "Microsoft PowerPoint",
        ["OUTLOOK"] = "Microsoft Outlook",
        ["ONENOTE"] = "Microsoft OneNote",

        // 通讯工具
        ["WeChat"] = "微信",
        ["QQ"] = "QQ",
        ["DingTalk"] = "钉钉",
        ["Feishu"] = "飞书",
        ["OUTLOOK"] = "Outlook",
        ["slack"] = "Slack",
        ["discord"] = "Discord",
        ["Telegram"] = "Telegram",

        // 媒体播放器
        ["PotPlayerMini64"] = "PotPlayer",
        ["vlc"] = "VLC Media Player",
        ["wmplayer"] = "Windows Media Player",
        ["foobar2000"] = "Foobar2000",
        ["netease-cloud-music"] = "网易云音乐",
        ["cloudmusic"] = "网易云音乐",
        ["QQMusic"] = "QQ音乐",

        // 设计工具
        ["Photoshop"] = "Adobe Photoshop",
        ["Illustrator"] = "Adobe Illustrator",
        ["Premiere Pro"] = "Adobe Premiere Pro",
        ["AfterFX"] = "Adobe After Effects",
        ["Figma"] = "Figma",

        // 游戏平台
        ["Steam"] = "Steam",
        ["EpicGamesLauncher"] = "Epic Games",
        ["Uplay"] = "Ubisoft Connect",
        ["WeGamePlatform"] = "WeGame",

        // 其他常用应用
        ["explorer"] = "文件资源管理器",
        ["Taskmgr"] = "任务管理器",
        ["cmd"] = "命令提示符",
        ["powershell"] = "PowerShell",
        ["WindowsTerminal"] = "Windows Terminal",
        ["Everything"] = "Everything",
        ["Typora"] = "Typora",
        ["notion"] = "Notion",
        ["obsidian"] = "Obsidian",
    };

    /// <summary>
    /// 获取应用的友好显示名称
    /// </summary>
    public static string GetFriendlyName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return "未知应用";

        // 先查找已知应用映射
        if (_knownApps.TryGetValue(processName, out var friendlyName))
        {
            return friendlyName;
        }

        // 尝试从进程获取产品名称
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                var process = processes[0];
                var mainModule = process.MainModule;

                if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
                {
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(mainModule.FileName);

                    // 优先使用 ProductName
                    if (!string.IsNullOrEmpty(fileVersionInfo.ProductName))
                    {
                        return fileVersionInfo.ProductName;
                    }

                    // 其次使用 FileDescription
                    if (!string.IsNullOrEmpty(fileVersionInfo.FileDescription))
                    {
                        return fileVersionInfo.FileDescription;
                    }
                }
            }
        }
        catch
        {
            // 无法获取进程信息，继续使用后备方案
        }

        // 后备方案：美化进程名
        return BeautifyProcessName(processName);
    }

    /// <summary>
    /// 美化进程名（去掉扩展名，首字母大写）
    /// </summary>
    private static string BeautifyProcessName(string processName)
    {
        // 移除 .exe 扩展名
        if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            processName = processName[..^4];
        }

        // 如果包含数字版本号（如 rider64），尝试去掉
        if (processName.Length > 2 && char.IsDigit(processName[^1]) && char.IsDigit(processName[^2]))
        {
            processName = processName[..^2];
        }

        // 首字母大写
        if (processName.Length > 0)
        {
            return char.ToUpper(processName[0]) + processName[1..];
        }

        return processName;
    }

    /// <summary>
    /// 添加自定义应用名称映射
    /// </summary>
    public static void AddCustomMapping(string processName, string friendlyName)
    {
        _knownApps[processName] = friendlyName;
    }
}
