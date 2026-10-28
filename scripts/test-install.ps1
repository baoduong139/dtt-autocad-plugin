$root = Split-Path -Parent $PSScriptRoot

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "KIEM TRA CHAN DOAN GOI CAI DAT AUTODESK APPLICATIONPLUGINS" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$bundleDir = Join-Path $appData "Autodesk\ApplicationPlugins\DTT_Tools.bundle"
$installedDll = Join-Path $bundleDir "Contents\DTT_AutoCAD_Plugin.dll"
$installedXml = Join-Path $bundleDir "PackageContents.xml"
$installedManager = Join-Path $bundleDir "Contents\DTT_Manager.exe"
$distDll = Join-Path $root "dist\release\DTT_AutoCAD_Plugin.dll"

$allPassed = $true

# 1. Kiem tra Bundle Directory
if (Test-Path $bundleDir) {
    Write-Host "[PASS] Thu muc DTT_Tools.bundle ton tai: $bundleDir" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Khong tim thay thu muc DTT_Tools.bundle tai: $bundleDir" -ForegroundColor Red
    $allPassed = $false
}

# 2. Kiem tra PackageContents.xml
if (Test-Path $installedXml) {
    [xml]$xml = Get-Content $installedXml
    $appVer = $xml.ApplicationPackage.AppVersion
    Write-Host "[PASS] PackageContents.xml hop le. Phien ban: $appVer" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Thieu file PackageContents.xml" -ForegroundColor Red
    $allPassed = $false
}

# 3. Kiem tra Plugin Core DLL & SHA-256
if (Test-Path $installedDll) {
    $h1 = (Get-FileHash -Path $installedDll -Algorithm SHA256).Hash
    Write-Host ("[PASS] DTT_AutoCAD_Plugin.dll ton tai ({0:N0} bytes)" -f (Get-Item $installedDll).Length) -ForegroundColor Green
    Write-Host "       SHA-256: $h1" -ForegroundColor Gray

    if (Test-Path $distDll) {
        $h2 = (Get-FileHash -Path $distDll -Algorithm SHA256).Hash
        if ($h1 -eq $h2) {
            Write-Host "[PASS] Khop 100% SHA-256 giua ban Release va ban da cai dat!" -ForegroundColor Green
        } else {
            Write-Warning "Khac biet ma bam SHA-256 so voi dist/release!"
        }
    }
} else {
    Write-Host "[FAIL] Khong tim thay DTT_AutoCAD_Plugin.dll trong thu muc Contents!" -ForegroundColor Red
    $allPassed = $false
}

# 4. Kiem tra DTT_Manager.exe
if (Test-Path $installedManager) {
    Write-Host ("[PASS] DTT_Manager.exe ton tai trong goi cai dat ({0:N0} bytes)" -f (Get-Item $installedManager).Length) -ForegroundColor Green
} else {
    Write-Host "[INFO] DTT_Manager.exe chua co trong Contents" -ForegroundColor Yellow
}

Write-Host "--------------------------------------------------------------------------"
if ($allPassed) {
    Write-Host "TAT CA KIEM TRA CAI DAT DEU DAT YEU CAU (PASS)!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "PHAT HIEN LOI TRONG GOI CAI DAT!" -ForegroundColor Red
    exit 1
}
