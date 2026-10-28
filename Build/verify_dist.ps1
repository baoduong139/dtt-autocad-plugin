$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "dist"

$files = Get-ChildItem -Path $dist -File

Write-Host "=========================================================================================="
Write-Host "THỐNG KÊ CHI TIẾT CÁC FILE ĐẦU RA TRONG DIST CỦA DTT_AUTOCAD_PLUGIN"
Write-Host "=========================================================================================="

foreach ($f in $files) {
    $sha = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash
    $ver = (Get-Item $f.FullName).VersionInfo
    [PSCustomObject]@{
        Name = $f.Name
        Size = "{0:N0} bytes" -f $f.Length
        ProductVersion = $ver.ProductVersion
        FileVersion = $ver.FileVersion
        LastWriteTime = $f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        SHA256 = $sha
    } | Format-List
}
