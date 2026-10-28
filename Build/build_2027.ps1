# PowerShell Build Script for AutoCAD Electrical 2027 (.NET 8 Profile)
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "[BUILD 2027] BAT DAU BIEN DICH CHO AUTOCAD ELECTRICAL 2027 (.NET 8 X64)" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

python "$rootDir\Build\build_2027.py"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[THÀNH CÔNG 100%] Đã tạo xong gói phân phối AutoCAD Electrical 2027!" -ForegroundColor Green
    Write-Host "Tệp ZIP gửi bạn bè/khách hàng:" -ForegroundColor Yellow
    Write-Host "$rootDir\dist_2027\DTT_Tools_2027_Electrical_v2027.1.0.zip" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[LỖI] Quá trình build 2027 thất bại!" -ForegroundColor Red
}
