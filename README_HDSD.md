# ⚡ DTools Pro v3.5 - HƯỚNG DẪN SỬ DỤNG BỘ CÔNG CỤ TỦ ĐIỆN VẠN NĂNG & THƯ VIỆN ĐIỆN

> **Tên chính thức:** DTools Pro  
> **Phiên bản:** v3.5 PRO  
> **Nâng cấp mới:** 
> 1. Sửa triệt để lỗi Thư Viện Ký Hiệu Điện bị trống (Đầy đủ 4 danh mục IEC/TCVN).
> 2. Tùy chỉnh Cỡ chữ Kích thước Dim (Dim Text Height & Scale 1:1 chống phóng to).
> 3. Tự động dựng Mô hình 3D Solid Tủ Điện & Xem 3D Quick View (SE Isometric / Conceptual).
> 4. Xuất Bóc Tách Vật Tư Sang File Excel (.xlsx) Tự Động Định Dạng Chuyên Nghiệp.  
> **Tương thích:** AutoCAD 2020, 2021, 2022, 2023, 2024, 2025, 2026 (64-bit)  
> **Kiến trúc:** 100% C# .NET Native, OpenXML Excel Native, WPF Dynamic Floating Dialog  

---

## 🚀 1. CÀI ĐẶT 1-CLICK VỚI FILE SETUP .EXE

