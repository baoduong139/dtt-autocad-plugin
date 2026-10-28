# HƯỚNG DẪN CÀI ĐẶT & SỬ DỤNG DTOOLS PRO CHO AUTOCAD
**Phiên bản**: 2026.2.0 (Bản Nâng Cấp Tối Ưu Cho Máy 8GB RAM)  
**Tương thích**: Autodesk AutoCAD 2024, 2025, 2026 (Windows 64-bit)

---

## 1. Yêu Cầu Hệ Thống Tối Thiểu
- **Hệ điều hành**: Windows 10 (64-bit) hoặc Windows 11 (64-bit).
- **Phần cứng đề xuất**: Laptop Dell Vostro 5620 hoặc máy tính tương đương:
  - CPU: Intel Core i5 / AMD Ryzen 5 trở lên.
  - RAM: Tối thiểu 8 GB (Đã kích hoạt chế độ **Lite Mode** tối ưu bộ nhớ).
  - Ổ cứng: Trống tối thiểu 150 MB cho thư viện Block và tệp tin cài đặt.
- **Phần mềm nền tảng**: Đã cài đặt Autodesk AutoCAD 2024, 2025 hoặc 2026.

---

## 2. Phương Pháp Cài Đặt (3 Cách Tiện Lợi)

### Cách 1: Sử Dụng Trình Cài Đặt Tự Động (Khuyến Nghị Cho Người Dùng)
1. Tải về hoặc mở thư mục `dist/Setup_DTools_Pro.exe`.
2. Nhấp đúp chuột vào tệp `Setup_DTools_Pro.exe`.
3. Giao diện Wizard cài đặt tiếng Việt xuất hiện -> Nhấn nút **[⚡ CÀI ĐẶT DTOOLS PRO NGAY]**.
4. Trình cài đặt sẽ tự động:
   - Dò tìm thư mục AutoCAD Plugins (`%APPDATA%\Autodesk\ApplicationPlugins`).
   - Sao lưu cấu hình cũ (nếu có).
   - Giải nén toàn bộ gói `DTT_Tools.bundle` vào đúng vị trí tiêu chuẩn của Autodesk.
5. Khởi động phần mềm AutoCAD -> Bảng công cụ **DTools Pro** sẽ tự động hiển thị neo ở mép phải màn hình!

---

### Cách 2: Cài Đặt Thủ Công Theo Tiêu Chuẩn Autodesk Autoloader Bundle
Nếu bạn không muốn chạy tệp thực thi `.exe`:
1. Nhấn tổ hợp phím `Windows + R`, gõ `%APPDATA%\Autodesk\ApplicationPlugins` và nhấn `Enter`.
2. Sao chép toàn bộ thư mục `DTT_Tools.bundle` từ mã nguồn vào thư mục vừa mở.
3. Cấu trúc thư mục đúng sẽ như sau:
   ```text
   %APPDATA%\Autodesk\ApplicationPlugins\
   └── DTT_Tools.bundle\
       ├── PackageContents.xml
       └── Contents\
           ├── DTT_AutoCAD_Plugin.dll
           ├── symbols_database.json
           └── Blocks\
               ├── Architectural - Metric.dwg
               ├── Electrical - Metric.dwg
               └── ...
   ```
4. Khởi động AutoCAD, plugin sẽ tự động nạp mà không cần gõ lệnh.

---

### Cách 3: Nạp Nhanh Bằng Lệnh `NETLOAD` Trong Phiên Làm Việc Hiện Tại
Dành cho chuyên viên kỹ thuật hoặc kiểm thử nhanh:
1. Mở AutoCAD và mở một bản vẽ bất kỳ.
2. Trên thanh dòng lệnh Command của AutoCAD, gõ lệnh: `NETLOAD` và nhấn `Enter`.
3. Duyệt tìm đến tệp `DTT_AutoCAD_Plugin.dll` tại thư mục `dist/` hoặc `bin/Release/net48/`.
4. Nhấn **Open**.
5. Gõ lệnh **`DTT`** hoặc **`DTOOLS`** để mở bảng điều khiển Palette.

---

## 3. Các Lệnh Thường Dùng Nhất (Quick Reference)

| Lệnh Gõ CAD | Chức Năng Chính | Hướng Dẫn Thao Tác Nhanh |
| :--- | :--- | :--- |
| **`DTT`** hoặc **`DTOOLS`** | Bật / Tắt bảng điều khiển DTools Pro | Mở Palette neo bên phải màn hình. |
| **`DT_FAST_PLACE`** | Chèn cấu kiện liên tục | Chọn khối, sau đó click chuột liên tục trên CAD để đặt hàng loạt vị trí. Nhấn Enter/Esc để xong. |
| **`DTT_ELEC_MENU`** | Thư viện 58 ký hiệu & khí cụ điện | Mở menu chọn ổ cắm, công tắc, đèn, tủ điện, PLC. |
| **`DTT_PLACE_SYMBOL`** | Chèn ký hiệu điện đang chọn | Click điểm đặt ký hiệu trực tiếp trên bản vẽ. |
| **`DTT_WALL220`** / **`110`** | Vẽ tường đôi 220mm / tường đơn 110mm | Nhập 2 điểm đầu - cuối để vẽ tường song song chuẩn layer. |
| **`DTT_DOOR`** / **`DT_DOOR`** | Chèn cửa đi thông phòng | Nhập bề rộng cửa (D800, D900, D1200) và click vị trí trên tường. |
| **`DT_CLEAN`** | Dọn dẹp & Tối ưu bản vẽ DWG | Tự động Purge đối tượng rác và Audit sửa lỗi bản vẽ. |
| **`DTT_TLEN`** | Đo tổng chiều dài các đoạn dây / tường | Quét chọn các nét vẽ, kết quả tổng chiều dài (mm & m) báo ngay trên CAD. |
| **`DTT_TAREA`** | Tính tổng diện tích các phòng | Quét chọn các hình kín, tự động cộng tổng diện tích ($m^2$). |

---

## 4. Hướng Dẫn Gỡ Cài Đặt (Uninstall)
- Để gỡ bỏ DTools Pro:
  1. Đóng phần mềm AutoCAD.
  2. Vào thư mục: `%APPDATA%\Autodesk\ApplicationPlugins\`
  3. Xóa thư mục `DTT_Tools.bundle`.
  4. Bản vẽ và dữ liệu người dùng tại `%LOCALAPPDATA%\DToolsPro` vẫn được giữ an toàn tuyệt đối.
