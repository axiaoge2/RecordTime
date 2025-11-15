# 清空今天的使用时间数据
$dbPath = "$env:LOCALAPPDATA\RecordTime\recordtime.db"
$today = Get-Date -Format "yyyy-MM-dd"

Write-Host "=== 清空今天 ($today) 的使用时间数据 ===" -ForegroundColor Yellow
Write-Host "数据库路径: $dbPath" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $dbPath)) {
    Write-Host "❌ 数据库文件不存在: $dbPath" -ForegroundColor Red
    exit 1
}

# 加载 SQLite 程序集
Add-Type -Path "E:\recordtime\src\RecordTime.Data\bin\Debug\net7.0-windows10.0.19041.0\Microsoft.Data.Sqlite.dll"

try {
    # 连接数据库
    $connectionString = "Data Source=$dbPath"
    $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
    $connection.Open()

    # 查询今天的记录数
    $countCommand = $connection.CreateCommand()
    $countCommand.CommandText = @"
        SELECT COUNT(*)
        FROM Sessions
        WHERE date(StartTime) = date('now', 'localtime')
"@

    $count = $countCommand.ExecuteScalar()
    Write-Host "今天共有 $count 条会话记录" -ForegroundColor Cyan

    if ($count -eq 0) {
        Write-Host "✅ 今天没有数据，无需清空" -ForegroundColor Green
        $connection.Close()
        exit 0
    }

    # 确认删除
    Write-Host ""
    Write-Host "⚠️  即将删除 $count 条记录，此操作不可恢复！" -ForegroundColor Yellow
    $confirm = Read-Host "确认删除? (输入 YES 继续)"

    if ($confirm -ne "YES") {
        Write-Host "❌ 已取消操作" -ForegroundColor Red
        $connection.Close()
        exit 0
    }

    # 删除今天的记录
    Write-Host ""
    Write-Host "正在删除今天的记录..." -ForegroundColor Yellow

    $deleteCommand = $connection.CreateCommand()
    $deleteCommand.CommandText = @"
        DELETE FROM Sessions
        WHERE date(StartTime) = date('now', 'localtime')
"@

    $deleted = $deleteCommand.ExecuteNonQuery()

    Write-Host "✅ 成功删除 $deleted 条记录" -ForegroundColor Green

    # 关闭连接
    $connection.Close()

    Write-Host ""
    Write-Host "=== 数据清空完成 ===" -ForegroundColor Green
    Write-Host "提示: 请关闭并重新打开 RecordTime 应用以刷新界面" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ 删除失败: $_" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
    exit 1
}
