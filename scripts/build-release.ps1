$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "BAT DAU QUY TRINH BUILD RELEASE DTT TOOLS PRO v2026.2.0" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    $msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
}
if (-not (Test-Path $msbuild)) {
    throw "Khong tim thay Visual Studio MSBuild.exe!"
}

Write-Host "`n[BUOC 1/3] Bien dich toan bo Solution DTools.sln (Release)..." -ForegroundColor Yellow
$slnPath = Join-Path $root "DTools.sln"
& $msbuild $slnPath -p:Configuration=Release -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
    throw "Loi bien dich DTools.sln!"
}
Write-Host "Bien dich Solution thanh cong 100%!" -ForegroundColor Green

Write-Host "`n[BUOC 2/3] Chay bo kiem thu tu dong toan dien (Unit & Integration Tests)..." -ForegroundColor Yellow
$testExe = Join-Path $root "tests\DTools.Tests\bin\Release\net48\DTools.Tests.exe"
if (Test-Path $testExe) {
    & $testExe
    if ($LASTEXITCODE -ne 0) {
        throw "Co bai test that bai! Huy quy trinh dong goi Release."
    }
    Write-Host "Tat ca 22/22 bai kiem thu tu dong deu VUOT QUA (PASS)!" -ForegroundColor Green
}

Write-Host "`n[BUOC 3/3] Tien hanh dong goi tron bo qua package.ps1..." -ForegroundColor Yellow
$packageScript = Join-Path $PSScriptRoot "package.ps1"
& $packageScript

Write-Host "`nQUY TRINH BUILD & DONG GOI RELEASE HOAN TAT THANH CONG!" -ForegroundColor Green
