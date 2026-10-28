# BUILD_REPORT.md — BÁO CÁO BIÊN DỊCH VÀ ĐÓNG GÓI THÀNH PHẨM

## 1. Môi Trường Và Công Cụ Biên Dịch
- **Hệ điều hành**: Windows 11 / Windows 10 x64
- **CLI Compiler**: `dotnet build` (.NET SDK 8.0/10.0 tương thích MSBuild)
- **Framework Compiler**: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (Dùng đóng gói installer WinForms standalone)
- **Target Framework**: .NET Framework 4.8 (`net48`)
- **Autodesk Reference**: AutoCAD 2021 R24.0 API (`accoremgd.dll`, `acdbmgd.dll`, `acmgd.dll`) từ `D:\Program Files\Autodesk\AutoCAD 2021\`

## 2. Lệnh Build Đã Thực Thi
1. Biên dịch toàn bộ Solution:
   ```cmd
   dotnet build "C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\DTools.sln" -c Release
   ```
   **Kết quả**: `Build succeeded. 0 Warning(s), 0 Error(s)`.
   Sinh file: `bin\Release\net48\DTT_AutoCAD_Plugin.dll` (771,072 bytes).

2. Đóng gói Payload Bundle:
   Nén thư mục `DTT_Tools.bundle` (chứa `PackageContents.xml`, DLL, samples, blocks, lisp) thành file `bundle_payload.zip` (43,660,407 bytes).

3. Biên dịch Trình Cài Đặt Tự Động EXE:
   ```cmd
   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:"C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\dist\Setup_DTools_Pro.exe" /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /resource:"bundle_payload.zip",BundlePayload "Build\Installer\SetupProgram.cs"
   ```
   Sao chép sang `dist\Setup_DTT_Tools.exe`.

## 3. Danh Mục Chi Tiết File Đầu Ra Tại Thư Mục DIST

| Tên File | Loại File | Kích Thước | Phiên Bản | Thời Điểm Tạo | SHA-256 Hash |
|---|---|---|---|---|---|
| `Setup_DTT_Tools.exe` | WinForms Installer GUI | 43,679,744 bytes | 0.0.0.0 | 2026-10-27 11:39:49 | `3EB879CB11F3A1A09B7DFAECC4911B389B58A372BADE13DA4A6FF7DCADE37187` |
| `Setup_DTools_Pro.exe` | WinForms Installer GUI | 43,679,744 bytes | 0.0.0.0 | 2026-10-27 11:39:49 | `3EB879CB11F3A1A09B7DFAECC4911B389B58A372BADE13DA4A6FF7DCADE37187` |
| `DTT_AutoCAD_Plugin.dll` | AutoCAD .NET Plugin Assembly | 771,072 bytes | 2026.2.0.0 | 2026-10-27 11:39:03 | `861BAF780BFA591B8F285B0667E54CEEED3DAE1CDD96D8F816FB5FCE7296F88D` |
| `DTools.Core.dll` | Thư viện lõi Core | 25,600 bytes | 2026.2.0.0 | 2026-10-27 11:04:14 | `867E7401E2166BD4F8B0105B0F5B0DDC8062BA18F547B8EECAD8A0517A9DA384` |
| `DTools.Application.dll` | Lớp ứng dụng | 7,168 bytes | 2026.2.0.0 | 2026-10-27 11:04:14 | `88CECA295E6A50557D4677F4C2524F13BCF8D3F41FADBB756C500A6FCA114225` |
| `DTools.exe` | Utility Standalone | 26,624 bytes | 1.0.0.0 | 2026-10-27 10:47:27 | `B1A81E44D6BBCE42758F0EAD3FECFA1F6A3CFC6E020266234A886CD5A732B579` |
| `DTools.Preview.exe` | Preview Standalone | 26,624 bytes | 1.0.0.0 | 2026-10-27 10:47:27 | `B1A81E44D6BBCE42758F0EAD3FECFA1F6A3CFC6E020266234A886CD5A732B579` |
| `DTools.Tests.exe` | Test Runner | 9,728 bytes | 0.0.0.0 | 2026-10-27 10:30:26 | `154D9DD9D61CB6287865E72509CB5016DAEC0A775B3D7978F831DE84123B501D` |
| `symbols_database.json` | Dữ liệu ký hiệu | 28,013 bytes | N/A | 2026-10-27 11:34:58 | `43A146E8E0F4A9AC46B16F3DA06A6D72C3DB36687A25782E764C432E1D11AAE1` |

## 4. Xác Minh Tính Toàn Vẹn Của Bộ Cài Đặt (Payload Matching)
Đã chạy kiểm thử giải nén tự động từ file installer `Setup_DTT_Tools.exe /silent`:
- File cài đặt đích: `%APPDATA%\Autodesk\ApplicationPlugins\DTT_Tools.bundle\Contents\DTT_AutoCAD_Plugin.dll`
- SHA-256 sau cài đặt: `861BAF780BFA591B8F285B0667E54CEEED3DAE1CDD96D8F816FB5FCE7296F88D`
- SHA-256 file build trong dist: `861BAF780BFA591B8F285B0667E54CEEED3DAE1CDD96D8F816FB5FCE7296F88D`
- **Kết luận**: Khớp 100%. Bộ cài EXE trong dist thực sự mang toàn bộ tính năng và mã nguồn mới nhất!
