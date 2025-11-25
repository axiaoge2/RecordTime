# RecordTime 发布脚本
# 用于生成可发布的单文件可执行程序

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# 设置路径
$RootPath = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RootPath "src\RecordTime.Avalonia\RecordTime.Avalonia.csproj"
$OutputPath = Join-Path $RootPath "build\output"
$PublishPath = Join-Path $RootPath "build\publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RecordTime 发布脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "版本: $Version" -ForegroundColor Green
Write-Host "配置: $Configuration" -ForegroundColor Green
Write-Host ""

# 清理旧的发布文件
Write-Host "[1/3] 清理旧的发布文件..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
    Write-Host "  - 已清理: $OutputPath" -ForegroundColor Gray
}
if (Test-Path $PublishPath) {
    Remove-Item -Path $PublishPath -Recurse -Force
    Write-Host "  - 已清理: $PublishPath" -ForegroundColor Gray
}

# 创建输出目录
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null

# 还原依赖（为 win-x64 运行时还原）
Write-Host ""
Write-Host "[2/3] 还原 NuGet 依赖..." -ForegroundColor Yellow
dotnet restore $ProjectPath -r win-x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ 还原失败" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ 还原成功" -ForegroundColor Green

# 发布单文件版本
Write-Host ""
Write-Host "[3/3] 发布单文件可执行程序..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    -c $Configuration `
    -r win-x64 `
    --no-restore `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ 发布失败" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ 发布成功" -ForegroundColor Green

# 创建便携版压缩包
Write-Host ""
Write-Host "创建便携版压缩包..." -ForegroundColor Yellow
$ZipPath = Join-Path $PublishPath "RecordTime-v$Version-Portable.zip"
Compress-Archive -Path "$OutputPath\*" -DestinationPath $ZipPath -Force
Write-Host "  ✓ 便携版: $ZipPath" -ForegroundColor Green

# 显示文件大小
$ExeFile = Get-Item (Join-Path $OutputPath "RecordTime.exe")
$ZipFile = Get-Item $ZipPath
$ExeSize = [math]::Round($ExeFile.Length / 1MB, 2)
$ZipSize = [math]::Round($ZipFile.Length / 1MB, 2)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "发布完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "可执行文件: $($ExeFile.FullName)" -ForegroundColor White
Write-Host "  大小: $ExeSize MB" -ForegroundColor Gray
Write-Host ""
Write-Host "便携版压缩包: $($ZipFile.FullName)" -ForegroundColor White
Write-Host "  大小: $ZipSize MB" -ForegroundColor Gray
Write-Host ""
Write-Host "下一步: 运行 build\create-installer.ps1 创建安装程序" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
