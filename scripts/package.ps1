$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "TIEN HANH DONG GOI GOI UNG DUNG VA BO CAI DAT RELEASE" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$distDir = Join-Path $root "dist"
$releaseDir = Join-Path $distDir "release"
$bundleDir = Join-Path $root "DTT_Tools.bundle"
$contentsDir = Join-Path $bundleDir "Contents"

if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Force -Path $distDir | Out-Null }
if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null }
if (-not (Test-Path $contentsDir)) { New-Item -ItemType Directory -Force -Path $contentsDir | Out-Null }

# 1. Dong bo DLL & EXE vao Bundle Contents
Write-Host "`n[1/6] Dong bo cac tep nhi phan DLL va EXE vao DTT_Tools.bundle..." -ForegroundColor Yellow
$binNet48 = Join-Path $root "bin\Release\net48"
$managerBin = Join-Path $root "src\DTT.Manager\bin\Release\net48"
$updaterBin = Join-Path $root "src\DTT.Updater\bin\Release\net48"

$artifacts = @(
    @{ Src = Join-Path $binNet48 "DTT_AutoCAD_Plugin.dll"; Dest = "DTT_AutoCAD_Plugin.dll" },
    @{ Src = Join-Path $binNet48 "DTools.Core.dll"; Dest = "DTools.Core.dll" },
    @{ Src = Join-Path $binNet48 "DTools.Application.dll"; Dest = "DTools.Application.dll" },
    @{ Src = Join-Path $managerBin "DTT_Manager.exe"; Dest = "DTT_Manager.exe" },
    @{ Src = Join-Path $updaterBin "DTT_Updater.exe"; Dest = "DTT_Updater.exe" }
)

foreach ($art in $artifacts) {
    if (Test-Path $art.Src) {
        Copy-Item -Path $art.Src -Destination (Join-Path $contentsDir $art.Dest) -Force
        Copy-Item -Path $art.Src -Destination (Join-Path $distDir $art.Dest) -Force
        Copy-Item -Path $art.Src -Destination (Join-Path $releaseDir $art.Dest) -Force
        Write-Host "  Da dong bo: $($art.Dest)" -ForegroundColor Green
    } else {
        Write-Warning "Khong tim thay tep nhi phan: $($art.Src)"
    }
}

# Dong bo symbols_database.json
$symJson = Join-Path $root "symbols_database.json"
if (Test-Path $symJson) {
    Copy-Item -Path $symJson -Destination (Join-Path $contentsDir "symbols_database.json") -Force
    Copy-Item -Path $symJson -Destination (Join-Path $distDir "symbols_database.json") -Force
    Copy-Item -Path $symJson -Destination (Join-Path $releaseDir "symbols_database.json") -Force
}

# 2. Dong bo Blocks va Samples
Write-Host "`n[2/6] Dong bo thu vien Dynamic Blocks va Mau ban ve..." -ForegroundColor Yellow
$blocksSrc = Join-Path $root "Blocks"
if (Test-Path $blocksSrc) {
    Copy-Item -Path "$blocksSrc\*" -Destination (Join-Path $contentsDir "Blocks") -Recurse -Force
}
$samplesSrc = Join-Path $contentsDir "samples"
if (Test-Path $samplesSrc) {
    Copy-Item -Path "$samplesSrc\*" -Destination (Join-Path $distDir "samples") -Recurse -Force
}

