# ROLLBACK_GUIDE_VI.md — HƯỚNG DẪN PHỤC HỒI HỆ THỐNG AN TOÀN

## 1. Giới Thiệu
Tài liệu này cung cấp các bước chi tiết để người dùng có thể khôi phục lại trạng thái ban đầu của hệ thống AutoCAD và các plugin DTools/DTT bất cứ lúc nào nếu cần thiết.

## 2. Vị Trí Các Thư Mục Lưu Trữ
- **Thư mục Sao Lưu (Backup)**:
  `C:\Users\Duong\.gemini\antigravity-ide\scratch\BACKUP_DTT_20261027_110230\`
- **Thư mục Cách Ly (Quarantine)**:
  `C:\Users\Duong\.gemini\antigravity-ide\scratch\QUARANTINE_DTT_20261027_110230\`
- **Thư mục Cài Đặt AutoCAD Autoloader**:
  - Người dùng hiện tại: `%APPDATA%\Autodesk\ApplicationPlugins\`
  - Toàn bộ máy tính: `%PROGRAMDATA%\Autodesk\ApplicationPlugins\`

## 3. Các Bước Khôi Phục (Rollback)

### Bước 1: Đóng AutoCAD
Trước khi thực hiện bất kỳ thao tác nào, hãy đảm bảo đóng hoàn toàn tất cả các tiến trình AutoCAD đang chạy:
```powershell
Get-Process -Name "acad" -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Bước 2: Khôi phục Bundle từ Thư Mục Backup
Chạy lệnh PowerShell sau để khôi phục bản bundle cũ trước khi nâng cấp:
```powershell
$backup = "C:\Users\Duong\.gemini\antigravity-ide\scratch\BACKUP_DTT_20261027_110230"
$destApp = "$env:APPDATA\Autodesk\ApplicationPlugins\DTT_Tools.bundle"

# Xóa bản hiện tại và chép lại bản từ backup
if (Test-Path $destApp) { Remove-Item -Path $destApp -Recurse -Force }
Copy-Item -Path "$backup\APPDATA_DTT_Tools.bundle" -Destination $destApp -Recurse -Force
Write-Host "Đã khôi phục thành công bản DTT_Tools.bundle từ Backup!"
```

### Bước 3: Khôi phục Các Bản Cũ Đã Cách Ly (Nếu Cần Thiết)
Nếu bạn muốn trả lại các bản `DTools.bundle` cũ đã được di chuyển vào Quarantine:
```powershell
$quarantine = "C:\Users\Duong\.gemini\antigravity-ide\scratch\QUARANTINE_DTT_20261027_110230"
# Kiểm tra danh sách trong Quarantine
Get-ChildItem -Path $quarantine
```
*Lưu ý: Không nên đưa `DTools.bundle` cũ trở lại nếu bạn muốn tránh xung đột nạp kép gây lỗi khởi động cho AutoCAD.*

### Bước 4: Kiểm Tra Lại Môi Trường
Khởi động AutoCAD và gõ lệnh `DTT` hoặc `APPLOAD` để kiểm tra trạng thái nạp plugin.
