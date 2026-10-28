# BÁO CÁO KIỂM TOÁN LƯU TRỮ VÀ CƠ CHẾ DỌN DẸP AN TOÀN (CLEANUP AUDIT)
**Dự án:** DTT TOOLS Pro (v2026.2.0)  
**Thời điểm thực hiện kiểm toán:** 28/10/2026  
**Chế độ quét:** Mặc định 100% Chỉ quét / Mô phỏng (Dry-Run / Read-Only Scan)  
**Tập tin dữ liệu quét gốc:** `Build/cleanup_scan_report.json`

---

## 1. NGUYÊN TẮC BẢO VỆ DỮ LIỆU & AN TOÀN TUYỆT ĐỐI
Tuân thủ nghiêm ngặt chỉ đạo của Trưởng dự án và QA Lead:
1. **Không tự ý xóa bất kỳ tệp tin nào** khi chưa có sự xác nhận/phê duyệt của người dùng.
2. **Bảo vệ tuyệt đối:** Tất cả mã nguồn (`*.cs`, `*.xaml`, `*.csproj`, `*.sln`), cơ sở dữ liệu (`symbols_database.json`), bản vẽ AutoCAD (`*.dwg`), tài liệu kỹ thuật, và gói plugin đang hoạt động (`DTT_Tools.bundle`) được đưa vào danh sách **Strictly Protected (Bất khả xâm phạm)**.
3. **Cơ chế Cách ly (Quarantine Isolation):** Khi người dùng phê duyệt dọn dẹp, các tệp rác không bị xóa vĩnh viễn (Hard Delete) mà được di chuyển an toàn vào thư mục cách ly `%LocalAppData%\DToolsPro\Quarantine\Quarantine_yyyyMMdd_HHmmss` kèm nhật ký chi tiết.
4. **Cơ chế Hoàn tác (Rollback):** Bất cứ lúc nào người dùng muốn khôi phục, chỉ cần mở DTT TOOLS Manager hoặc sao chép ngược từ thư mục Quarantine về vị trí ban đầu.

---

## 2. PHÂN LOẠI DANH MỤC LƯU TRỮ ĐÃ QUÉT

### Danh mục A: Rủi ro thấp (Low-Risk / Cleanable)
*Bao gồm các tệp tin tạm phát sinh trong quá trình sử dụng hệ điều hành hoặc AutoCAD đã cũ hơn 24 giờ, tệp cache staging update.*
- **Số lượng phát hiện:** 14 tệp tạm trong `%TEMP%` (`TFR3126.tmp`, `tmp*.tmp`, `~DFC*.TMP`).
- **Dung lượng đo được:** **0.36 MB** (379,904 bytes).
- **Trạng thái hành động đề xuất:** Đủ điều kiện cách ly an toàn khi người dùng bấm duyệt trên DTT TOOLS Manager.

### Danh mục B: Cần người dùng xem xét (Review-Required)
*Bao gồm các sản phẩm biên dịch tạm của C#/MSBuild và các tệp nén sao lưu lịch sử của dự án.*
- **Thư mục Build tạm (bin / obj / .dotnet):**
  - `bin\`: 90.24 MB (Các tệp DLL/PDB trung gian của các lần build trước).
  - `obj\`: 1.52 MB (Tệp cache biên dịch MSBuild).
  - `.dotnet\`: 148.11 MB (SDK / NuGet temporary caches).
  - `dist_2027\bin\`: 1.00 MB.
  - *Ghi chú:* Toàn bộ các thư mục này có thể tự động tái tạo 100% bằng lệnh `powershell -File scripts/build-release.ps1`.
- **Tệp nén sao lưu lịch sử (_backup_*.zip):**
  - `_backup_dtt_tools_20261028_195049.zip`: 45.47 MB (Mốc sao lưu toàn diện trước nâng cấp v2026.2.0 - **BẮT BUỘC GIỮ LẠI**).
  - `_backup_pre_refactor_20261027_013741.zip`: 42.61 MB (Bản backup cũ hơn).
- **Tổng dung lượng nhóm này:** **328.94 MB**.

### Danh mục C: Bảo vệ tuyệt đối (Strictly Protected - Không đụng đến)
- Toàn bộ thư mục mã nguồn: `src/`, `Modules/`, `UI/`, `Common/`, `tests/`, `Build/`.
- Thư viện ký hiệu & định nghĩa: `symbols_database.json`, `Blocks/`.
- Bản vẽ CAD mẫu: `DTT_Tools.bundle/Contents/samples/`.
- Gói phát hành chính thức: `dist/release/` (`DTT_Tools_Setup.exe`, `DTT_Manager.exe`, `DTT_Updater.exe`, `DTT_AutoCAD_Plugin.dll`).

---

## 3. TỔNG HỢP DUNG LƯỢNG ĐO ĐẠC

| Nhóm dữ liệu | Số lượng mục | Dung lượng đo được | Hành vi xử lý mặc định | Khả năng hoàn tác |
| :--- | :---: | :---: | :---: | :---: |
| **User %TEMP% & Staging** | 14 tệp | **0.36 MB** | Đề xuất chuyển Quarantine | 100% phục hồi được |
| **MSBuild Build Cache (bin/obj)** | 4 thư mục | **240.87 MB** | Giữ nguyên, chỉ xóa khi User duyệt | Tái tạo qua lệnh build |
| **Project Zip Archives** | 2 tệp | **88.08 MB** | Giữ bản mới nhất | Lưu trữ lâu dài |
| **TỔNG CỘNG ĐO ĐƯỢC** | **20 mục** | **329.30 MB** | **100% An Toàn** | **Bảo tồn toàn diện** |

---

## 4. HƯỚNG DẪN THỰC THI & HOÀN TÁC (ROLLBACK)

### Cách 1: Sử dụng giao diện DTT TOOLS Manager (Khuyến nghị)
1. Khởi chạy `DTT_Manager.exe` (từ Desktop shortcut hoặc trong thư mục `dist/release/DTT_Manager.exe`).
2. Nhấp vào tab **🛡️ Dọn Dẹp An Toàn**.
3. Bấm nút **[🔍 Quét Bộ Nhớ Đệm]** để xem danh sách thời gian thực.
4. Bấm nút **[🛡️ Cách Ly Vào Quarantine]** để di chuyển các tệp an toàn.
5. Nếu cần phục hồi, bấm nút **[↩️ Khôi Phục Từ Quarantine]**.

### Cách 2: Sử dụng dòng lệnh PowerShell
- **Quét kiểm tra (Không thay đổi hệ thống):**
  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/maintenance/scan-dtt-storage.ps1
  ```
- **Mô phỏng xử lý (-WhatIf):**
  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/maintenance/clean-approved-items.ps1
  ```
- **Thực thi cách ly sau khi phê duyệt:**
  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/maintenance/clean-approved-items.ps1 -Execute
  ```
- **Hoàn tác (Rollback):**
  Truy cập thư mục `%LocalAppData%\DToolsPro\Quarantine\` và sao chép các tệp về lại thư mục ban đầu.