# 3. Nen Bundle thanh ZIP Payload de nhung vao Installer
Write-Host "`n[3/6] Nen goi DTT_Tools.bundle thanh ZIP Payload nhung..." -ForegroundColor Yellow
$bundleZip = Join-Path $distDir "bundle_payload.zip"
if (Test-Path $bundleZip) { Remove-Item -Path $bundleZip -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($bundleDir, $bundleZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
$zipSize = (Get-Item $bundleZip).Length
Write-Host ("  Payload ZIP: {0:N2} MB" -f ($zipSize / 1MB)) -ForegroundColor Green

# 4. Bien dich Bo Cai Dat DTT_Tools_Setup.exe doc lap (csc.exe)
Write-Host "`n[4/6] Bien dich Bo Cai Dat DTT_Tools_Setup.exe bang C# Native Installer..." -ForegroundColor Yellow
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

$setupSource = Join-Path $root "Build\Installer\SetupProgram.cs"
$setupExe = Join-Path $releaseDir "DTT_Tools_Setup.exe"

$cscArgs = @(
    "/target:winexe",
    "/platform:x64",
    "/out:$setupExe",
    "/reference:System.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.IO.Compression.dll",
    "/reference:System.IO.Compression.FileSystem.dll",
    "/resource:$bundleZip,BundlePayload",
    $setupSource
)

& $csc $cscArgs
if ($LASTEXITCODE -eq 0 -and (Test-Path $setupExe)) {
    Write-Host "  Da tao DTT_Tools_Setup.exe tai: $setupExe" -ForegroundColor Green
    Copy-Item -Path $setupExe -Destination (Join-Path $distDir "Setup_DTT_Tools.exe") -Force
    Copy-Item -Path $setupExe -Destination (Join-Path $distDir "Setup_DTools_Pro.exe") -Force
} else {
    throw "Loi bien dich DTT_Tools_Setup.exe!"
}

# Don dep tep ZIP tam
if (Test-Path $bundleZip) { Remove-Item -Path $bundleZip -Force }

# 5. Tao version.json manifest cap nhat tu xa
Write-Host "`n[5/6] Sinh tep manifest cap nhat tu xa (version.json)..." -ForegroundColor Yellow
$dllPath = Join-Path $releaseDir "DTT_AutoCAD_Plugin.dll"
$dllHash = (Get-FileHash -Path $dllPath -Algorithm SHA256).Hash
$setupHash = (Get-FileHash -Path $setupExe -Algorithm SHA256).Hash
$setupSize = (Get-Item $setupExe).Length

$manifest = @{
    schemaVersion = "1.0"
    version = "2026.2.0"
    channel = "stable"
    publishedAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    releaseNotesUrl = "https://raw.githubusercontent.com/baoduong139/dtt-autocad-plugin/main/dist/release/CHANGELOG.md"
    releaseNotes = "DTT TOOLS Pro v2026.2.0: Tich hop day du DTT TOOLS Manager, Remote Auto Updater, Safe Cleanup 8GB RAM Safe, So do DOL va Sao-Tam giac chuan IEC, va Mat bang chieu sang 12x6m."
    minAutoCadVersion = "2020"
    maxAutoCadVersion = "2026"
    downloadUrl = "https://raw.githubusercontent.com/baoduong139/dtt-autocad-plugin/main/dist/update.zip"
    sha256 = $setupHash
    packageSize = $setupSize
    pluginDllSha256 = $dllHash
    publisher = "DTT CAD Software Corporation"
    mandatory = $false
}

$manifestJson = $manifest | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText((Join-Path $releaseDir "version.json"), $manifestJson)
[System.IO.File]::WriteAllText((Join-Path $distDir "version.json"), $manifestJson)
Write-Host "  Da tao version.json (SHA-256 Setup: $setupHash)" -ForegroundColor Green

# 6. Sao chep Tai lieu Huong Dan va Changelog
Write-Host "`n[6/6] Tao tai lieu phat hanh (CHANGELOG.md va HUONG_DAN_CAI_DAT.md)..." -ForegroundColor Yellow
$changelogPath = Join-Path $releaseDir "CHANGELOG.md"
$guidePath = Join-Path $releaseDir "HUONG_DAN_CAI_DAT.md"

$changelogContent = @"
# NHAT KY PHIEN BAN (CHANGELOG) - DTT TOOLS PRO

## Phien ban 2026.2.0 (Release)
- Safe Startup 100% Lazy Loading chong crash AutoCAD.
- DTT_Manager.exe: Quan ly cai dat, sua chua, cap nhat va don dep an toan.
- DTT_Updater.exe: Cap nhat tu xa an toan qua HTTPS manifest va SHA-256.
- So do DOL chuan IEC (MCB2 3P, MC1 13-14 NO, TH1 95-96 NC).
- Mach Sao-Tam giac khoa cheo kep dien va co khi kem Dead-time 50ms.
- Mat bang chieu sang 12x6m chuan ky thuat.
- Bo cai DTT_Tools_Setup.exe tu dong nhan dien AutoCAD 2021 tren moi o dia.
"@

$guideContent = @"
# HUONG DAN CAI DAT DTT TOOLS PRO v2026.2.0

## 1. Yeu cau he thong
- Windows 10/11 64-bit
- AutoCAD 2021 / AutoCAD Electrical 2021 (R24.0)
- RAM: 8 GB

## 2. Cach cai dat
1. Dong AutoCAD neu dang mo.
2. Chay tep DTT_Tools_Setup.exe.
3. Bam [CAI DAT NGAY].
4. Mo AutoCAD va su dung Palette DTT (hoac go DTT).
"@

[System.IO.File]::WriteAllText($changelogPath, $changelogContent)
[System.IO.File]::WriteAllText($guidePath, $guideContent)

Write-Host "`nTAT CA THANH PHAM DA DUOC DONG GOI DAY DU VAO: $releaseDir" -ForegroundColor Cyan
Get-ChildItem -Path $releaseDir | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
