# BÁO CÁO AUDIT TOÀN DIỆN DTT_AutoCAD_Plugin.dll & CODEBASE DTOOLS PRO

## 1. Thông Tin Nhị Phân & Assembly Metadata
- **Tên tệp gốc**: `DTT_AutoCAD_Plugin.dll`
- **Dung lượng**: ~738.304 bytes (~721 KiB)
- **Assembly Name**: `DTT_AutoCAD_Plugin`
- **Target Runtime Framework**: `.NETFramework,Version=v4.8`
- **Assembly Version**: `2026.1.0.0`
- **Tham chiếu API Autodesk AutoCAD**:
  - `Acdbmgd.dll` (v24.3.0.0 — Họ AutoCAD 2024)
  - `accoremgd.dll` (v24.3.0.0 — Họ AutoCAD 2024)
  - `acmgd.dll` (v24.3.0.0 — Họ AutoCAD 2024)
  - Cấu hình nạp phụ thuộc: `CopyLocal = false` (Tránh đè DLL lõi AutoCAD)
- **Thành phần giao diện**:
  - `PresentationCore`, `PresentationFramework`, `WindowsBase`, `System.Xaml` (WPF .NET 4.8)
  - `WindowsFormsIntegration`, `System.Windows.Forms`, `System.Drawing` (Host Palette & WinForms Dialog)

---

## 2. Thống Kê TypeDef, MethodDef & CommandMethod
Qua phân tích reflection tĩnh và quét codebase hiện hữu:
- **Tổng số TypeDef**: 134 types (bao gồm Model, Engine, UI Windows, Manager, Utils).
- **Tổng số MethodDef**: ~1.391 methods.
- **Tổng số CommandMethod**: 156 tên lệnh được đăng ký trong hệ thống AutoCAD CommandStack.
- **Tổng số phương thức xử lý lệnh thực tế**: 93 Handler Methods.
  - *Kết luận*: 156 tên lệnh thực chất ánh xạ đến 93 tính năng cốt lõi. Khoảng 63 tên lệnh còn lại là các **alias** phục vụ tương thích ngược (ví dụ: `DT_INSERTBLOCK`, `DTT_INSERTBLOCK`, `DT_BLOCK`, `DTT_BLOCK` cùng trỏ về `CmdInsertBlock`).
  - *Vấn đề phát hiện*: 5 alias đáng ngờ bắt đầu bằng `=T_` (`=T_INSERTBLOCK`, `=T_FAST_PLACE`, `=T_DOOR`, `=T_COLUMN`, `=T_ELEVATION`). Đây là các alias sinh ra do lỗi bộ gõ tiếng Việt (Unikey/EVKey) nuốt tiền tố `\x03\x03D` biến thành `=T_`. Các alias này cần được giữ tương thích chuyển tiếp trong `docs/MIGRATION_MAP.csv` nhưng loại bỏ khỏi UI/Ribbon chính thức.

---

## 3. Phân Tích Cấu Trúc Các Tầng Nghiệp Vụ (Layer Analysis)

