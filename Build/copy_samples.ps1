$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "DTT_Tools.bundle\Contents\samples"
$d1 = Join-Path $root "dist\samples"

if (-not (Test-Path $d1)) { New-Item -ItemType Directory -Force -Path $d1 | Out-Null }

if (Test-Path $src) {
    Copy-Item -Path "$src\*" -Destination $d1 -Force -Recurse
}

Write-Host "Danh sach file trong dist\samples:"
Get-ChildItem -Path $d1 | Select-Object Name, Length | Format-Table -AutoSize
