<#
.SYNOPSIS
    DTT Tools Pro - Daily Autonomous Development Runner Script
.DESCRIPTION
    Tự động thực thi phiên làm việc hằng ngày của AI Autonomous System:
    Phân tích -> Đề xuất -> Lập trình -> Kiểm thử -> Build -> Cập nhật GitHub -> Báo cáo.
#>

param(
    [string]$ProjectDir = $PSScriptRoot + "\..",
    [string]$AgyExe = "C:\Users\Duong\.gemini\bin\agy.exe",
    [string]$ProjectId = "4a99ce22-88d6-4eac-8b40-c3751ed7fdee"
)

$ErrorActionPreference = "Continue"
$dateStr = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logDir = "$ProjectDir\logs"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }
$logFile = "$logDir\daily_run_$dateStr.log"

function Log-Message([string]$msg) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timestamp] $msg"
    Write-Host $line
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

Log-Message "=========================================================="
Log-Message "  DTT TOOLS PRO - PHIÊN PHÁT TRIỂN TỰ ĐỘNG HẰNG NGÀY AI   "
Log-Message "=========================================================="
Log-Message "Thư mục dự án: $ProjectDir"
Log-Message "Project ID: $ProjectId"

# 1. Kiểm tra Git repo
Push-Location $ProjectDir
try {
    $gitExe = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
    if (-not (Test-Path $gitExe)) { $gitExe = "git" }

    Log-Message "Đang kiểm tra trạng thái Git..."
    & $gitExe status --short | Out-String | ForEach-Object { if ($_) { Log-Message $_ } }

    # 2. Thực thi kiểm thử tự động
    $testExe = "$ProjectDir\tests\DTools.Tests\bin\Release\net48\DTools.Tests.exe"
    if (Test-Path $testExe) {
        Log-Message "Đang chạy bộ kiểm thử tự động..."
        $testOutput = & $testExe | Out-String
        Log-Message $testOutput
    }

    # 3. Kích hoạt Antigravity AI Engine
    if (Test-Path $AgyExe) {
        Log-Message "Kích hoạt AI Autonomous Session qua CLI chính thức agy.exe..."
        $prompt = "Bạn là AI Autonomous Developer của DTT Tools Pro. Hôm nay là $(Get-Date -Format 'dd/MM/yyyy'). Hãy đọc docs/ai-development/DEVELOPMENT_BACKLOG.md, kiểm tra mã nguồn, chọn 1 công việc ưu tiên để thực hiện, chạy kiểm thử, build và lập báo cáo ngày theo đúng cấu trúc."
        
        & $AgyExe --project $ProjectId --mode auto --print $prompt | Out-String | ForEach-Object {
            if ($_) { Log-Message $_ }
        }
    } else {
        Log-Message "CẢNH BÁO: Không tìm thấy agy.exe tại $AgyExe."
    }

    Log-Message "Phiên làm việc tự động hoàn tất thành công."
}
catch {
    Log-Message "LỖI TRONG PHIÊN TỰ ĐỘNG: $_"
}
finally {
    Pop-Location
}