| Phân hệ / Module | Tệp tin đảm nhiệm chính | Số lượng Handler | Đánh giá hiện trạng & Rủi ro |
| :--- | :--- | :--- | :--- |
| **App Entry & Autoload** | `AppEntry.cs`, `PackageContents.xml` | 6 | Nạp Palette, đăng ký sự kiện Document, tích hợp menu AutoCAD. |
| **Quản lý Dynamic Blocks** | `DTT_DynamicBlockCommands.cs`, `DtoolsBlockService.cs` | 7 | Chèn cửa, cột, cao độ, đọc thuộc tính động. Trước đây gọi trực tiếp UI thread khi chèn. |
| **Kiến Trúc (Architecture)** | `ArchitectureTools.cs`, `ArchitecturalHousePlanEngine.cs` | 19 | Vẽ tường 75/100/110/220, cửa đi, cửa sổ, louver, mặt bằng, trục cột. Cần kiểm soát DocumentLock. |
| **Kết Cấu (Structure)** | `StructureTools.cs` | 6 | Uốn thép 90/135, đai thép, mặt cắt dầm, bảng thống kê. Tính toán theo công thức TCVN. |
| **Điện & Tự Động Hóa** | `ElectricalTools.cs`, `ElectricalSymbolsManager.cs`, `PlcParametricEngine.cs` | 23 | 58 ký hiệu điện IEC/TCVN, dây 3 pha, đánh số dây, PLC I/O, tủ điện DB/MDB. Đã fix lỗi trailing comma JSON. |
| **Cơ Điện (MEP & PCCC)** | `MepTools.cs`, `PlumbingAndHvacEngine.cs` | 6 | Tuyến ống thoát nước, sprinkler PCCC, ống gió HVAC. |
| **Thống Kê & Xuất Bản** | `BomExporterModule.cs`, `ComprehensiveBoqEngine.cs`, `BatchPlotAndSheetEngine.cs` | 4 | Xuất BOM ra Excel/CSV, in ấn hàng loạt, đánh số bản vẽ. Cần xử lý mã hóa UTF-8 BOM. |
| **Bảo Trì & Dọn Dẹp** | `DTT_MaintenanceTools.cs`, `DrawingDoctor.cs` | 4 | `DT_CLEAN`, `DT_QUICKLAYER`, `DT_YQREFIX`. Rủi ro xóa nhầm layer/block nếu không có Snapshot/Undo. |
| **Vision AI (Tùy chọn)** | `AiRasterToCadEngine.cs`, `DTools_AiScanToCadDialog.xaml` | 2 | Kết nối Ollama localhost:11434, vector hóa ảnh. Rất nặng nếu chạy mặc định trên máy RAM 8GB. |
| **Giao Diện WPF Palette** | `DTT_PaletteView.xaml`, `DTT_GlobalCenterModal.xaml`, `DTT_QuickMenuDialog.xaml` | 16 | Giao diện Cyberpunk/WPF. Đã gỡ bỏ modal hijacking và chuẩn hóa lệnh CAD an toàn. |

---

## 4. Các Vấn Đề Kỹ Thuật Đã Xác Thực (Verified Flaws)

1. **Vấn đề tính di động (Hardcoded Developer Path)**:
   - Trong `Modules/ElectricalSymbolsManager.cs` xuất hiện đường dẫn tuyệt đối:
     `@"C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\symbols_database.json"`
   - *Khắc phục*: Thay thế 100% bằng cơ chế `ResourceLocator` độc lập, tự động dò tìm vị trí Bundle `%APPDATA%\Autodesk\ApplicationPlugins\DTT_Tools.bundle\Contents` hoặc thư mục chạy DLL.
2. **Xung đột luồng WPF UI & AutoCAD Command Engine**:
   - Một số handler trong Dialog gọi `ed.GetPoint()` hoặc sửa `Database` trực tiếp trong sự kiện click của WPF mà không có DocumentLock hoặc không chạy trong Command Context.
   - *Khắc phục*: Tách biệt hoàn toàn, định tuyến qua `SendStringToExecute` hoặc `ElectricalPlacementContext` an toàn.
3. **Quản lý bộ nhớ máy Dell Vostro 5620 (RAM 8 GB)**:
   - Trước đây nạp toàn bộ ảnh Bitmap, 3D Mesh Engine và sẵn sàng gọi Ollama AI ngay khi khởi động. Trên máy RAM 8GB đang chạy AutoCAD, việc này dễ gây OutOfMemoryException hoặc giật đơ hệ điều hành.
   - *Khắc phục*: Thiết lập chế độ **Lite Mode** mặc định: tắt 3D & AI khi khởi động, lazy load Palette, dùng bộ nhớ đệm có giới hạn LRU Cache (tối đa 20 preview).
4. **Không đồng nhất chuỗi phiên bản**:
   - Một số dialog ghi "v3.5", tiêu đề ghi "v5.0 ULTIMATE", Assembly ghi "2026.1.0.0".
   - *Khắc phục*: Chuẩn hóa thương hiệu thành **DTools Pro (v2026.2.0)** đồng bộ từ AssemblyInfo đến giao diện người dùng.

---

## 5. Kết Luận & Định Hướng Tái Kiến Trúc
Codebase hiện có nền tảng thuật toán rất phong phú và thực chiến. Việc tái kiến trúc cần:
- Tách tầng Domain/Core thuần C# không phụ thuộc AutoCAD API để có thể viết Unit Tests và chạy `dotnet test`.
- Tách tầng UI MVVM gọn nhẹ, không dùng hiệu ứng tốn GPU.
- Giữ vững toàn bộ 156 lệnh cũ cho người dùng, bảo đảm 100% tiếng Việt có dấu chuẩn Unicode.
