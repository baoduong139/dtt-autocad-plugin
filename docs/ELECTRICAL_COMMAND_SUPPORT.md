# ELECTRICAL_COMMAND_SUPPORT.md — KHẢO SÁT VÀ TÍCH HỢP LỆNH AUTOCAD ELECTRICAL NATIVE

## 1. Môi Trường Host Thực Tế Đã Xác Minh
- **Phần mềm host**: AutoCAD Electrical 2021 (x64)
- **Đường dẫn cài đặt gốc**: `D:\Program Files\Autodesk\AutoCAD 2021\`
- **Thư viện chuẩn ký hiệu**: `C:\Users\Public\Documents\Autodesk\Acade 2021\Libs\iec-60617\` (IEC 60617)
- **File cấu hình Circuit Builder**: `C:\Users\Public\Documents\Autodesk\Acade 2021\Support\en-US\ace_circuit_builder.xls`
- **Nền tảng .NET Framework**: .NET Framework 4.8 x64 (Tương thích 100% với CLR 4.0 và AutoCAD 2020-2026)

## 2. Bảng Danh Mục Lệnh Native Khảo Sát & Trạng Thái Hỗ Trợ

| Lệnh Native AutoCAD Electrical | Ý Nghĩa / Mục Đích | Trạng Thái Trên Máy | Cơ Chế Tích Hợp Của DTT | Ghi Chú Tương Tác |
|---|---|---|---|---|
| `AECIRCBUILDER` | Mở Circuit Builder sinh mạch tự động | **CÓ SẴN** | Hỗ trợ cấu hình XLS & template DWG | Mở hộp thoại UI gốc của Autodesk khi cần tinh chỉnh |
| `AESAVEDCIRCUIT` | Chèn mạch đã lưu (.dwg) vào bản vẽ | **CÓ SẴN** | Đã tạo sẵn 3 circuit DWG chuẩn trong `dist\samples\` | Chèn trực tiếp vào tọa độ chỉ định |
| `AESAVECIRCUIT` | Lưu cụm mạch hiện tại thành Saved Circuit | **CÓ SẴN** | Hỗ trợ tạo khối WBLOCK mạch chuẩn ACADE | Yêu cầu người dùng chọn vùng bao quanh |
| `AEWBCIRCUIT` | Chèn sơ đồ từ WBLOCK mạch | **CÓ SẴN** | Nạp khối DWG định nghĩa sẵn | Tự động căn chỉnh điểm chèn |
| `AECOMPONENT` | Chèn linh kiện schematic từ catalog | **CÓ SẴN** | Tham chiếu thư viện `Libs\iec-60617` | Lấy mã catalog chuẩn |
| `AEWIRE` | Vẽ dây điện thông minh có kết nối | **CÓ SẴN** | Tự động đặt layer dây theo chuẩn ACADE | Tự động sinh giao điểm (wire crossing) |
| `AECONNECTCOMP` | Nối dây tự động giữa các linh kiện | **CÓ SẴN** | Tự động tìm connection points | Giảm thiểu thao tác kéo dây thủ công |
| `AEMULTIBUS` | Tạo bus nguồn 3 pha đa nhánh | **CÓ SẴN** | Tạo bus 3 pha L1, L2, L3, N, PE | Dùng trong mạch động lực DOL & Sao-Tam giác |
| `AELADDER` | Tạo thang điều khiển (Control Ladder) | **CÓ SẴN** | Thiết lập bước thang điều khiển | Chuẩn hóa mạch điều khiển dọc |
| `AEWIRENO` | Tự động đánh số dây điện | **CÓ SẴN** | Kích hoạt bộ đánh số dây dự án | Đánh số dây duy nhất cho toàn bộ project |
| `AEXREF` | Tham chiếu chéo tiếp điểm/cuộn hút (Cross-Ref) | **CÓ SẴN** | Liên kết cuộn hút MC1/MC2 với tiếp điểm NO/NC | Tự động ghi vị trí tọa độ tiếp điểm |
| `AEAUDIT` | Kiểm toán Electrical Audit phát hiện lỗi | **CÓ SẴN** | Tích hợp trong lệnh `DTT_E_AUDIT` | Tự động báo cáo dây hở, chân lỗi |

## 3. Không Ghi Đè Lệnh Autodesk — Namespace DTT Độc Lập
Theo quy định an toàn nghiêm ngặt, DTT **tuyệt đối không** tạo các attribute `[CommandMethod]` trùng tên với lệnh gốc của Autodesk. Thay vào đó, DTT sử dụng namespace lệnh độc lập:
- `DTT_ELECTRICAL`: Bảng điều khiển trung tâm điện thông minh.
- `DTT_E_LIGHTING`: Mặt bằng chiếu sáng phòng 12m x 6m (Kích thước lọt lòng 12000x6000, tường 200mm, dim mét `12m`/`6m`, 2 đèn trần, công tắc, dây nét đứt).
- `DTT_E_DOL`: Mạch khởi động trực tiếp 3 pha (Chuẩn IEC 60947 tiếp điểm tự giữ NO 13-14 và cảnh báo chế độ ảnh tham khảo 21-22).
- `DTT_E_STARDELTA`: Mạch Sao - Tam giác (Động cơ 6 cực U1-V1-W1/U2-V2-W2, khóa chéo điện NC MC3/MC4, khóa liên động cơ khí, FSM Dead-time 50ms chống ngắn mạch hồ quang).
- `DTT_E_AUDIT`: Kiểm toán an toàn mạch điện (Tự động kích hoạt AEAUDIT nếu có host ACADE hoặc chạy bộ kiểm toán nội bộ DTT).
- `DTT_DIAG`: Chẩn đoán toàn diện môi trường host, thư viện IEC và tình trạng autoloader.
