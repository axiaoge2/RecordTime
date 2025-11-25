# RecordTime 安装程序创建脚本
# 用于使用 Inno Setup 创建 Windows 安装程序

param(
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

# 设置路径
$RootPath = Split-Path -Parent $PSScriptRoot
$SetupScript = Join-Path $PSScriptRoot "setup.iss"
$OutputPath = Join-Path $RootPath "build\output"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RecordTime 安装程序创建脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 Inno Setup 是否安装
if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "未找到 Inno Setup" -ForegroundColor Red
    Write-Host ""
    Write-Host "请先安装 Inno Setup:" -ForegroundColor Yellow
    Write-Host "  下载地址: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    Write-Host "  默认安装路径: C:\Program Files (x86)\Inno Setup 6\" -ForegroundColor Gray
    Write-Host ""
    Write-Host "如果已安装但路径不同，请使用 -InnoSetupPath 参数:" -ForegroundColor Yellow
    Write-Host '  .\create-installer.ps1 -InnoSetupPath "C:\Your\Path\ISCC.exe"' -ForegroundColor Gray
    exit 1
}

# 检查是否已经发布
if (-not (Test-Path $OutputPath)) {
    Write-Host "未找到发布文件" -ForegroundColor Red
    Write-Host ""
    Write-Host "请先运行发布脚本:" -ForegroundColor Yellow
    Write-Host "  .\build\publish.ps1" -ForegroundColor Gray
    exit 1
}

$ExeFile = Join-Path $OutputPath "RecordTime.exe"
if (-not (Test-Path $ExeFile)) {
    Write-Host "未找到 RecordTime.exe" -ForegroundColor Red
    Write-Host ""
    Write-Host "请先运行发布脚本:" -ForegroundColor Yellow
    Write-Host "  .\build\publish.ps1" -ForegroundColor Gray
    exit 1
}

# 编译安装程序
Write-Host "正在创建安装程序..." -ForegroundColor Yellow
Write-Host ""

& $InnoSetupPath $SetupScript

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "创建安装程序失败" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "安装程序创建完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# 显示生成的文件
$PublishPath = Join-Path $RootPath "build\publish"
$SetupFiles = Get-ChildItem -Path $PublishPath -Filter "RecordTime-*-Setup.exe"

if ($SetupFiles) {
    foreach ($file in $SetupFiles) {
        $fileSize = [math]::Round($file.Length / 1MB, 2)
        Write-Host "安装程序: $($file.FullName)" -ForegroundColor White
        Write-Host "  大小: $fileSize MB" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "现在可以分发以下文件给用户:" -ForegroundColor Yellow
Write-Host "  1. RecordTime-v*-Setup.exe (安装版)" -ForegroundColor White
Write-Host "  2. RecordTime-v*-Portable.zip (便携版)" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
