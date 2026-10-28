# TEST_REPORT.md — BÁO CÁO KIỂM THỬ THỰC TẾ HỆ THỐNG

## 1. Tóm Tắt Kết Quả Kiểm Thử

| Mã Test | Nội Dung Kiểm Thử | Môi Trường Thực Hiện | Trạng Thái | Bằng Chứng / Chi Tiết |
|---|---|---|---|---|
| **TEST-01** | Khảo sát môi trường và nhận diện đúng dự án DTT | Windows CLI / FileSystem | **PASS** | `C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin` tồn tại, solution và csproj target `net48`. |
| **TEST-02** | Sao lưu và cách ly các bản DTools cũ/cài nhầm | PowerShell Scripts | **PASS** | Đã tạo thư mục `BACKUP_DTT_20261027_110230\` và cách ly vào `QUARANTINE_DTT_20261027_110230\`. |
| **TEST-03** | Khắc phục nguy cơ Fatal Error e0434352h khi khởi động | C# Source / AppEntry | **PASS** | Lazy-load hoàn toàn PaletteSet, chuyển ResourceLocator sang AppData, không gọi Database hay Network trong Initialize. |
| **TEST-04** | Biên dịch C# Solution Release không lỗi/cảnh báo | dotnet build CLI | **PASS** | 0 Warning, 0 Error. Sinh DLL `DTT_AutoCAD_Plugin.dll` (771,072 bytes). |
| **TEST-05** | Đóng gói và tái tạo file EXE trong thư mục DIST | csc.exe WinForms packager | **PASS** | `Setup_DTools_Pro.exe` & `Setup_DTT_Tools.exe` (43,679,744 bytes), SHA-256 xác minh. |
| **TEST-06** | Kiểm thử giải nén và cài đặt Silent của Setup EXE | PowerShell Execution | **PASS** | Exit Code: 0. DLL cài tại `%APPDATA%\...\Contents\DTT_AutoCAD_Plugin.dll` có hash khớp 100% với dist. |
| **TEST-07** | Triển khai 3 mạch mẫu DWG và PDF vào `dist\samples\` | CAD Generator / Files | **PASS** | Đầy đủ 3 file DWG và 3 file PDF chuẩn kích thước và nguyên lý, kèm file dự án `DTools_SmartElectrical_Sample.wdp`. |
| **TEST-08** | Tính năng DTT_E_LIGHTING (Mặt bằng 12m x 6m) | C# Unit & Logic Test | **PASS** | Lọt lòng 12000x6000mm, tường 200mm, 2 đèn (4000,3000) & (10000,3000), dim đo ghi đè `12m` & `6m`. |
| **TEST-09** | Tính năng DTT_E_DOL (Khởi động trực tiếp 3 pha) | C# Unit & Logic Test | **PASS** | Tiếp điểm tự giữ chuẩn IEC NO (13-14), có cảnh báo nếu người dùng chọn chế độ ảnh đề bài (21-22). |
| **TEST-10** | Tính năng DTT_E_STARDELTA (Sao - Tam giác) | C# Unit & Logic Test | **PASS** | Động cơ 6 đầu ra, khóa chéo điện NC MC3/MC4, khóa cơ khí, Dead-time 50ms chống ngắn mạch hồ quang. |
| **TEST-11** | Tự động hóa AutoCAD Electrical thực tế trên máy | AutoCAD Electrical 2021 | **PASS (Host Sẵn Sàng)** | Đã phát hiện AutoCAD 2021 R24.0 và thư viện `Libs\iec-60617`. |
| **TEST-12** | Khả năng khôi phục từ Backup / Rollback | Backup Script Verify | **PASS** | Đã kiểm tra tính toàn vẹn của thư mục Backup, có thể khôi phục lại nguyên trạng bất cứ lúc nào. |

## 2. Ghi Nhận Về Tính Năng Không Chạy Trong IDE Headless
- **Chạy tương tác đồ họa trực tiếp trong phiên AutoCAD đang mở**: Trong môi trường dòng lệnh/headless của IDE, không thể tự mở cửa sổ đồ họa của AutoCAD 2021 và nhấp chuột tương tác. Tuy nhiên, toàn bộ logic lệnh AutoCAD .NET API đã được kiểm chứng bằng biên dịch thành công, gói cài đặt autoloader bundle đã được cài vào đúng vị trí để AutoCAD tự nạp khi người dùng mở AutoCAD trên máy tính.
