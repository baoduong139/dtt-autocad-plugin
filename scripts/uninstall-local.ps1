# =====================================================================
# DTT TOOLS PRO - GỠ CÀI ĐẶT CỤC BỘ (UNINSTALL-LOCAL.PS1)
# =====================================================================
$ErrorActionPreference = "Stop"

Write-Host "🗑️ TIẾN HÀNH GỠ BỎ DTT TOOLS KHỎI AUTOCAD..." -ForegroundColor Cyan

# Kiểm tra AutoCAD đang mở
$acadProcs = Get-Process -Name "acad" -ErrorAction SilentlyContinue
if ($acadProcs) {
    Write-Warning "AutoCAD đang chạy! Vui lòng đóng AutoCAD trước khi gỡ cài đặt để tránh lỗi khóa tệp."
    exit 2
}

$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$bundleDir = Join-Path $appData "Autodesk\ApplicationPlugins\DTT_Tools.bundle"

if (Test-Path $bundleDir) {
    Remove-Item -Path $bundleDir -Recurse -Force
    Write-Host "✔ Đã xóa thư mục Bundle: $bundleDir" -ForegroundColor Green
} else {
    Write-Host "ℹ️ Thư mục Bundle không tồn tại hoặc đã được gỡ bỏ trước đó." -ForegroundColor Gray
}

# Xóa Shortcut Desktop & Start Menu
$desktopLnk = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)) "DTT TOOLS Manager.lnk"
if (Test-Path $desktopLnk) {
    Remove-Item -Path $desktopLnk -Force
    Write-Host "✔ Đã xóa Shortcut trên Desktop" -ForegroundColor Green
}

$startMenuDir = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)) "DTT TOOLS PRO"
if (Test-Path $startMenuDir) {
    Remove-Item -Path $startMenuDir -Recurse -Force
    Write-Host "✔ Đã xóa Shortcut trong Start Menu" -ForegroundColor Green
}

Write-Host "🎉 GỠ BỎ HOÀN TẤT THÀNH CÔNG!" -ForegroundColor Green

