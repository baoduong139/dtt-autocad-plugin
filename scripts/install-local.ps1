$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "TIEN HANH CAI DAT DTT TOOLS PRO VAO MOI TRUONG AUTOCAD CUC BO..." -ForegroundColor Cyan

# Kiem tra AutoCAD dang mo
$acadProcs = Get-Process -Name "acad" -ErrorAction SilentlyContinue
if ($acadProcs) {
    Write-Warning "AutoCAD dang chay! Vui long dong AutoCAD de tranh loi khoa tep DLL."
    exit 2
}

$setupExe = Join-Path $root "dist\release\DTT_Tools_Setup.exe"
if (-not (Test-Path $setupExe)) {
    $setupExe = Join-Path $root "dist\Setup_DTT_Tools.exe"
}

if (Test-Path $setupExe) {
    Write-Host "Chay trinh cai dat o che do im lang (/silent)..." -ForegroundColor Yellow
    $p = Start-Process -FilePath $setupExe -ArgumentList "/silent" -Wait -PassThru
    Write-Host "Ma ket thuc cai dat: $($p.ExitCode)" -ForegroundColor Green
} else {
    Write-Host "Khong tim thay file Setup EXE, tien hanh sao chep Bundle truc tiep..." -ForegroundColor Yellow
    $appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
    $targetBundle = Join-Path $appData "Autodesk\ApplicationPlugins\DTT_Tools.bundle"
    $sourceBundle = Join-Path $root "DTT_Tools.bundle"

    if (-not (Test-Path $targetBundle)) { New-Item -ItemType Directory -Force -Path $targetBundle | Out-Null }
    Copy-Item -Path "$sourceBundle\*" -Destination $targetBundle -Recurse -Force
}

$testScript = Join-Path $PSScriptRoot "test-install.ps1"
if (Test-Path $testScript) {
    & $testScript
}
