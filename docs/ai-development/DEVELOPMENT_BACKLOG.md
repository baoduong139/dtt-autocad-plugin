# DTT CAD MASTER / DTOOLS PRO - DEVELOPMENT BACKLOG

Hệ thống backlog phát triển tự động của DTT Tools Pro. Được cập nhật và phân loại liên tục bởi AI Autonomous Development System.

---

## 1. PHÂN LOẠI & MỨC ĐỘ ƯU TIÊN

| Độ Ưu Tiên | Ý Nghĩa | SLA Mục Tiêu |
|:---|:---|:---|
| **P0 - Khẩn cấp (Critical)** | Sửa lỗi crash CAD, treo DocumentLock, lỗi chèn nhiều đối tượng, lỗi mất bản vẽ | Trong ngày |
| **P1 - Cao (High)** | Tính năng kiến trúc/cơ điện trọng điểm, tăng tốc độ thao tác người dùng x5 | 1 - 2 ngày |
| **P2 - Trung bình (Medium)** | Tối ưu UI/UX, hoàn thiện Preview, chuẩn hóa font tiếng Việt và hiển thị màn hình 8GB RAM | 3 - 5 ngày |
| **P3 - Thấp (Low)** | Mở rộng thư viện khối mẫu, bổ sung định dạng xuất báo cáo phụ | Backlog dài hạn |

---

## 2. DANH SÁCH HẠNG MỤC CẦN PHÁT TRIỂN (BACKLOG)

### A. Công Cụ Vẽ Nhanh & Biên Tập 2D (Fast Drafting)
- [x] **DF-01**: Tích hợp các lệnh CAD 2D cơ bản (LINE, PLINE, CIRCLE, RECTANG, OFFSET, TRIM, EXTEND, ROTATE, COPY, MOVE) lên bảng Palette trung tâm. *(Hoàn thành v2026.2.0)*
- [ ] **DF-02**: Lệnh `DTT_QUICK_OFFSET` - Offset song song 2 phía tự động tạo tim trục hoặc bề dày tường. *(Ưu tiên P1)*
- [ ] **DF-03**: Lệnh `DTT_FILLET_ALL` - Bo tròn hoặc vát mép hàng loạt góc của Polyline kín trong một thao tác. *(Ưu tiên P2)*
- [ ] **DF-04**: Lệnh `DTT_SUPER_TRIM` - Cắt tỉa nét thừa thông minh cắt qua một hàng đối tượng chỉ với 1 đường quét Fence. *(Ưu tiên P1)*

### B. Module Kiến Trúc (Architectural Suite)
- [x] **AR-01**: Vẽ cầu thang phong thủy chuẩn 2D với công thức Blondel $2h + b$ và đánh số bậc tự động. *(Hoàn thành v2026.2.0)*
- [x] **AR-02**: Bố trí trọn bộ nội thất kiến trúc tự động (Phòng khách, Ngủ master, Bếp ăn, WC, Phòng làm việc) với Preview 2D thật 1-1. *(Hoàn thành v2026.2.0)*
- [ ] **AR-03**: Lệnh `DTT_GRID_SYSTEM` - Sinh hệ thống lưới trục đa nhịp tự động (X: 1, 2, 3.. / Y: A, B, C..) kèm bong bóng trục 2 đầu và dim kích thước tự động. *(Ưu tiên P0 - Đang triển khai)*
- [ ] **AR-04**: Lệnh `DTT_WALL_AUTO` - Tự động tạo nét tường đôi từ tim trục hoặc tim polyline với bề dày 110mm / 220mm và xử lý giao điểm chữ T, chữ L. *(Ưu tiên P1)*
- [ ] **AR-05**: Lệnh `DTT_COLUMN_GRID` - Tự động đặt cột bê tông cốt thép vuông/tròn tại tất cả giao điểm lưới trục chỉ với 1 click. *(Ưu tiên P1)*

### C. Module Điện Dân Dụng & Công Nghiệp (Electrical & Automation Suite)
- [x] **EL-01**: Kho thư viện ký hiệu điện chuẩn IEC / TCVN, thẻ danh mục responsive không bị cắt chữ. *(Hoàn thành v2026.2.0)*
- [x] **EL-02**: Cơ chế chèn chuẩn 1 ký hiệu duy nhất (Single Placement Default), ngăn chặn lặp vô tận. *(Hoàn thành v2026.2.0)*
- [ ] **EL-03**: Đánh số dây thông minh tự động `DTT_SMART_WIRENUM` theo quy tắc phân pha L1-L2-L3-N-PE và tín hiệu điều khiển. *(Ưu tiên P1)*
- [ ] **EL-04**: Kiểm tra xung đột & DRC mạch điện `DTT_ELEC_DRC` - Phát hiện chân thiết bị chưa nối, dây điện hở mạch, trùng tên cuộn hút Contactor. *(Ưu tiên P1)*
- [ ] **EL-05**: Tự động sinh sơ đồ đơn tuyến tủ điện phân phối chiếu sáng & ổ cắm (Single Line Diagram - SLD) từ bảng tính công suất. *(Ưu tiên P1)*

### D. Module Cơ Điện & Cấp Thoát Nước (MEP Suite)
- [ ] **MEP-01**: Tự động vẽ tuyến ống nước cấp / thoát kèm phụ kiện (Co, Lơ, Tê, Giảm) và độ dốc tiêu chuẩn 2%. *(Ưu tiên P2)*
- [ ] **MEP-02**: Thống kê danh mục thiết bị và đường ống xuất trực tiếp ra bảng Table trong AutoCAD và file CSV / Excel. *(Ưu tiên P1)*

### E. Giao Diện Người Dùng & Tương Thích (UI/UX & Platform)
- [x] **UI-01**: Tắt popup trung tâm công cụ khi mở CAD, giữ lại Palette bên trái làm bảng điều khiển trung tâm duy nhất. *(Hoàn thành v2026.2.0)*
- [x] **UI-02**: Thay thế hình tròn placeholder màu xanh bằng Canvas vector 2D CAD Preview top-down thực thụ. *(Hoàn thành v2026.2.0)*
- [x] **UI-03**: Thống nhất phiên bản hiển thị thành `v2026.2.0` trên toàn bộ UI, Menu và DLL. *(Hoàn thành v2026.2.0)*
- [ ] **UI-04**: Bổ sung bộ lọc tìm kiếm tức thì theo từ khóa không dấu tiếng Việt trong bảng ký hiệu. *(Ưu tiên P2)*

---

*Tài liệu tự động duy trì bởi DTT AI Development Engine. Cập nhật ngày 29/10/2026.*

