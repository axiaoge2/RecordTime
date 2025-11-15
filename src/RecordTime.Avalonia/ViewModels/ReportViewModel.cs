using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Data;
using RecordTime.Data.Reports;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecordTime.Avalonia.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _startDateText = DateTime.Today.AddDays(-7).ToString("yyyy年MM月dd日");

    [ObservableProperty]
    private string _endDateText = DateTime.Today.ToString("yyyy年MM月dd日");

    [ObservableProperty]
    private string _statusText = "准备生成报告";

    [ObservableProperty]
    private string? _statusDetail;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string? _lastReportPath;

    [ObservableProperty]
    private int _totalDays = 0;

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private int _totalApps = 0;

    public ReportViewModel()
    {
        _ = LoadPreviewDataAsync();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        StartDateText = value.ToString("yyyy年MM月dd日");
        _ = LoadPreviewDataAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        EndDateText = value.ToString("yyyy年MM月dd日");
        _ = LoadPreviewDataAsync();
    }

    private async Task LoadPreviewDataAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var sessions = await dbContext.Sessions
                .Where(s => s.StartTime >= StartDate && s.StartTime < EndDate.AddDays(1))
                .ToListAsync();

            TotalDays = (int)(EndDate - StartDate).TotalDays + 1;
            TotalSessions = sessions.Count;
            TotalApps = sessions.Select(s => s.ProcessName).Distinct().Count();

            if (sessions.Count == 0)
            {
                StatusDetail = "所选时间范围内没有数据";
            }
            else
            {
                StatusDetail = $"共有 {TotalSessions} 条记录，涉及 {TotalApps} 个应用";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载预览数据失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            IsGenerating = true;
            StatusText = "正在生成报告...";
            StatusDetail = "请稍候，这可能需要几秒钟";

            // 创建报告生成器
            await using var dbContext = new RecordTimeDbContext();
            var reportGenerator = new HtmlReportGenerator(dbContext);

            // 生成报告
            var reportPath = await reportGenerator.GenerateReportAsync(StartDate, EndDate);

            LastReportPath = reportPath;
            StatusText = "✓ 报告生成成功";
            StatusDetail = $"文件名：{Path.GetFileName(reportPath)}";
        }
        catch (Exception ex)
        {
            StatusText = "✗ 生成失败";
            StatusDetail = ex.Message;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void OpenReport()
    {
        if (string.IsNullOrEmpty(LastReportPath) || !File.Exists(LastReportPath))
        {
            StatusText = "报告文件不存在";
            return;
        }

        try
        {
            // 在默认浏览器中打开 HTML 报告
            Process.Start(new ProcessStartInfo
            {
                FileName = LastReportPath,
                UseShellExecute = true
            });
            StatusText = "已在浏览器中打开报告";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenReportsFolder()
    {
        try
        {
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");

            if (!Directory.Exists(reportsFolder))
            {
                StatusText = "报告文件夹不存在";
                return;
            }

            // 在文件资源管理器中打开文件夹
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = reportsFolder,
                UseShellExecute = true
            });
            StatusText = "已打开报告文件夹";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void SetStartDateToday()
    {
        StartDate = DateTime.Today;
        StartDateText = StartDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetEndDateToday()
    {
        EndDate = DateTime.Today;
        EndDateText = EndDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetLastWeek()
    {
        StartDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        StartDateText = StartDate.ToString("yyyy年MM月dd日");
        EndDateText = EndDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetLastMonth()
    {
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
    }

    [RelayCommand]
    private void SetLastThreeMonths()
    {
        StartDate = DateTime.Today.AddMonths(-3);
        EndDate = DateTime.Today;
    }
}
