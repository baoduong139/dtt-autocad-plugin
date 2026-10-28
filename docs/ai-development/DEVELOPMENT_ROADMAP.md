# DTT CAD MASTER - LỘ TRÌNH PHÁT TRIỂN (DEVELOPMENT ROADMAP)

Lộ trình nâng cấp dài hạn biến DTT Tools thành bộ công cụ tự động hóa AutoCAD toàn diện nhất cho kỹ sư Việt Nam.

---

## GIAI ĐOẠN 1: ỔN ĐỊNH HỆ THỐNG & TRẢI NGHIỆM NGƯỜI DÙNG (THÁNG 10/2026)
- [x] Thống nhất phiên bản `v2026.2.0` trên toàn bộ DLL, Exe, Palette và Dialog.
- [x] Chấm dứt lỗi chèn lặp ký hiệu điện; áp dụng chế độ chèn 1 lần mặc định.
- [x] Thiết kế lại toàn diện bộ công cụ Cầu thang và Nội thất với 2D Preview chân thực.
- [x] Thiết lập hệ thống AI Autonomous Daily Development System với CI/CD tự động.

---

## GIAI ĐOẠN 2: BỘ CÔNG CỤ THIẾT KẾ KIẾN TRÚC THÔNG MINH (THÁNG 11/2026)
- [ ] **Lưới trục đa nhịp tự động (`DTT_GRID_SYSTEM`)**: Sinh toàn bộ lưới trục, đánh số và dim kích thước chỉ với 1 click.
- [ ] **Vẽ tường đôi & Cột tự động (`DTT_WALL`, `DTT_COLUMN`)**: Tự động nhận diện tim trục và bố trí kết cấu.
- [ ] **Thư viện cửa đi & Cửa sổ thông minh (`DTT_DOOR`, `DTT_WINDOW`)**: Tự động cắt nét tường khi chèn cửa.
- [ ] **Tự động đo kích thước kiến trúc (`DTT_AUTO_DIM`)**: Ghi kích thước 3 lớp (kích thước chi tiết cửa/cột, kích thước trục và kích thước phủ bì).

---

## GIAI ĐOẠN 3: BỘ CÔNG CỤ CƠ ĐIỆN & TỰ ĐỘNG HÓA CAO CẤP (THÁNG 12/2026)
- [ ] **Tính toán chiếu sáng & Phân bố đèn theo lux (`DTT_DIALUX_LITE`)**: Tự động tính số lượng đèn LED theo diện tích và công năng phòng.
- [ ] **Tự động sinh sơ đồ đơn tuyến tủ điện (`DTT_SLD_GEN`)**: Đọc bảng phụ tải và vẽ sơ đồ nguyên lý tủ điện hoàn chỉnh.
- [ ] **Báo cáo xung đột DRC mạch điện & MEP (`DTT_MEP_CLASH`)**: Phát hiện va chạm ống nước và khay cáp điện trong không gian 2D/3D.

---

## GIAI ĐOẠN 4: HỆ SINH THÁI TỰ ĐỘNG CẬP NHẬT & CLOUD SYNC (2027)
- [ ] Trình quản lý block đám mây chia sẻ chung giữa các máy trạm trong công ty.
- [ ] Tích hợp tính năng AI Scan-to-CAD nhận diện bản vẽ PDF scan sang DWG thông minh.
- [ ] Hỗ trợ đa phiên bản AutoCAD từ AutoCAD 2020 đến AutoCAD 2027+.

