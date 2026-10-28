# CHANGELOG.md — LỊCH SỬ THAY ĐỔI VÀ NÂNG CẤP DTT_AUTOCAD_PLUGIN

## Phiên Bản: 2026.2.0 (Phát hành ngày 27/10/2026)

### 1. Tính Năng Mới — Phân Hệ Điện Thông Minh (Electrical Native-First)
- **`DTT_ELECTRICAL`**: Bảng điều khiển trung tâm thiết kế điện thông minh, tích hợp sẵn điều hướng trực quan bằng Tiếng Việt.
- **`DTT_E_LIGHTING`**: Tự động dựng mặt bằng chiếu sáng phòng 12m x 6m:
  - Phân định rõ ràng kích thước lọt lòng (12.000 x 6.000 mm) và kích thước phủ bì tường 200 mm (12.400 x 6.400 mm).
  - Bố trí 02 đèn trần tại (4000, 3000) và (10000, 3000), 01 công tắc 2 nút, 01 cửa đi 2 cánh, 03 cửa sổ.
  - Vẽ đường dây nét đứt (`DTT_WIRING`, lineType: `DASHED`) kết nối từ công tắc đến các đèn.
  - Ghi kích thước chuẩn mét `12m` và `6m`.
- **`DTT_E_DOL`**: Mạch khởi động trực tiếp 3 pha (DOL):
  - Mạch động lực: 3 pha L1, L2, L3, N, MCB2, Contactor MC1, Rơ le nhiệt TH1, Động cơ 3 pha M1.
  - Mạch điều khiển: Tiếp điểm quá tải NC TH1 (95-96), Nút dừng STOP (NC 11-12), Nút mở START (NO), Cuộn hút MC1 (A1-A2).
  - Tiếp điểm duy trì tự giữ: Mặc định chuẩn IEC 60947 tiếp điểm NO (13-14). Cung cấp tùy chọn tham chiếu ảnh đề bài (21-22) kèm cảnh báo lỗi kỹ thuật.
- **`DTT_E_STARDELTA`**: Mạch khởi động Sao - Tam giác (Y - Δ):
  - Động cơ 6 đầu ra U1-V1-W1 và U2-V2-W2.
  - Contactor chính MC2, Contactor Tam giác MC3 (đấu chéo pha), Contactor Sao MC4 (chụm 3 đầu sao).
  - Timer T1 (5 giây) điều khiển chuyển mạch.
  - Liên động kép: Khóa chéo điện qua tiếp điểm NC của MC3 và MC4 + Khóa liên động cơ khí giữa MC3 và MC4.
  - Mô phỏng FSM Dead-time 50ms chống ngắn mạch hồ quang giữa Sao và Tam giác.
- **`DTT_E_AUDIT`**: Tự động gọi công cụ kiểm toán `AEAUDIT` của AutoCAD Electrical nếu có, hoặc chạy kiểm toán nội bộ DTT.
- **`DTT_DIAG`**: Chẩn đoán toàn diện môi trường host AutoCAD Electrical, phiên bản, thư viện IEC-60617 và file cấu hình Circuit Builder.

### 2. Sửa Lỗi Khởi Động & Tối Ưu Hóa Hiệu Năng
- **Loại bỏ nguy cơ Fatal Error `e0434352h`**:
  - Không khởi tạo WPF Palette hoặc quét Database trong hàm `IExtensionApplication.Initialize`.
  - Chuyển toàn bộ cơ chế nạp sang Lazy Loading (chỉ mở Palette khi người dùng gọi lệnh).
  - Thay thế các đường dẫn hardcode `C:\Users\Duong\...` thành đường dẫn động lấy từ `%LOCALAPPDATA%` và thư mục cài đặt bundle.

### 3. Đóng Gói Bộ Cài Đặt Mới Trong Thư Mục DIST
- Tái tạo thành công các file EXE cài đặt trong `dist`:
  - `Setup_DTools_Pro.exe` (43,677,184 bytes)
  - `Setup_DTT_Tools.exe` (43,677,184 bytes)
- Nhúng toàn bộ bundle `DTT_Tools.bundle` phiên bản mới nhất, chứa DLL `DTT_AutoCAD_Plugin.dll` (767,488 bytes), thư viện block và mẫu bản vẽ DWG/PDF.
- Tích hợp tính năng tự động phát hiện và cách ly bundle cũ (`DTools.bundle`) sang thư mục Quarantine an toàn trước khi nạp bản mới.

### 4. Giữ Nguyên Tính Tương Thích Ngược
- Toàn bộ các lệnh kiến trúc, kết cấu, MEP và tiện ích cũ của DTT (`DTT`, `DTT_WALL`, `DTT_DOOR`, `DTT_FIXFONT`, `DTT_FLATTEN`...) được giữ nguyên vẹn 100%.
