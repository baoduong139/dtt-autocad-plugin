# OLD_DTOOLS_INVENTORY.md — KIỂM KÊ CÁC BẢN DTOOLS TRÊN HỆ THỐNG

## 1. Bảng Tổng Hợp Kiểm Kê Vị Trí Plugin & Bundle
Dưới đây là bảng thống kê toàn bộ các thư mục, bundle và file nạp của DTools/DTT được khảo sát trên máy tính:

| STT | Đường Dẫn Tuyệt Đối | Định Danh Bundle / File | Nguồn Gốc | Đang Được AutoCAD Nạp? | Trạng Thái Xử Lý |
|---|---|---|---|---|---|
| 1 | `%APPDATA%\Autodesk\ApplicationPlugins\DTT_Tools.bundle` | `DTT_Tools.bundle` (v3.5 / 2026.3.5) | Bản cài đặt người dùng (User-scope) | CÓ (Tự động nạp qua Autoloader) | **HỢP LỆ — ĐÃ CẬP NHẬT BẢN MỚI** |
| 2 | `%PROGRAMDATA%\Autodesk\ApplicationPlugins\DTT_Tools.bundle` | `DTT_Tools.bundle` | Bản cài đặt toàn bộ máy (All-Users) | CÓ (Nếu tồn tại) | **ĐÃ CÁCH LY AN TOÀN** vào thư mục Quarantine |
| 3 | `%APPDATA%\Autodesk\ApplicationPlugins\DTools.bundle` | `DTools.bundle` | Bản DTools cũ cài nhầm từ zip/thử nghiệm | CÓ NGUY CƠ NẠP TRÙNG | **ĐÃ CÁCH LY AN TOÀN** vào Quarantine |
| 4 | `%PROGRAMDATA%\Autodesk\ApplicationPlugins\DTools.bundle` | `DTools.bundle` | Bản DTools cũ cài nhầm | CÓ NGUY CƠ NẠP TRÙNG | **ĐÃ CÁCH LY AN TOÀN** vào Quarantine |
| 5 | `D:\VSTUDIO FILE\DTools` | Thư mục source cũ ngoài luồng | Nguồn cũ ngoài scratch | KHÔNG ĐƯỢC NẠP | **GIỮ NGUYÊN (KHÔNG ĐỤNG CHẠM)** |

## 2. Nhận Diện Sự Trùng Lặp & Xung Đột
- Khi cả `DTools.bundle` và `DTT_Tools.bundle` cùng tồn tại trong `ApplicationPlugins`, AutoCAD sẽ nạp cả hai DLL vào cùng một AppDomain.
- Vì cả hai DLL đều định nghĩa các lệnh trùng tên như `[CommandMethod("DTT")]` hoặc các lớp static singleton, việc này gây ra xung đột namespace, deadlock khi đăng ký lệnh, hoặc lỗi `e0434352h` khi khởi động.
- Do đó, toàn bộ các bundle mang tên `DTools.bundle` đã được cách ly ra khỏi thư mục `ApplicationPlugins`.

## 3. Bản Hợp Lệ Duy Nhất
- Bản hợp lệ duy nhất hiện tại là `DTT_Tools.bundle` chứa DLL `DTT_AutoCAD_Plugin.dll` (SHA-256: `FE43FDCF0905684683E449DE059868721D18334B8F736BD3DC6CC2E9070080B8`).
