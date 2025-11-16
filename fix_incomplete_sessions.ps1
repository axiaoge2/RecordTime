# PowerShell 脚本: 修复数据库中 EndTime 为 null 的异常会话
# 用途: 清理应用异常退出时未正确结束的会话记录

$dbPath = "$env:LOCALAPPDATA\RecordTime\recordtime.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "❌ 数据库文件不存在: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "📂 数据库路径: $dbPath" -ForegroundColor Cyan
Write-Host ""

# 检查是否有 sqlite3 命令
$sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue

if (-not $sqlite3) {
    Write-Host "❌ 未找到 sqlite3 命令,请先安装 SQLite" -ForegroundColor Red
    Write-Host "   下载地址: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    exit 1
}

# 1. 查询异常会话数量
Write-Host "🔍 正在查询异常会话..." -ForegroundColor Yellow
$incompleteCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL;"
Write-Host "   发现 $incompleteCount 条未结束的会话" -ForegroundColor Cyan
Write-Host ""

if ($incompleteCount -eq 0) {
    Write-Host "✅ 数据库中没有异常会话,无需清理" -ForegroundColor Green
    exit 0
}

# 2. 显示异常会话详情
Write-Host "📋 异常会话详情:" -ForegroundColor Yellow
& sqlite3 $dbPath -header -column @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime,
    CAST((julianday('now') - julianday(StartTime)) * 24 AS INTEGER) as HoursSinceStart
FROM Sessions
WHERE EndTime IS NULL
ORDER BY StartTime DESC
LIMIT 10;
"@
Write-Host ""

# 3. 询问用户是否修复
$response = Read-Host "是否修复这些异常会话? (Y/N)"
if ($response -ne 'Y' -and $response -ne 'y') {
    Write-Host "⏹️ 已取消操作" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🔧 正在修复异常会话..." -ForegroundColor Yellow

# 4. 备份数据库
$backupPath = "$env:LOCALAPPDATA\RecordTime\recordtime_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
Copy-Item $dbPath $backupPath
Write-Host "   ✅ 已创建备份: $backupPath" -ForegroundColor Green

# 5. 修复策略:将 EndTime 设置为 StartTime + 5分钟 (合理的默认会话时长)
$updateSql = @"
UPDATE Sessions
SET EndTime = datetime(StartTime, '+5 minutes'),
    DurationSeconds = 300
WHERE EndTime IS NULL;
"@

& sqlite3 $dbPath $updateSql

# 6. 验证修复结果
$remainingCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL;"

Write-Host ""
if ($remainingCount -eq 0) {
    Write-Host "✅ 修复完成! 已处理 $incompleteCount 条异常会话" -ForegroundColor Green
    Write-Host "   所有会话的 EndTime 已设置为 StartTime + 5分钟" -ForegroundColor Cyan
} else {
    Write-Host "⚠️ 修复部分完成,仍有 $remainingCount 条异常会话" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "💡 提示: 如果需要恢复,请使用备份文件:" -ForegroundColor Cyan
Write-Host "   $backupPath" -ForegroundColor Gray
