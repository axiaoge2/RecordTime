using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Microsoft.Win32;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 图标提取服务接口
/// </summary>
public interface IIconExtractor
{
    /// <summary>
    /// 从进程名提取应用图标
    /// </summary>
    /// <param name="processName">进程名称（不含.exe）</param>
    /// <returns>Avalonia Bitmap，失败返回 null</returns>
    global::Avalonia.Media.Imaging.Bitmap? ExtractIcon(string processName);

    /// <summary>
    /// 从进程名提取应用图标，支持分类默认图标
    /// </summary>
    /// <param name="processName">进程名称（不含.exe）</param>
    /// <param name="category">应用分类（用于选择默认图标）</param>
    /// <returns>Avalonia Bitmap，失败返回分类默认图标</returns>
    global::Avalonia.Media.Imaging.Bitmap? ExtractIcon(string processName, string category);

    /// <summary>
    /// 清除缓存的图标
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Windows 应用图标提取器
/// 使用 Win32 API 从 .exe 文件中提取图标
/// </summary>
public class IconExtractor : IIconExtractor
{
    // 内存缓存：进程名 -> Avalonia Bitmap
    private readonly Dictionary<string, global::Avalonia.Media.Imaging.Bitmap?> _iconCache = new();
    private readonly object _cacheLock = new();

    // 磁盘缓存目录
    private readonly string _cacheDirectory;

    // 默认图标（当提取失败时使用）
    private global::Avalonia.Media.Imaging.Bitmap? _defaultIcon;

    // 分类默认图标缓存
    private readonly Dictionary<string, global::Avalonia.Media.Imaging.Bitmap?> _categoryIconCache = new();

    public IconExtractor()
    {
        // 设置缓存目录
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDirectory = Path.Combine(appDataPath, "RecordTime", "IconCache");

        // 确保缓存目录存在
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// 从进程名提取图标
    /// </summary>
    public global::Avalonia.Media.Imaging.Bitmap? ExtractIcon(string processName)
    {
        return ExtractIcon(processName, "其他");
    }

    /// <summary>
    /// 从进程名提取图标，支持分类默认图标
    /// </summary>
    public global::Avalonia.Media.Imaging.Bitmap? ExtractIcon(string processName, string category)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return GetCategoryDefaultIcon(category);

        // 1. 检查内存缓存
        lock (_cacheLock)
        {
            if (_iconCache.TryGetValue(processName, out var cachedIcon))
                return cachedIcon ?? GetCategoryDefaultIcon(category);
        }

        // 2. 检查磁盘缓存
        var diskCachePath = Path.Combine(_cacheDirectory, $"{processName}.png");
        if (File.Exists(diskCachePath))
        {
            try
            {
                var icon = new global::Avalonia.Media.Imaging.Bitmap(diskCachePath);
                lock (_cacheLock)
                {
                    _iconCache[processName] = icon;
                }
                return icon;
            }
            catch
            {
                // 磁盘缓存损坏，删除并继续提取
                File.Delete(diskCachePath);
            }
        }

        // 3. 尝试提取图标
        var extractedIcon = TryExtractFromExecutable(processName);

        // 4. 缓存结果
        lock (_cacheLock)
        {
            _iconCache[processName] = extractedIcon;
        }

        // 5. 保存到磁盘缓存
        if (extractedIcon != null)
        {
            try
            {
                extractedIcon.Save(diskCachePath);
            }
            catch
            {
                // 忽略保存失败
            }
        }

        return extractedIcon ?? GetCategoryDefaultIcon(category);
    }