1. Mở thư mục `dist\`, nhấp đúp file:
   👉 **`Setup_DTools_Pro.exe`**
2. Nhấp nút **[⚡ CÀI ĐẶT NGAY]** -> Trình cài đặt tự động nạp `DTT_Tools.bundle` vào `Autodesk\ApplicationPlugins`.
3. Mở AutoCAD lên:
   - Thanh công cụ **DTools Pro Palette** tự động xuất hiện.
   - Thư viện ký hiệu và toàn bộ tính năng 3D, bóc tách Excel sẵn sàng sử dụng 100%!

---

## 🌟 2. CHI TIẾT 4 TÍNH NĂNG NÂNG CẤP MỚI

### 1️⃣ THƯ VIỆN KÝ HIỆU ĐIỆN PRO (SỬA DỨT ĐIỂM LỖI GIAO DIỆN BỊ TRỐNG)
- **Lệnh mở nhanh:** `DTT_ELEC_MENU` hoặc bấm nút trên Palette: `⚡ Menu Thư Viện Ký Hiệu Điện PRO`.
- **4 Danh mục chuẩn hóa:**
  1. **Ổ CẮM ĐIỆN (OUTLET):** Ổ cắm đơn, đôi, chống nước IP55, âm sàn, 3 pha công nghiệp.
  2. **ĐÈN CHIẾU SÁNG (LIGHT):** Đèn Downlight LED, đèn tuýp đôi/đơn, đèn Panel âm trần, Exit lối thoát hiểm, đèn sự cố.
  3. **CÔNG TẮC & BẢO VỆ (SWITCH):** Công tắc 1-2-3 hạt, công tắc đảo chiều, Aptomat MCB 1P/2P/3P, MCCB 3P, RCCB chống giật, Cầu chì Fuse, Rơ-le bảo vệ, Tiếp địa Earth.
  4. **TỦ ĐIỆN & THIẾT BỊ ĐIỀU KHIỂN (PANEL):** Tủ DB, MDB, ATS, Biến tần VFD, Khởi động từ Contactor, Nút dừng khẩn cấp E-Stop, Đèn báo pha L1-L2-L3, Bộ lưu điện UPS, Ắc quy Battery.
- **Cơ chế nạp tự động:** File `symbols_database.json` nằm cùng thư mục plugin được đọc tự động. Các Icon hiển thị rõ ràng, kèm tên tiếng Việt và thông số kỹ thuật.
- **Thao tác chèn:** Nhấp vào nút ký hiệu -> Click điểm đặt trên CAD -> Block chuẩn vector được chèn chính xác.

### 2️⃣ TÙY CHỈNH CỠ CHỮ KÍCH THƯỚC DIMENSION (DIM TEXT HEIGHT SCALE)
- **Vị trí cài đặt:** Tại **Tab 1 [Kích thước & Góc nhìn]** của Form "Menu Vẽ Tủ Điện Vạn Năng" (`DTT_CABINET`):
  - Ô nhập: **"Chiều cao chữ Dim (mm):"** (Mặc định = 25 mm).
  - Ô nhập: **"Tỉ lệ DimScale (1:1):"** (Mặc định = 1.0).
- **Thuật toán sinh Dim:** Gán trực tiếp `dim.Dimtxt = cfg.DimTextHeight`, `dim.Dimscale = cfg.DimScale`, `dim.Dimtad = 1`. 
- **Kết quả:** Con số đo kích thước (1200, 800, 300, 100...) hiển thị nhỏ gọn tỉ lệ 1:1, tự động thụt lề offset cân đối, tuyệt đối không bị phóng to khổng lồ đè lên hình vẽ kỹ thuật!

### 3️⃣ TỰ ĐỘNG DỰNG & XEM MÔ HÌNH 3D TỦ ĐIỆN (3D CABINET BUILDER)
- **Lệnh kích hoạt:** 
  - `DTT_CABINET_3D` hoặc bấm nút **[🧊 DỰNG 3D SOLID]** trực tiếp trong Menu Tủ Điện.
  - Lệnh chuyển góc nhìn: `DTT_3D_VIEW`.
  - Lệnh quay lại 2D: `DTT_2D_VIEW`.
- **Thuật toán dựng 3D Solid:**
  - **Chân đế (Plinth):** Khối hộp 3D Solid bằng thép dập màu xám đậm RAL 7024.
  - **Thân vỏ tủ (Body Hollow Box):** Tạo khối Outer Box và trừ khối Inner Cavity bằng giải thuật Boolean `Subtract`, tạo khoang rỗng chuẩn xác độ dày tôn 1.5 - 2.0mm.
  - **Tấm gá linh kiện (Mounting Plate):** Tấm tôn mạ kẽm màu cam/vàng gá sát lưng tủ.
  - **Dàn thanh ray nhôm DIN Rail 3D:** Gá đều các hàng theo cấu hình.
  - **MCCB 3D Khối Động Lực:** Vỏ màu đỏ/đen kèm cần gạt toggle 3D.
  - **Dàn MCB nhánh & Cầu đấu dây TB 3D:** Phân bố đều trên các thanh ray.
  - **Cánh tủ & Đèn báo pha 3D:** Khối cánh mặt trước kèm các cụm đèn báo hình nón Frustum 3D đỏ/vàng/xanh.
- **Chuyển Viewport tức thì:** Tự động xoay sang góc nhìn không gian **3D SE Isometric** và áp dụng Visual Style **Conceptual/Shaded** để người dùng có thể quan sát, xoay 3D Orbit trực quan.

### 4️⃣ MODULE BÓC TÁCH VẬT TƯ SANG FILE EXCEL (.XLSX)
- **Lệnh kích hoạt:** `DTT_EXPORT_BOM` hoặc `DTT_BOM` hoặc bấm nút **[📊 Xuất BOM Sang Excel (.xlsx)]** trên Palette.
- **Thuật toán BOM Scanner:** Quét toàn bộ linh kiện điện, block thuộc tính trong bản vẽ CAD hiện hành, tự động đọc mã hiệu, tên, tag nhãn và thông số, gộp nhóm và đếm số lượng.
- **Cơ chế xuất Excel chuẩn OpenXML:** Sử dụng engine nén `ZipArchive` (.NET Framework 4.8 native), tạo file `.xlsx` định dạng bảng biểu văn phòng chuẩn không cần cài MS Excel Interop.
- **Cấu trúc bảng kê vật tư:**
  - Tiêu đề: *BẢNG TỔNG HỢP BÓC TÁCH VẬT TƯ & LINH KIỆN TỦ ĐIỆN*.
  - Các cột: **STT | Tên Linh Kiện / Vật Tư | Ký Hiệu Nhãn (Tag) | Thông Số Kỹ Thuật | Đơn Vị | Số Lượng | Ghi Chú**.
  - Dòng **TỔNG CỘNG** ở cuối bảng.
  - Độ rộng các cột được tự động căn chỉnh (Auto-fit).
  - Tự động mở ngay file Excel sau khi xuất xong lên màn hình!
### 5️⃣ MODULE VẼ MẶT BẰNG ĐIỆN 2D PRO (M&E FLOOR PLAN BUILDER)
- **Lệnh mở:** `DTT_ELEC_FLOORPLAN` hoặc `DTT_FLOORPLAN` hoặc nút **[🏠 Vẽ Mặt Bằng Điện 2D Full Tính Năng]** trên Palette.
- **5 Tab tính năng chuyên nghiệp:**
  1. **Tab 1: Kiến Trúc Phòng & Tường (Architecture & Walls):**
     - Hỗ trợ đa dạng hình dáng: Phòng Chữ nhật, Phòng Chữ L, Phòng Chữ T, hoặc chế độ **Pick Polyline ranh giới bất kỳ** trên bản vẽ AutoCAD.
     - Tùy chỉnh độ dày tường bao: 220mm (tường chịu lực / tường bao ngoài) hoặc 110mm (tường ngăn chia phòng).
     - Trổ cửa đi tự động: Cửa 1 cánh mở xoay, Cửa 2 cánh, Cửa lùa (Sliding Door).
     - Trổ cửa sổ tự động (Window) 4 lớp nét kiến trúc.
  2. **Tab 2: Chiếu Sáng & Rải Lưới Đèn (Lighting System):**
     - Chủng loại đèn: Downlight LED (Âm trần), Đèn tuýp LED đôi, Đèn Panel 600x600, Đèn thoát hiểm khẩn cấp EXIT.
     - 2 chế độ rải đèn:
       - Theo **Số lượng cố định (Hàng x Cột)**.
       - Theo **Độ rọi tiêu chuẩn (Target Lux)**: Tự động tính toán theo công thức quang thông $N = \frac{E \cdot S}{F \cdot U \cdot K}$ và rải đều trên lưới tọa độ tâm phòng.
  3. **Tab 3: Ổ Cắm & Động Lực (Power & Outlets):**
     - Rải ổ cắm chân tường tự động (chạy dọc chu vi tường với khoảng cách thiết lập, tự động né cửa đi và cửa sổ).
     - Tùy chọn đặt nhanh các phụ tải động lực: Điều hòa không khí (AC), Quạt trần (Ceiling Fan), Bình nước nóng lạnh (Water Heater).
  4. **Tab 4: Điện Nhẹ ELV (Extra Low Voltage):**
     - Chèn nhanh các đầu ra hạ tầng: Ổ cắm mạng Data LAN RJ45, Ổ cắm Tivi (Coaxial/TV), Đầu báo khói báo cháy (Smoke Detector), Camera an ninh quan sát (CCTV).
  5. **Tab 5: Đi Dây & Tủ DB (Wiring & Distribution Board):**
     - Đặt công tắc điện 1-3 hạt hoặc đảo chiều ngay cạnh cửa đi.
     - Tự động đi dây nét đứt cong Arc Bezier (`Bulge`) hoặc Orthogonal từ công tắc đến các bóng đèn.
     - Gom nhóm lộ tuyến nguồn và tự động vẽ mũi tên chỉ hướng về Tủ điện phân phối (`Home-run Arrow` có nhãn N1, N2...).
     - Chèn Block Tủ điện tổng (DB). Khi người dùng nhấp vào Block này, có thể kích hoạt trực tiếp sang Module Vẽ Tủ Điện Vạn Năng.
- **Xem trước trực quan thời gian thực (Live Vector Preview Canvas):** Toàn bộ kích thước tường, cửa, đèn, ổ cắm, tủ điện được vẽ mô phỏng trực tiếp trên giao diện trước khi xuất ra CAD.

---

### 6️⃣ THƯ VIỆN PLC I/O PARAMETRIC (PARAMETRIC PLC BUILDER)
- **Lệnh mở:** `DTT_PLC_BUILDER` hoặc `DTT_PLC` hoặc nút **[🎛️ Mô-đun PLC I/O]** trên Palette.
- **Hỗ trợ các dòng PLC công nghiệp phổ biến nhất:**
  - Siemens SIMATIC S7-1200 (CPU 1214C/1215C)
  - Siemens SIMATIC S7-1500 (CPU 1511/1513)
  - Mitsubishi MELSEC iQ-F FX5U (FX5U-32M/64M)
  - Omron Sysmac CP1H (CP1H-XA/X)
  - Schneider Electric Modicon M221
- **Thuật toán sinh khối PLC tham số hóa:**
  - Tự động vẽ khối sơ đồ nguyên lý với đầy đủ:
    - Cọc cấp nguồn (24VDC / 220VAC, Mass / PE).
    - Cổng mạng công nghiệp PROFINET / Ethernet RJ45.
    - Đèn LED trạng thái Run/Stop/Error/Maint.
    - Cọc tín hiệu Đầu vào số (Digital Input: I0.0 - I0.7 / X0 - X7).
    - Cọc tín hiệu Đầu ra số (Digital Output: Q0.0 - Q0.7 / Y0 - Y7).
    - Cọc tín hiệu tương tự (Analog AI 0..10V / 4..20mA, Analog AO).
  - Khối PLC tự động co giãn chiều cao theo số lượng điểm I/O người dùng nhập.

---

### 7️⃣ SMART SYNC (TỰ ĐỘNG ĐỒNG BỘ SƠ ĐỒ NGUYÊN LÝ <-> MẶT TỦ PANEL LAYOUT)
- **Lệnh mở:** `DTT_SMART_SYNC` hoặc `DTT_SYNC_PANEL` hoặc qua Tab 2 của PLC Builder Dialog.
- **Thuật toán đối chiếu và xếp tủ thông minh (Component Sync Engine):**
  - Quét tự động toàn bộ bản vẽ để bóc tách các linh kiện có trong Sơ đồ nguyên lý (Schematics): PLC, Biến tần VFD, Aptomat khối MCCB, MCB tép nhánh, Khởi động từ Contactor, Rơ-le kiếng, Nguồn 24VDC, Cầu đấu Terminal...
  - Bảng đối chiếu thời gian thực hiển thị danh sách linh kiện chưa xếp vào tủ.
  - Bấm nút **[⚡ TỰ ĐỘNG XẾP VÀO PANEL]**:
    - Tool tự động đọc kích thước thực tế ($W \times H$) của từng linh kiện từ cơ sở dữ liệu `symbols_database.json`.
    - Tự động sinh Tấm Panel tôn mạ kẽm và máng cáp lược bao quanh 4 cạnh.
    - Tự động bố trí 4 tầng thanh ray nhôm DIN Rail theo quy chuẩn tủ điện chuyên nghiệp:
      - **Tầng 1 (Đỉnh tủ):** Khí cụ động lực chính (MCCB nguồn tổng, Bộ điều khiển PLC, Biến tần VFD).
      - **Tầng 2:** Dàn Aptomat nhánh tép (MCB 1P, 2P, 3P).
      - **Tầng 3:** Khí cụ điều khiển & chuyển mạch (Contactor động cơ, Rơ-le kiếng trung gian).
      - **Tầng 4 (Đáy tủ):** Khối cầu đấu xuất tuyến (Terminal Blocks TB) ra động cơ và cảm biến.
---

### 8️⃣ SƠ ĐỒ HÀNG KẸP TERMINAL (TERMINAL STRIP BUILDER)
- **Lệnh mở:** `DTT_TERMINAL_STRIP` hoặc `DTT_TERMINAL` hoặc bấm nút **[⛓️ Hàng Kẹp Cầu Đấu]** trên Palette.
- **Tính năng nổi bật:**
  - Tự động sinh bảng hàng kẹp cầu đấu chuẩn IEC (-X1, -X2, -TB1...).
  - Hiển thị chi tiết 5 cột thông số:
    1. **Thiết bị trong tủ:** Đọc nguồn xuất tuyến (ví dụ: `KM1:2`, `PLC:Q0.0`, `BAR_PE`...).
    2. **Số dây trong tủ:** Nhãn dây nội bộ (`U1`, `V1`, `W1`, `101`, `24V`, `0V`, `PE`).
    3. **Chân kẹp Terminal:** Biểu tượng ốc vít tròn kèm số thứ tự (1, 2, 3, 4...). Tự động vẽ thanh Jumper liên kết các chân kẹp chung nguồn.
    4. **Cáp ra ngoài tủ:** Tiết diện và mã cáp hiện trường (`W1: 4x4.0 mm²`, `W2: 2x1.0 mm²`...).
    5. **Thiết bị hiện trường:** Đích đến ngoại vi ngoài tủ (`M1 Động cơ Bơm`, `Y1 Van điện từ`, `S1 Nút dừng khẩn`...).

---

### 9️⃣ ĐÁNH SỐ DÂY THÔNG MINH TỰ ĐỘNG (SMART WIRE NUMBERING)
- **Lệnh mở:** `DTT_SMART_WIRENUM` hoặc `DTT_WIRENUM_AUTO` hoặc bấm nút **[🔢 Đánh Số Dây Auto]** trên Palette.
- **Tính năng nổi bật:**
  - Quét toàn bộ mạch điện, tự động bảo tồn và giữ nguyên các dây nguồn cố định: `L1`, `L2`, `L3`, `N`, `PE`, `24V+`, `24V-`, `0V`, `GND`, `COM`.
  - Tự động đánh số liên tục cho các dây tín hiệu / điều khiển: `101`, `102`, `103`...
  - Tự động xoay góc chữ theo phương ngang hoặc đứng của dây, đặt chính xác tại tâm đoạn dây, không bao giờ bị đè lên ký hiệu khí cụ điện.

---

### 🔟 THAM CHIẾU CHÉO TIẾP ĐIỂM (RELAY CROSS-REFERENCE)
- **Lệnh mở:** `DTT_CROSS_REF` hoặc `DTT_RELAY_XREF` hoặc bấm nút **[🪞 Tham Chiếu Chéo]** trên Palette.
- **Tính năng nổi bật:**
  - Tự động dò tìm các Cuộn hút (Coil) của Contactor/Rơ-le (`KM1`, `KM2`, `KA1`...) và các Tiếp điểm phụ NO/NC trên bản vẽ.
  - Tự động vẽ **Bảng Gương Tiếp Điểm (Contact Mirror)** ngay dưới chân cuộn hút theo chuẩn IEC 61082 / DIN EN 81346:
    - Tiếp điểm Thường Mở NO (13-14): Hiển thị vị trí tọa độ lưới (ví dụ: `/1.3`).
    - Tiếp điểm Thường Đóng NC (21-22): Hiển thị vị trí tọa độ lưới (ví dụ: `/2.5`).

---

### 1️⃣1️⃣ BỘ MẪU MẠCH KHỞI ĐỘNG ĐỘNG CƠ (MOTOR STARTER GENERATOR)
- **Lệnh mở:** `DTT_MOTOR_STARTER` hoặc `DTT_STARTER` hoặc bấm nút **[⚙️ Mạch Khởi Động Động Cơ]** trên Palette.
- **Hỗ trợ 4 kiểu khởi động phổ biến nhất:**
  1. **Khởi động Trực tiếp (DOL Starter)**: 0.75 - 15 kW.
  2. **Khởi động Sao - Tam giác (Star-Delta Starter)**: 11 - 75 kW.
  3. **Biến tần điều khiển tốc độ (VFD Inverter)**.
  4. **Khởi động mềm (Soft Starter)** cho động cơ công suất lớn.
- **Tự động tính toán & chọn linh kiện từ công suất P (kW):**
  - Nhập kW -> Tự tính dòng định mức $I_{dm}$, tự động tra bảng chọn:
    - Cỡ Aptomat MCCB/MCB
    - Cỡ Khởi động từ Contactor chính, sao, tam giác
    - Dải chỉnh định Rơ-le nhiệt (Thermal Overload Relay)
    - Tiết diện cáp cấp nguồn $Cu/PVC$ ($2.5 - 70\text{ mm}^2$).
- **Sinh trọn bộ bản vẽ:**
  - **Mạch Động Lực (Power Circuit):** Nguồn 3 pha, CB, Contactor, Rơ-le nhiệt, Động cơ M1.
  - **Mạch Điều Khiển (Control Circuit):** Nguồn điều khiển 220VAC/24VDC, Tiếp điểm sự cố 95-96, Nút STOP, Nút START, Tiếp điểm duy trì 13-14, Cuộn hút A1-A2, Đèn báo RUN (xanh), Đèn TRIP (vàng).
  - Bảng tra cứu thông số kỹ thuật (Spec Sheet) đính kèm bản vẽ.

---

### 1️⃣2️⃣ KIỂM LỖI THIẾT KẾ MẠCH ĐIỆN TỰ ĐỘNG (ELECTRICAL DRC INSPECTOR)
- **Lệnh mở:** `DTT_ELEC_DRC` hoặc `DTT_DRC` hoặc bấm nút **[🛡️ Kiểm Lỗi Mạch DRC]** trên Palette.
- **Các quy tắc kiểm tra (Design Rules):**
  1. **Trùng nhãn thiết bị (Duplicate Tag Collision):** Phát hiện 2 linh kiện độc lập mang cùng ký hiệu nhãn (ví dụ 2 cái cùng tên `QF1` hoặc `KM1`).
  2. **Đầu dây hở mạch (Floating Pin / Open Circuit):** Phát hiện các đoạn dây điện có đầu mút lơ lửng, không đấu nối vào cọc hoặc thiết bị nào.
  3. **Kiểm tra phụ tải:** Cảnh báo nguy cơ quá tải trên nhánh cấp nguồn.
- **Báo cáo trực quan:**
  - Hộp thoại liệt kê danh sách lỗi chi tiết theo cấp độ (Error: Đỏ / Warning: Vàng).
  - Nút **[🔍 KHOANH VÙNG LỖI TRÊN CAD]**: Tự động vẽ các đám mây cảnh báo đỏ (Revision Cloud) và cờ nhãn lỗi tại các vị trí vi phạm trên bản vẽ CAD để kỹ sư khoanh vùng sửa lỗi tức thì.

---

## 📋 3. BẢNG TỔNG HỢP CÁC LỆNH CAD CHÍNH

| Lệnh CAD | Nhóm | Mô tả chi tiết |
|---|---|---|
| `DTT` hoặc `DTOOLS` | **Core** | Bật / tắt thanh công cụ DTools Pro Palette |
| `DTT_ELEC_FLOORPLAN` / `DTT_FLOORPLAN` | **Mặt Bằng Điện** | **Vẽ Mặt Bằng Điện 2D Full Tính Năng (Tường, Đèn, Ổ Cắm, ELV, Dây Nối, Zoom/3D Preview)** |
| `DTT_TERMINAL_STRIP` / `DTT_TERMINAL` | **Hàng Kẹp** | **Tự động sinh sơ đồ hàng kẹp Terminal Strip X1/X2 hiển thị chi tiết 5 cột** |
| `DTT_SMART_WIRENUM` / `DTT_WIRENUM_AUTO` | **Đánh Số Dây** | **Đánh số thứ tự dây tự động theo chuỗi, bảo tồn nguồn cố định** |
| `DTT_CROSS_REF` / `DTT_RELAY_XREF` | **Tham Chiếu Chéo** | **Tự động dò và sinh bảng gương tiếp điểm NO/NC dưới chân cuộn hút Rơ-le** |
| `DTT_MOTOR_STARTER` / `DTT_STARTER` | **Mạch Động Cơ** | **Bộ mẫu mạch khởi động động cơ (DOL, Sao-Tam giác, VFD, Soft Starter) chuẩn IEC** |
| `DTT_ELEC_DRC` / `DTT_DRC` | **Kiểm Lỗi DRC** | **Quét và phát hiện lỗi trùng Tag, hở mạch dây điện, khoanh vùng đám mây đỏ** |
| `DTT_PLC_BUILDER` / `DTT_PLC` | **Tự Động Hóa** | **Khởi tạo Khối Mô-đun PLC I/O Tham Số Động (Siemens, Mitsu, Omron...)** |
| `DTT_SMART_SYNC` / `DTT_SYNC_PANEL` | **Đồng Bộ Tủ** | **Smart Sync: Quét Sơ Đồ Nguyên Lý -> Tự Động Xếp Vào Mặt Panel Tủ** |
| `DTT_CABINET_BUILDER` / `DTT_CABINET` | **Tủ Điện** | **Mở Bộ Thiết Kế & Sinh Tủ Điện Vạn Năng Tham Số Động** |
| `DTT_CABINET_3D` | **Tủ Điện 3D** | Tự động dựng mô hình 3D Solid Tủ Điện |
| `DTT_3D_VIEW` / `DTT_2D_VIEW` | **Góc Nhìn** | Chuyển góc nhìn 3D SE Isometric / Quay về 2D Top View |
| `DTT_EXPORT_BOM` / `DTT_BOM` | **Vật Tư** | Xuất bảng bóc tách khối lượng vật tư sang Excel (.xlsx) |
| `DTT_ELEC_MENU` | **Ký Hiệu** | Menu Thư Viện Ký Hiệu Điện TCVN / IEC PRO |
| `DTT_SCAN_TO_CAD` / `DTT_RASTER_TO_CAD` | **AI Vision & Scan** | **Quét bản vẽ (ảnh scan/PDF) và tự động dựng lại nét vẽ dạng CAD (DWG) bằng AI Vision & Computer Vision** |
| `DTT_STANDARD_FLOOR_PLAN` | **Kiến Trúc** | **Tự động sinh Mặt bằng tầng điển hình: 4 phòng, cột BTCT, buồng thang, cửa, cửa sổ, 3 lớp Dim** |
| `DTT_HOUSE_PLAN` / `DTT_ARCH_PLAN` | **Kiến Trúc** | **Vẽ Mặt Bằng Nhà Kiến Trúc & Bộ Preview 2D/3D Tương Tác (Tường, Cột, Cửa, Trục, 3 Lớp Dim, MEP)** |
| `DTT_CABLE_CALC` | **Tính Cáp** | **Tính chọn tiết diện cáp Cadivi & kiểm tra độ sụt áp % theo TCVN / IEC** |
| `DTT_SLD_BUILDER` | **Sơ Đồ Đơn Tuyến** | **Sinh Sơ đồ nguyên lý đơn tuyến (Single Line Diagram) tự động cho Tủ điện tổng MSB** |
| `DTT_STAIRCASE` | **Cầu Thang** | **Vẽ Cầu thang phong thủy kiến trúc 2 vế chữ U (17, 21, 25 bậc Cung Sinh) có chiếu nghỉ & tim đi** |
| `DTT_FURNITURE` | **Nội Thất** | **Tự động bố trí trọn bộ nội thất hoàn thiện (Khách, Bếp, Giường Master, WC, Làm việc)** |
| `DTT_PLUMBING` | **Cấp Thoát Nước** | **Vẽ tuyến ống cấp nước sạch PPR (lạnh D25/nóng D20) và ống thoát uPVC (D90/D114 có độ dốc)** |
| `DTT_HVAC_DUCT` | **Thông Gió HVAC** | **Vẽ tuyến ống gió 2D cấp/hồi/thải SAD/RAD/EAD (kèm nhãn WxH và tim nét tâm)** |
| `DTT_HVAC_DIFFUSER` | **Miệng Gió** | **Chèn miệng gió khuếch tán 4 hướng 600x600 kèm lưu lượng CFM** |
| `DTT_RENUMBER_SHEETS` | **Quản Lý Bản Vẽ** | **Tự động quét khung tên từ trên xuống dưới, trái sang phải để đánh số bản vẽ (E-01, E-02...)** |
| `DTT_BATCH_PLOT` | **In Ấn Hàng Loạt** | **Tự động quét khung bản vẽ Model/Layout và in hàng loạt sang PDF (DWG To PDF)** |
| `DTT_BOQ_FULL_EXPORT` / `DTT_BOQ_EXPORT` | **Dự Toán BOQ** | **Quét toàn diện bản vẽ, xuất file Excel CSV dự toán và chèn Bảng Table BOQ vào CAD** |
| `DTT_REBARTABLE` | **Kết Cấu** | **Xuất Bảng Thống Kê Thép Tự Động Quét Bản Vẽ (Tính toán Kg theo TCVN 5574:2018)** |
| `DTT_BEAMSEC` | **Kết Cấu** | Vẽ mặt cắt dầm BTCT kèm bố trí thép và xem trước |
| `DTT_DRAINPIPE` | **MEP** | Vẽ ống thoát nước tính độ dốc qua Floating Dialog |
| `DTT_FIXFONT` | **Bảo Dưỡng** | Quét & sửa lỗi hiển thị tiếng Việt trên toàn bản vẽ |
| `DTT_FLATTEN` | **Bảo Dưỡng** | Đưa toàn bộ cao độ Z về 0 phẳng tuyệt đối |

---

## 🛠️ 4. QUẢN TRỊ & NÂNG CẤP DỰ ÁN DÀNH CHO DEVELOPER

- **Biên dịch mã nguồn & đóng gói installer mới:**
  ```powershell
  python Build\build_all.py
  ```
- **File đầu ra:**
  - `dist\Setup_DTools_Pro.exe` (File cài đặt độc lập nén tích hợp)
  - `dist\DTT_AutoCAD_Plugin.dll` (File thư viện plugin AutoCAD .NET 4.8)
  - `dist\symbols_database.json` (Cơ sở dữ liệu ký hiệu điện và linh kiện tủ điện)
  - `DTT_Tools.bundle\` (Gói Autodesk Autoloader Bundle)
