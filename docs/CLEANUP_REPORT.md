# CLEANUP_REPORT.md — BÁO CÁO SAO LƯU VÀ CÁCH LY AN TOÀN

## 1. Nguyên Tắc An Toàn Tuyệt Đối Đã Áp Dụng
- KHÔNG sử dụng lệnh xóa vĩnh viễn (`del /s`, `Remove-Item -Recurse`) đối với bất kỳ file nào chưa được xác nhận rõ ràng.
- Toàn bộ dữ liệu trước khi di chuyển đều được sao lưu nguyên vẹn vào thư mục Backup có đóng dấu thời gian (Timestamp).
- Các bundle cũ/cài nhầm được chuyển vào thư mục cách ly (Quarantine) nằm ngoài phạm vi quét của AutoCAD.

## 2. Thư Mục Sao Lưu (Backup)
- **Đường dẫn Backup**: `C:\Users\Duong\.gemini\antigravity-ide\scratch\BACKUP_DTT_20261027_110230\`
- **Nội dung sao lưu**:
  - Toàn bộ `DTT_Tools.bundle` từ `%APPDATA%\Autodesk\ApplicationPlugins`
  - Toàn bộ `DTT_Tools.bundle` từ `%PROGRAMDATA%\Autodesk\ApplicationPlugins`
  - Thư mục cấu hình `DTT_AutoCAD_Plugin` trong `%LOCALAPPDATA%`
- **Trạng thái**: Sao lưu đầy đủ, có thể khôi phục 100% bất kỳ lúc nào.

## 3. Thư Mục Cách Ly (Quarantine)
- **Đường dẫn Quarantine**: `C:\Users\Duong\.gemini\antigravity-ide\scratch\QUARANTINE_DTT_20261027_110230\`
- **Các thành phần đã cách ly**:
  - `QUARANTINE_DTT_20261027_110230\APPDATA_DTT_Tools.bundle`
  - `QUARANTINE_DTT_20261027_110230\PROGRAMDATA_DTT_Tools.bundle`
  - Các bản cài `DTools.bundle` cũ phát hiện trong hệ thống.
- **Trạng thái nạp**: AutoCAD đã ngắt kết nối hoàn toàn với các bundle này, không còn nguy cơ nạp kép hay xung đột phiên bản.

## 4. Hướng Dẫn Khôi Phục (Rollback)
Nếu người dùng muốn khôi phục lại trạng thái trước khi cách ly, chỉ cần chạy script khôi phục hoặc sao chép ngược lại:
```powershell
Copy-Item -Path "C:\Users\Duong\.gemini\antigravity-ide\scratch\BACKUP_DTT_20261027_110230\APPDATA_DTT_Tools.bundle" -Destination "$env:APPDATA\Autodesk\ApplicationPlugins\DTT_Tools.bundle" -Recurse -Force
```
*Lưu ý: Chỉ thực hiện khôi phục khi đã đóng toàn bộ AutoCAD.*

## 5. Danh Sách File Chờ Người Dùng Xác Nhận Xóa Vĩnh Viễn
Hiện tại toàn bộ file cách ly vẫn được lưu giữ an toàn trong `QUARANTINE_DTT_20261027_110230`. Hệ thống KHÔNG tự ý xóa vĩnh viễn bất kỳ file nào cho đến khi người dùng kiểm tra bản mới chạy PASS hoàn toàn và đưa ra chỉ thị xóa.
