# PROJECT_IDENTITY.md — XÁC MINH DANH TÍNH DỰ ÁN VÀ CẤU TRÚC ĐẦU RA

## 1. Xác Nhận Dự Án Gốc Duy Nhất
- **Đường dẫn thư mục dự án**: `C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin`
- **Solution File**: `DTools.sln`
- **Core C# Project**: `DTT_AutoCAD_Plugin.csproj` (Target Framework: `net48`)
- **Core Sub-projects**:
  - `src\DTools.Core\DTools.Core.csproj` (net48 / netstandard2.0)
  - `src\DTools.Application\DTools.Application.csproj` (net48 / netstandard2.0)
- **Tên Assembly Lõi**: `DTT_AutoCAD_Plugin.dll`
- **Hệ thống nạp Autoloader**: `DTT_Tools.bundle\PackageContents.xml`

## 2. Thư Mục Đầu Ra Thực Tế (DIST)
- **Đường dẫn phân phối**: `C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\dist`
- **Các file EXE trong dist**:
  1. `Setup_DTools_Pro.exe` (43,677,184 bytes | SHA-256: `78E62BFBD32A9A11EB5D3F02FA66EF4DDCC3797B79882CD860616EBD8D1B1C29`)
  2. `Setup_DTT_Tools.exe` (43,677,184 bytes | SHA-256: `78E62BFBD32A9A11EB5D3F02FA66EF4DDCC3797B79882CD860616EBD8D1B1C29`)
  3. `DTools.exe` (26,624 bytes | SHA-256: `B1A81E44D6BBCE42758F0EAD3FECFA1F6A3CFC6E020266234A886CD5A732B579` - File utility độc lập)
  4. `DTools.Preview.exe` (26,624 bytes | SHA-256: `B1A81E44D6BBCE42758F0EAD3FECFA1F6A3CFC6E020266234A886CD5A732B579` - Bản xem trước)
- **Assembly Core trong dist**:
  - `DTT_AutoCAD_Plugin.dll` (767,488 bytes | SHA-256: `FE43FDCF0905684683E449DE059868721D18334B8F736BD3DC6CC2E9070080B8` | ProductVersion: `2026.2.0.0`)
- **Các thư mục tài nguyên trong dist**:
  - `dist\samples\`: Chứa 3 bản vẽ mẫu DWG và PDF kỹ thuật (`01_Mat_bang_chieu_sang_12x6`, `02_Khoi_dong_truc_tiep_DOL`, `03_Khoi_dong_sao_tam_giac`) và project file `DTools_SmartElectrical_Sample.wdp`.
  - `dist\Blocks\`: Chứa các dynamic blocks CAD.
  - `dist\symbols_database.json`: Cơ sở dữ liệu ký hiệu điện và kiến trúc.

## 3. Khẳng Định Tuân Thủ Tuyệt Đối
- **TUYỆT ĐỐI KHÔNG** can thiệp vào các đường dẫn cũ: `D:\VSTUDIO FILE\DTools`, `DTools(2).zip`, `DTools(3).zip`.
- **TUYỆT ĐỐI KHÔNG** tạo một dự án DTools độc lập bên ngoài rồi xuất ZIP.
- Mọi sửa đổi được thực hiện trực tiếp trong `C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin`.
- Các file EXE trong `dist` đã được biên dịch lại thực sự qua `csc.exe` nhúng payload zip chứa toàn bộ bundle và plugin DLL mới nhất.
