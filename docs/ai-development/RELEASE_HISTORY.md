# DTT TOOLS - LỊCH SỬ PHÁT HÀNH (RELEASE HISTORY)

Theo dõi chi tiết các phiên bản phát hành chính thức của DTT CAD MASTER / DTools Pro.

---

## BẢNG TỔNG HỢP PHIÊN BẢN

| Phiên Bản | Ngày Phát Hành | Kênh | Gói Cài Đặt / Update | Trạng Thái | Thay Đổi Trọng Tâm |
|:---|:---:|:---:|:---|:---:|:---|
| **v2026.2.0** | 29/10/2026 | Stable | `dist/update.zip` | Đang hoạt động | Thiết kế lại Preview Cầu thang & Nội thất 2D thật, cơ chế chèn 1 ký hiệu điện duy nhất, thống nhất phiên bản toàn hệ thống. |
| **v2026.1.0** | 26/10/2026 | Stable | `dist/update.zip` | Đã lưu trữ | Khởi tạo cấu trúc module Kiến trúc, Kết cấu, Điện công nghiệp và Quản lý Layer/Block. |

---

## CHI TIẾT PHIÊN BẢN v2026.2.0

- **Thời gian**: 29/10/2026 06:05:00 UTC+7
- **Đóng gói**:
  - `DTT_AutoCAD_Plugin.dll`: Assembly plugin chính cho AutoCAD 2020 - 2026.
  - `DTools.Core.dll` & `DTools.Application.dll`: Thư viện tính toán hình học, điện, kết cấu và dự toán.
  - `DTT_Manager.exe`: Ứng dụng điều hành và quản lý bản quyền ngoài desktop.
  - `DTT_Updater.exe`: Trình cập nhật tự động độc lập chống xung đột khi AutoCAD đang chạy.
  - `symbols_database.json`: Danh mục 100+ ký hiệu điện chuẩn IEC / TCVN.
- **Tính năng nổi bật**:
  1. Cửa sổ **Bố trí nội thất tự động**: Thay thế hoàn toàn vòng tròn màu xanh bằng mặt bằng vector 2D chi tiết top-down; bổ sung kích thước phòng, khoảng lùi tường và nút chọn ranh giới phòng từ CAD.
  2. Cửa sổ **Vẽ cầu thang phong thủy**: Tính bậc Cung Sinh, hiển thị live chiều cao cổ bậc $h = H / N$, kiểm tra công thức tiện nghi Blondel $2h + b \approx 600\text{--}650\text{ mm}$, hỗ trợ thang chữ U, chữ I và chữ L.
  3. **Kho ký hiệu điện**: Giao diện thẻ trực quan không cắt chữ, mặc định chèn đúng 1 ký hiệu duy nhất; bổ sung nút `⚡ CHÈN 1 KÝ HIỆU RA CAD` và checkbox `Chèn liên tục`.
  4. **Tối ưu RAM 8GB**: Cache thumbnail bằng `BitmapImage.Freeze()`, an toàn rò rỉ GDI/HBitmap.

