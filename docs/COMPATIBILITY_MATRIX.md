# MA TRẬN TƯƠNG THÍCH DTOOLS PRO (COMPATIBILITY MATRIX)

## 1. Nguyên Tắc Kiến Trúc Đa Nền Tảng (Multi-Targeting Principles)
AutoCAD không duy trì tính tương thích nhị phân (binary compatibility) xuyên suốt qua các đợt chuyển đổi CLR (Common Language Runtime) lớn của Microsoft:
- **AutoCAD 2020 – 2024**: Chạy trên **.NET Framework 4.8**.
- **AutoCAD 2025 – 2026**: Chuyển sang **.NET 8.0 (Windows Desktop)**.
- **AutoCAD 2026 (Update 1.2+) & AutoCAD 2027**: Chuyển sang **.NET 10.0 (Windows Desktop)**.

Do đó, DTools Pro áp dụng kiến trúc **Lõi Độc Lập (Core Decoupled)**:
1. `DTools.Core` & `DTools.Application`: Biên dịch dưới dạng thư viện dùng chung cho tất cả các bản AutoCAD.
2. `DTools.AutoCAD.Host`: Phân tách các adapter tương ứng với từng phiên bản AutoCAD SDK.
3. **KHÔNG** đóng gói đè DLL Autodesk (`Acdbmgd.dll`, `acmgd.dll`, `accoremgd.dll`) vào gói cài đặt (`CopyLocal = false`).

---

## 2. Bảng Ma Trận Tương Thích Chi Tiết

| Phiên Bản AutoCAD | Bản Cập Nhật (Build) | CLR Runtime Mục Tiêu | SDK API Autodesk Tham Chiếu | Trạng Thái Kiểm Chứng | Ghi Chú Kỹ Thuật |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **AutoCAD 2024** | 24.3.0 (Bản gốc & Update) | `.NET Framework 4.8` | `AutoCAD.NET 24.3.0` | **Đã kiểm chứng & Build thành công (Active)** | Mục tiêu chính trên máy thử nghiệm hiện tại. Đã test NETLOAD & Command. |
| **AutoCAD 2025** | 25.0.0+ | `.NET 8.0-windows` | `AutoCAD.NET 25.0.0` | **Sẵn sàng mã nguồn (Code-Ready Adapter)** | Yêu cầu .NET 8 Runtime trên máy người dùng. Cấu hình SeriesMin R25.0. |
| **AutoCAD 2026** | Update 1.0 – 1.1 | `.NET 8.0-windows` | `AutoCAD.NET 25.1.0` | **Sẵn sàng mã nguồn (Code-Ready Adapter)** | Tương tự bản 2025. |
| **AutoCAD 2026** | Update 1.2+ | `.NET 10.0-windows` | `AutoCAD.NET 25.2.0` | **Đã thiết lập cấu hình build (.NET 10)** | .NET SDK 10.0.402 đã có sẵn trên máy phát triển. |
| **AutoCAD 2027** | Pre-release / 2027 RTM | `.NET 10.0-windows` | `AutoCAD.NET 26.0.0` | **Cần xác thực SDK thực tế (Unverified)** | AutoCAD 2027 yêu cầu tối thiểu 16 GB RAM chính thức. Lite Mode giúp giảm tải plugin. |

---

## 3. Cấu Hình Autodesk Autoloader (`PackageContents.xml`)

File cấu hình tự động nạp `PackageContents.xml` được phân nhánh rõ ràng theo `SeriesMin` và `SeriesMax` để AutoCAD nạp đúng runtime:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage 
    SchemaVersion="1.0" 
    AutodeskProduct="AutoCAD" 
    ProductType="Application" 
    Name="DTools Pro for AutoCAD" 
    AppVersion="2026.2.0" 
    Description="Hệ Thống Tiện Ích Kiến Trúc, Kết Cấu & Cơ Điện Tối Ưu Cho Máy 8GB RAM" 
    Author="DTT CAD Software Corporation" 
    ProductCode="{9B3C8E41-D842-4F1A-B63C-2A5F89C407F2}">

  <CompanyDetails Name="DTT CAD Software Corporation" Url="https://dtools.vn" />

  <!-- Target 1: AutoCAD 2024 (.NET Framework 4.8) -->
  <Components Description="DTools Pro .NET 4.8 Runtime Host">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD*" SeriesMin="R24.3" SeriesMax="R24.3" />
    <ComponentEntry AppName="DToolsPro_Net48" 
                    ModuleName="./Contents/DTT_AutoCAD_Plugin.dll" 
                    AppDescription="DTools Pro Plugin Engine for AutoCAD 2024" 
                    LoadOnAutoCADStartup="True">
      <Commands GroupName="DTT_COMMANDS">
        <Command Global="DTT" Local="DTT" />
        <Command Global="DTOOLS" Local="DTOOLS" />
        <Command Global="DT_FAST_PLACE" Local="DT_FAST_PLACE" />
        <Command Global="DTT_PLACE_SYMBOL" Local="DTT_PLACE_SYMBOL" />
        <Command Global="DT_CLEAN" Local="DT_CLEAN" />
      </Commands>
    </ComponentEntry>
  </Components>

</ApplicationPackage>
```

---

## 4. Phạm Vi Hỗ Trợ & Giới Hạn Nghiêm Ngặt

1. **AutoCAD LT**:
   - AutoCAD LT không hỗ trợ các API `ObjectARX` và `AutoCAD .NET Managed API` tùy biến từ bên thứ ba (trừ tính năng AutoLISP hạn chế trên các bản LT mới). DTools Pro **không** công bố hỗ trợ AutoCAD LT.
2. **AutoCAD trên macOS**:
   - AutoCAD for Mac không hỗ trợ giao diện người dùng WPF (Windows Presentation Foundation) và không dùng chuẩn Windows Forms. DTools Pro chỉ hỗ trợ Windows 64-bit (Windows 10, Windows 11).
3. **Các phần mềm CAD khác (BricsCAD, ZWCAD, GstarCAD)**:
   - Các phần mềm này cần bản build riêng (`BrxMgd`, `ZwSoft.ZwCAD.Runtime`, `GrxMgd`). Không tự nhận tương thích nhị phân khi chưa được liên kết SDK tương ứng.