    /// <summary>
    /// 尝试从可执行文件提取图标
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? TryExtractFromExecutable(string processName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[IconExtractor] 尝试提取图标: {processName}");

            // 0. 优先: 尝试从运行中的进程获取路径 (最可靠的方法)
            var runningProcessPath = TryGetPathFromRunningProcess(processName);
            if (!string.IsNullOrEmpty(runningProcessPath))
            {
                System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 从运行进程找到: {runningProcessPath}");
                var icon = ExtractIconFromFile(runningProcessPath);
                if (icon != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 成功从运行进程提取图标!");
                    return icon;
                }
            }

            // 构建搜索路径列表
            var searchPaths = new List<string>();

            // 1. 从 Windows Registry 查询安装路径
            var registryPaths = GetPathsFromRegistry(processName);
            searchPaths.AddRange(registryPaths);

            // 2. System32
            searchPaths.Add(Path.Combine(Environment.SystemDirectory, $"{processName}.exe"));

            // 3. Program Files 标准路径
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                processName, $"{processName}.exe"));
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                processName, $"{processName}.exe"));

            // 4. 当前用户的 AppData\Local
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                processName, $"{processName}.exe"));

            // 5. 常见应用的特殊路径
            AddCommonAppPaths(processName, searchPaths);

            // 6. Windows Apps (UWP应用)
            var windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsApps");
            if (Directory.Exists(windowsAppsPath))
            {
                try
                {
                    var matches = Directory.GetDirectories(windowsAppsPath, $"*{processName}*",
                        SearchOption.TopDirectoryOnly);
                    foreach (var dir in matches)
                    {
                        searchPaths.Add(Path.Combine(dir, $"{processName}.exe"));
                    }
                }
                catch
                {
                    // 访问 WindowsApps 可能需要权限，忽略错误
                }
            }

            // 尝试所有路径
            foreach (var path in searchPaths)
            {
                System.Diagnostics.Debug.WriteLine($"[IconExtractor]   检查路径: {path}");
                if (File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 找到文件,尝试提取图标...");
                    var icon = ExtractIconFromFile(path);
                    if (icon != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 成功提取图标!");
                        return icon;
                    }
                }
            }

            // 7. 尝试在 PATH 环境变量中查找
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                var paths = pathEnv.Split(';');
                foreach (var dir in paths)
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    var exePath = Path.Combine(dir.Trim(), $"{processName}.exe");
                    if (File.Exists(exePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 在PATH中找到: {exePath}");
                        var icon = ExtractIconFromFile(exePath);
                        if (icon != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✓ 成功提取图标!");
                            return icon;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✗ 未找到 {processName}.exe");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IconExtractor]   ✗ 提取失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 尝试从运行中的进程获取可执行文件路径
    /// </summary>
    private string? TryGetPathFromRunningProcess(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                try
                {
                    var mainModule = processes[0].MainModule;
                    if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
                    {
                        return mainModule.FileName;
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // 无权限访问进程模块 (常见于系统进程)
                }
                catch (InvalidOperationException)
                {
                    // 进程已退出
                }
                finally
                {
                    // 释放进程资源
                    foreach (var p in processes)
                    {
                        p.Dispose();
                    }
                }
            }
        }
        catch
        {
            // 忽略所有错误
        }

        return null;
    }

    /// <summary>
    /// 从 Windows Registry 查询应用安装路径
    /// </summary>
    private List<string> GetPathsFromRegistry(string processName)
    {
        var paths = new List<string>();

        try
        {
            // 搜索 Uninstall 注册表键
            var uninstallKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            var registryRoots = new[] { Registry.LocalMachine, Registry.CurrentUser };

            foreach (var root in registryRoots)
            {
                foreach (var uninstallKeyPath in uninstallKeys)
                {
                    try
                    {
                        using var uninstallKey = root.OpenSubKey(uninstallKeyPath);
                        if (uninstallKey == null) continue;

                        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                        {
                            try
                            {
                                using var appKey = uninstallKey.OpenSubKey(subKeyName);
                                if (appKey == null) continue;

                                var displayName = appKey.GetValue("DisplayName") as string;
                                var installLocation = appKey.GetValue("InstallLocation") as string;
                                var displayIcon = appKey.GetValue("DisplayIcon") as string;

                                // 检查是否匹配进程名
                                if (displayName != null &&
                                    (displayName.Contains(processName, StringComparison.OrdinalIgnoreCase) ||
                                     subKeyName.Contains(processName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // 尝试从 DisplayIcon 获取路径
                                    if (!string.IsNullOrEmpty(displayIcon))
                                    {
                                        // DisplayIcon 可能包含图标索引，如 "C:\path\app.exe,0"
                                        var iconPath = displayIcon.Split(',')[0].Trim('"');
                                        if (File.Exists(iconPath))
                                        {
                                            paths.Add(iconPath);
                                            System.Diagnostics.Debug.WriteLine($"[IconExtractor]   Registry DisplayIcon: {iconPath}");
                                        }
                                    }

                                    // 尝试从 InstallLocation 构建路径
                                    if (!string.IsNullOrEmpty(installLocation))
                                    {
                                        var exePath = Path.Combine(installLocation, $"{processName}.exe");
                                        paths.Add(exePath);
                                        System.Diagnostics.Debug.WriteLine($"[IconExtractor]   Registry InstallLocation: {exePath}");
                                    }
                                }
                            }
                            catch
                            {
                                // 忽略单个子键的错误
                            }
                        }
                    }
                    catch
                    {
                        // 忽略注册表访问错误
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IconExtractor]   Registry查询失败: {ex.Message}");
        }

        return paths;
    }

    /// <summary>
    /// 添加常见应用的特殊路径
    /// </summary>
    private void AddCommonAppPaths(string processName, List<string> searchPaths)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // 根据进程名添加已知路径
        switch (processName.ToLower())
        {
            case "chrome":
                searchPaths.Add(Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"));
                searchPaths.Add(Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"));
                break;

            case "msedge":
                searchPaths.Add(Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"));
                break;

            case "firefox":
                searchPaths.Add(Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Mozilla Firefox", "firefox.exe"));
                break;

            case "qq":
                // QQ 可能的路径
                searchPaths.Add(Path.Combine(programFilesX86, "Tencent", "QQ", "Bin", "QQ.exe"));
                searchPaths.Add(Path.Combine(programFiles, "Tencent", "QQ", "Bin", "QQ.exe"));
                searchPaths.Add("C:\\Program Files (x86)\\Tencent\\QQ\\Bin\\QQ.exe");
                searchPaths.Add("D:\\Program Files (x86)\\Tencent\\QQ\\Bin\\QQ.exe");
                searchPaths.Add("E:\\Tencent\\QQ\\Bin\\QQ.exe");
                searchPaths.Add("D:\\Tencent\\QQ\\Bin\\QQ.exe");
                searchPaths.Add("F:\\Tencent\\QQ\\Bin\\QQ.exe");
                // QQNT 新版本路径
                searchPaths.Add(Path.Combine(programFiles, "Tencent", "QQNT", "QQ.exe"));
                searchPaths.Add(Path.Combine(localAppData, "Programs", "Tencent", "QQNT", "QQ.exe"));
                break;

            case "wechat":
                searchPaths.Add(Path.Combine(programFiles, "Tencent", "WeChat", "WeChat.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Tencent", "WeChat", "WeChat.exe"));
                break;

            case "feishu":
            case "lark":
                searchPaths.Add(Path.Combine(localAppData, "ByteDance", "Feishu", "Feishu.exe"));
                searchPaths.Add(Path.Combine(programFiles, "Feishu", "Feishu.exe"));
                searchPaths.Add(Path.Combine(programFiles, "ByteDance", "Feishu", "Feishu.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "ByteDance", "Feishu", "Feishu.exe"));
                // 尝试不同盘符
                searchPaths.Add("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\ByteDance\\Feishu\\Feishu.exe");
                searchPaths.Add("D:\\Feishu\\Feishu.exe");
                searchPaths.Add("E:\\Feishu\\Feishu.exe");
                break;

            case "dingtalk":
                searchPaths.Add(Path.Combine(programFiles, "DingDing", "DingtalkLauncher.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "DingDing", "DingtalkLauncher.exe"));
                break;

            case "code":
                searchPaths.Add(Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe"));
                searchPaths.Add(Path.Combine(programFiles, "Microsoft VS Code", "Code.exe"));
                break;

            case "windowsterminal":
                // Windows Terminal 通常在 WindowsApps 中，但也可能在其他位置
                searchPaths.Add(Path.Combine(localAppData, "Microsoft", "WindowsApps", "wt.exe"));
                // Windows Terminal 的实际exe路径
                searchPaths.Add(Path.Combine(programFiles, "WindowsApps", "Microsoft.WindowsTerminal_1.18.3181.0_x64__8wekyb3d8bbwe", "WindowsTerminal.exe"));
                searchPaths.Add(Path.Combine(programFiles, "WindowsApps", "Microsoft.WindowsTerminal_1.19.10302.0_x64__8wekyb3d8bbwe", "WindowsTerminal.exe"));
                searchPaths.Add(Path.Combine(programFiles, "WindowsApps", "Microsoft.WindowsTerminal_1.20.11781.0_x64__8wekyb3d8bbwe", "WindowsTerminal.exe"));
                break;

            case "potplayer":
            case "potplayermini64":
            case "potplayermini":
                // PotPlayer 常见安装路径
                searchPaths.Add(Path.Combine(programFiles, "DAUM", "PotPlayer", "PotPlayerMini64.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "DAUM", "PotPlayer", "PotPlayerMini.exe"));
                searchPaths.Add(Path.Combine(programFiles, "PotPlayer", "PotPlayerMini64.exe"));
                searchPaths.Add("C:\\PotPlayer\\PotPlayerMini64.exe");
                searchPaths.Add("D:\\PotPlayer\\PotPlayerMini64.exe");
                searchPaths.Add("E:\\PotPlayer\\PotPlayerMini64.exe");
                break;

            case "baidunetdiskunite":
            case "baidunetdisk":
                // 百度网盘路径
                searchPaths.Add(Path.Combine(programFiles, "BaiduNetdisk", "BaiduNetdisk.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "BaiduNetdisk", "BaiduNetdisk.exe"));
                searchPaths.Add(Path.Combine(localAppData, "Programs", "BaiduNetdisk", "BaiduNetdisk.exe"));
                searchPaths.Add("C:\\Program Files\\BaiduNetdisk\\BaiduNetdisk.exe");
                searchPaths.Add("D:\\BaiduNetdisk\\BaiduNetdisk.exe");
                break;

            case "yy":
                // YY语音路径
                searchPaths.Add(Path.Combine(programFiles, "YY", "YY.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "YY", "YY.exe"));
                searchPaths.Add(Path.Combine(programFiles, "duowan", "YY", "YY.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "duowan", "YY", "YY.exe"));
                searchPaths.Add("C:\\Program Files (x86)\\YY\\YY.exe");
                searchPaths.Add("D:\\YY\\YY.exe");
                break;

            case "notepad++":
                searchPaths.Add(Path.Combine(programFiles, "Notepad++", "notepad++.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Notepad++", "notepad++.exe"));
                break;

            case "snipaste":
                // Snipaste 可能的路径
                searchPaths.Add(Path.Combine(programFiles, "Snipaste", "Snipaste.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Snipaste", "Snipaste.exe"));
                searchPaths.Add(Path.Combine(localAppData, "Programs", "Snipaste", "Snipaste.exe"));
                // 很多用户把 Snipaste 放在自定义位置
                searchPaths.Add("C:\\Snipaste\\Snipaste.exe");
                searchPaths.Add("D:\\Snipaste\\Snipaste.exe");
                searchPaths.Add("E:\\Snipaste\\Snipaste.exe");
                break;

            case "hiddify":
                // Hiddify 可能的路径
                searchPaths.Add(Path.Combine(localAppData, "Programs", "Hiddify", "Hiddify.exe"));
                searchPaths.Add(Path.Combine(programFiles, "Hiddify", "Hiddify.exe"));
                searchPaths.Add(Path.Combine(programFilesX86, "Hiddify", "Hiddify.exe"));
                break;

            case "recordtime.avalonia":
            case "recordtime":
                // RecordTime 自身的图标
                var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(currentExe))
                {
                    searchPaths.Add(currentExe);
                }
                break;
        }

        // 7. 对于所有应用，尝试在所有盘符下查找常见安装位置
        var driveLetters = new[] { "C", "D", "E", "F" };
        foreach (var drive in driveLetters)
        {
            searchPaths.Add($"{drive}:\\Program Files\\{processName}\\{processName}.exe");
            searchPaths.Add($"{drive}:\\Program Files (x86)\\{processName}\\{processName}.exe");
            searchPaths.Add($"{drive}:\\{processName}\\{processName}.exe");
        }
    }

    /// <summary>
    /// 从文件路径提取图标
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? ExtractIconFromFile(string filePath)
    {
        try
        {
            // 使用 System.Drawing.Icon 提取
            using var icon = Icon.ExtractAssociatedIcon(filePath);
            if (icon == null)
                return null;

            // 转换为 Bitmap (System.Drawing)
            using var bitmap = icon.ToBitmap();

            // 转换为 Avalonia Bitmap
            return ConvertToAvaloniaBitmap(bitmap);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 System.Drawing.Bitmap 转换为 Avalonia.Media.Imaging.Bitmap
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? ConvertToAvaloniaBitmap(System.Drawing.Bitmap drawingBitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            drawingBitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new global::Avalonia.Media.Imaging.Bitmap(memoryStream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取默认图标
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? GetDefaultIcon()
    {
        if (_defaultIcon != null)
            return _defaultIcon;

        try
        {
            // 创建一个简单的默认图标（32x32 蓝色方块）
            using var bitmap = new System.Drawing.Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);

            // 填充渐变色（使用应用主题色）
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, 32, 32),
                Color.FromArgb(102, 126, 234),  // #667eea
                Color.FromArgb(118, 75, 162),   // #764ba2
                45f
            );

            graphics.FillRectangle(brush, 0, 0, 32, 32);

            _defaultIcon = ConvertToAvaloniaBitmap(bitmap);
            return _defaultIcon;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 根据分类获取默认图标
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? GetCategoryDefaultIcon(string category)
    {
        if (string.IsNullOrEmpty(category))
            return GetDefaultIcon();

        // 检查缓存
        lock (_cacheLock)
        {
            if (_categoryIconCache.TryGetValue(category, out var cachedIcon))
                return cachedIcon;
        }

        try
        {
            // 根据分类选择颜色
            var (primaryColor, secondaryColor, symbol) = GetCategoryColors(category);

            // 创建图标
            using var bitmap = new System.Drawing.Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 填充渐变背景
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, 32, 32),
                primaryColor,
                secondaryColor,
                45f
            );
            graphics.FillRectangle(brush, 0, 0, 32, 32);

            // 添加符号文字
            using var font = new Font("Segoe UI", 16, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var textSize = graphics.MeasureString(symbol, font);
            var x = (32 - textSize.Width) / 2;
            var y = (32 - textSize.Height) / 2;
            graphics.DrawString(symbol, font, textBrush, x, y);

            var icon = ConvertToAvaloniaBitmap(bitmap);
            lock (_cacheLock)
            {
                _categoryIconCache[category] = icon;
            }
            return icon;
        }
        catch
        {
            return GetDefaultIcon();
        }
    }

    /// <summary>
    /// 根据分类获取颜色方案
    /// </summary>
    private (Color primary, Color secondary, string symbol) GetCategoryColors(string category)
    {
        return category switch
        {
            "开发工具" => (Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235), "<>"),     // 蓝色
            "办公软件" => (Color.FromArgb(34, 197, 94), Color.FromArgb(22, 163, 74), "W"),       // 绿色
            "视频娱乐" => (Color.FromArgb(239, 68, 68), Color.FromArgb(220, 38, 38), "▶"),      // 红色
            "社交通讯" => (Color.FromArgb(168, 85, 247), Color.FromArgb(147, 51, 234), "@"),    // 紫色
            "浏览器" => (Color.FromArgb(251, 146, 60), Color.FromArgb(249, 115, 22), "W"),       // 橙色
            "系统工具" => (Color.FromArgb(107, 114, 128), Color.FromArgb(75, 85, 99), "⚙"),    // 灰色
            "游戏" => (Color.FromArgb(236, 72, 153), Color.FromArgb(219, 39, 119), "♦"),        // 粉色
            "文件管理" => (Color.FromArgb(245, 158, 11), Color.FromArgb(217, 119, 6), "📁"),    // 黄色
            _ => (Color.FromArgb(102, 126, 234), Color.FromArgb(118, 75, 162), "?")              // 默认紫蓝色
        };
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _iconCache.Clear();
            _categoryIconCache.Clear();
        }

        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
            }
        }
        catch
        {
            // 忽略删除失败
        }
    }
}
