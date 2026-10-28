[CmdletBinding(SupportsShouldProcess = $true)]
param (
    [Parameter(Mandatory = $false)]
    [switch]$Execute,

    [Parameter(Mandatory = $false)]
    [string]$ManifestInputPath = ""
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "TIEN HANH DON DEP CAC MUC DUOC PHE DUYET (CACH LY AN TOAN)" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

if (-not $Execute) {
    Write-Warning "Script dang chay o che do MO PHONG (-WhatIf / Dry-run)."
    Write-Warning "Khong co tep tin nao bi di chuyen hoac xoa bo."
    Write-Warning "De thuc thi thuc te, can truyen them tham so: -Execute"
}

if (-not $ManifestInputPath) {
    $ManifestInputPath = Join-Path $root "Build\cleanup_scan_report.json"
}

if (-not (Test-Path $ManifestInputPath)) {
    throw "Khong tim thay tep danh sach phe duyet tai: $ManifestInputPath. Hay chay scan-dtt-storage.ps1 truoc!"
}

$items = Get-Content -Path $ManifestInputPath -Raw | ConvertFrom-Json
$approved = $items | Where-Object { $_.RiskLevel -eq "LowRisk" }

Write-Host "So luong muc thuoc dien LowRisk duoc xu ly: $($approved.Count)"

$localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$quarantineDir = Join-Path $localAppData ("DToolsPro\Quarantine\Quarantine_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))

if ($Execute) {
    New-Item -ItemType Directory -Force -Path $quarantineDir | Out-Null
}

$reclaimed = 0

foreach ($it in $approved) {
    $p = $it.Path
    if (-not (Test-Path $p)) { continue }

    # BẢO VỆ TUYỆT ĐỐI KHÔNG CAN THIỆP MÃ NGUỒN VÀ BẢN VẼ
    if ($p -like "*.cs" -or $p -like "*.sln" -or $p -like "*.csproj" -or $p -like "*.dwg") {
        Write-Warning "BAO VE: Bo qua tep ma nguon hoac ban ve: $p"
        continue
    }

    if ($Execute) {
        $dest = Join-Path $quarantineDir ([System.IO.Path]::GetFileName($p))
        Write-Host "  -> Cach ly: $p -> $dest" -ForegroundColor Green
        Move-Item -Path $p -Destination $dest -Force
        $reclaimed += $it.Size
    } else {
        Write-Host "  [SIMULATION] Cach ly: $p (Dung luong: $($it.SizeFormatted))" -ForegroundColor Gray
        $reclaimed += $it.Size
    }
}

Write-Host "`nTONG DUNG LUONG DU KIEN THU HOI: {0:N2} MB" -f ($reclaimed / 1MB) -ForegroundColor Yellow
if ($Execute) {
    Write-Host "Cac muc da duoc cach ly an toan tai: $quarantineDir" -ForegroundColor Green
    Write-Host "Co the khoi phuc bat ky luc nao bang cach copy lai tep tu Quarantine ve vi tri cu." -ForegroundColor Cyan
}
