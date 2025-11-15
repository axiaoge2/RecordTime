using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;

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

    // 磁盘缓存目录
    private readonly string _cacheDirectory;

    // 默认图标（当提取失败时使用）
    private global::Avalonia.Media.Imaging.Bitmap? _defaultIcon;

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
        if (string.IsNullOrWhiteSpace(processName))
            return GetDefaultIcon();

        // 1. 检查内存缓存
        if (_iconCache.TryGetValue(processName, out var cachedIcon))
            return cachedIcon;

        // 2. 检查磁盘缓存
        var diskCachePath = Path.Combine(_cacheDirectory, $"{processName}.png");
        if (File.Exists(diskCachePath))
        {
            try
            {
                var icon = new global::Avalonia.Media.Imaging.Bitmap(diskCachePath);
                _iconCache[processName] = icon;
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
        _iconCache[processName] = extractedIcon;

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

        return extractedIcon ?? GetDefaultIcon();
    }

    /// <summary>
    /// 尝试从可执行文件提取图标
    /// </summary>
    private global::Avalonia.Media.Imaging.Bitmap? TryExtractFromExecutable(string processName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[IconExtractor] 尝试提取图标: {processName}");

            // 构建搜索路径列表
            var searchPaths = new List<string>();

            // 1. System32
            searchPaths.Add(Path.Combine(Environment.SystemDirectory, $"{processName}.exe"));

            // 2. Program Files 标准路径
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                processName, $"{processName}.exe"));
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                processName, $"{processName}.exe"));

            // 3. 当前用户的 AppData\Local
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                processName, $"{processName}.exe"));

            // 4. 常见应用的特殊路径
            AddCommonAppPaths(processName, searchPaths);

            // 5. Windows Apps (UWP应用)
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

            // 6. 尝试在 PATH 环境变量中查找
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
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _iconCache.Clear();

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
