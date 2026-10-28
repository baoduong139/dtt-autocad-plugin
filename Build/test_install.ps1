$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'dist\Setup_DTT_Tools.exe'
if (-not (Test-Path $exe)) {
    $exe = Join-Path $root 'dist\release\DTT_Tools_Setup.exe'
}

$proc = Start-Process -FilePath $exe -ArgumentList '/silent' -Wait -PassThru
Write-Host "Exit Code: " $proc.ExitCode

$appdata = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$installedDll = Join-Path $appdata 'Autodesk\ApplicationPlugins\DTT_Tools.bundle\Contents\DTT_AutoCAD_Plugin.dll'
$distDll = Join-Path $root 'dist\DTT_AutoCAD_Plugin.dll'

if (Test-Path $installedDll) {
    $h1 = (Get-FileHash -Path $installedDll -Algorithm SHA256).Hash
    $h2 = (Get-FileHash -Path $distDll -Algorithm SHA256).Hash
    Write-Host "Installed Path: $installedDll"
    Write-Host "Installed SHA256: $h1"
    Write-Host "Dist SHA256:      $h2"
    if ($h1 -eq $h2) {
        Write-Host "MATCH: YES (100%)"
    } else {
        Write-Host "MATCH: NO"
    }
} else {
    Write-Host "Installed DLL NOT FOUND at: $installedDll"
}
