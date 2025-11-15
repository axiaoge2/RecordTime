# 验证RecordTime数据库
$dbPath = "$env:LOCALAPPDATA\RecordTime\recordtime.db"

Write-Host "数据库位置: $dbPath" -ForegroundColor Cyan

if (Test-Path $dbPath) {
    $fileInfo = Get-Item $dbPath
    Write-Host "✅ 数据库文件存在" -ForegroundColor Green
    Write-Host "   文件大小: $($fileInfo.Length) 字节" -ForegroundColor Yellow
    Write-Host "   最后修改: $($fileInfo.LastWriteTime)" -ForegroundColor Yellow
} else {
    Write-Host "❌ 数据库文件不存在" -ForegroundColor Red
}
