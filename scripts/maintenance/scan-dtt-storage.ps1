[CmdletBinding()]
param (
    [string]$ExportJsonPath = "",
    [string]$ExportTxtPath = ""
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "BAT DAU QUET BO NHO DEM VA DU LIEU TAM DTT TOOLS (CHE DO CHI QUET)" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$findings = @()

# 1. Quet %TEMP% cua nguoi dung (File tam dtt*, tmp*, .log cu > 24h)
$tempDir = [System.IO.Path]::GetTempPath()
Write-Host "`n[1/4] Dang quet thu muc tam nguoi dung: $tempDir..." -ForegroundColor Yellow
if (Test-Path $tempDir) {
    Get-ChildItem -Path $tempDir -File -ErrorAction SilentlyContinue | Where-Object {
        ($_.Name -like "dtt*" -or $_.Name -like "*acad*tmp*" -or $_.Extension -eq ".tmp") -and
        ($_.LastWriteTime -lt (Get-Date).AddHours(-24))
    } | ForEach-Object {
        $findings += [PSCustomObject]@{
            Path = $_.FullName
            Name = $_.Name
            Size = $_.Length
            SizeFormatted = "{0:N2} MB" -f ($_.Length / 1MB)
            LastModified = $_.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
            RiskLevel = "LowRisk"
            Category = "UserTemp"
            Reason = "Tep tin tam %TEMP% cu hon 24 gio."
            RecommendedAction = "Quarantine"
        }
    }
}

# 2. Quet bo nho dem DTT Staging (%LocalAppData%\DToolsPro\Staging)
$localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$stagingDir = Join-Path $localAppData "DToolsPro\Staging"
Write-Host "`n[2/4] Dang quet bo nho dem DTT Staging: $stagingDir..." -ForegroundColor Yellow
if (Test-Path $stagingDir) {
    Get-ChildItem -Path $stagingDir -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $findings += [PSCustomObject]@{
            Path = $_.FullName
            Name = $_.Name
            Size = $_.Length
            SizeFormatted = "{0:N2} KB" -f ($_.Length / 1KB)
            LastModified = $_.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
            RiskLevel = "LowRisk"
            Category = "DttStaging"
            Reason = "Tep tam trong qua trinh tai ban cap nhat truoc do."
            RecommendedAction = "Quarantine"
        }
    }
}

# 3. Quet bo nho dem bien dich MSBuild trong workspace (obj va bin tam)
Write-Host "`n[3/4] Dang quet bo nho dem bien dich C# (bin/obj) trong workspace: $root..." -ForegroundColor Yellow
$buildDirs = @("obj", "bin", ".dotnet", "dist_2027\bin")
foreach ($bd in $buildDirs) {
    $fullPath = Join-Path $root $bd
    if (Test-Path $fullPath) {
        $items = Get-ChildItem -Path $fullPath -Recurse -File -ErrorAction SilentlyContinue
        $totalSize = ($items | Measure-Object -Property Length -Sum).Sum
        if ($totalSize -gt 0) {
            $findings += [PSCustomObject]@{
                Path = $fullPath
                Name = "$bd (Build Artifacts)"
                Size = $totalSize
                SizeFormatted = "{0:N2} MB" -f ($totalSize / 1MB)
                LastModified = (Get-Item $fullPath).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                RiskLevel = "ReviewRequired"
                Category = "BuildArtifacts"
                Reason = "Thu muc dau ra bien dich tam cua C#. Hoan toan co the tai tao khi build lai."
                RecommendedAction = "ReviewBeforeClean"
            }
        }
    }
}

# 4. Quet tep sao luu zip lon da tao truoc do
Write-Host "`n[4/4] Dang quet cac tep sao luu nen (ZIP) cu..." -ForegroundColor Yellow
Get-ChildItem -Path $root -Filter "_backup_*.zip" -File | ForEach-Object {
    $findings += [PSCustomObject]@{
        Path = $_.FullName
        Name = $_.Name
        Size = $_.Length
        SizeFormatted = "{0:N2} MB" -f ($_.Length / 1MB)
        LastModified = $_.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        RiskLevel = "ReviewRequired"
        Category = "BackupArchive"
        Reason = "Ban sao luu nen cua du an. Can giu lai ban moi nhat lam moc rollback."
        RecommendedAction = "KeepLatestOnly"
    }
}

# TONG KET BAO CAO
Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host "TONG KET KET QUA QUET LUU TRU (READ-ONLY REPORT)" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$totalBytes = ($findings | Measure-Object -Property Size -Sum).Sum
Write-Host "Tong so muc phat hien : $($findings.Count)"
Write-Host ("Tong dung luong do duoc: {0:N2} MB ({1:N0} bytes)" -f ($totalBytes / 1MB), $totalBytes)

Write-Host "`nDANH SACH CHI TIET CAC MUC DUOC DE XUAT XEM XET:"
$findings | Format-Table -Property RiskLevel, Category, SizeFormatted, Name, Reason -AutoSize

# Xuat bao cao neu duoc yeu cau
if (-not $ExportJsonPath) {
    $ExportJsonPath = Join-Path $root "Build\cleanup_scan_report.json"
}
$findingsJson = $findings | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($ExportJsonPath, $findingsJson)
Write-Host "`nDa luu bao cao JSON tai: $ExportJsonPath" -ForegroundColor Green

if ($ExportTxtPath) {
    $findings | Format-List | Out-File -FilePath $ExportTxtPath -Encoding UTF8
    Write-Host "Da luu bao cao TXT tai: $ExportTxtPath" -ForegroundColor Green
}
