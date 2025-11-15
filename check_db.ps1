# 查询今天的应用数量
$dbPath = "$env:LOCALAPPDATA\RecordTime\recordtime.db"
$today = (Get-Date).ToString("yyyy-MM-dd")

Write-Host "=== 查询 $today 的应用数据 ===" -ForegroundColor Green
Write-Host "数据库路径: $dbPath" -ForegroundColor Gray
Write-Host ""

# 使用 dotnet 工具查询
$query = @"
SELECT
    COALESCE(DisplayName, ProcessName) as AppName,
    COUNT(*) as SessionCount,
    SUM(DurationSeconds) as TotalSeconds
FROM Sessions
WHERE date(StartTime) = '$today'
GROUP BY COALESCE(DisplayName, ProcessName)
ORDER BY TotalSeconds DESC
LIMIT 15
"@

Write-Host "执行查询..." -ForegroundColor Yellow
dotnet run --project "src/RecordTime.Console/RecordTime.Console.csproj" verify
