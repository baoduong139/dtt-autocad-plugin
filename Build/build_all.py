#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DTT CAD TOOLSET PRO - MASTER BUILD & INSTALLER PACKAGER 2026
Biên dịch giải pháp C# .NET WPF thành DLL và đóng gói Setup_DTT_Tools.exe GUI
"""

import os
import sys
import base64
import shutil
import subprocess
from pathlib import Path

def print_banner():
    print("=" * 70)
    print("⚡ DTT CAD TOOLSET PRO - MASTER BUILD ENGINE 2026")
    print("🚀 100% C# .NET Native WPF & Autodesk Application Bundle Autoloader")
    print("=" * 70)

def run_cmd(cmd, cwd=None):
    print(f"[RUN] {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    res = subprocess.run(cmd, cwd=cwd, shell=isinstance(cmd, str))
    if res.returncode != 0:
        print(f"[ERROR] Lệnh thất bại với mã lỗi {res.returncode}")
        return False
    return True

def main():
    print_banner()
    root_dir = Path(__file__).resolve().parent.parent
    dist_dir = root_dir / "dist"
    dist_dir.mkdir(parents=True, exist_ok=True)

    # 1. BIÊN DỊCH PLUGIN C# .NET WPF
    print("\n[BƯỚC 1/4] Biên dịch dự án C# DTT_AutoCAD_Plugin.csproj...")
    dotnet = shutil.which("dotnet")
    if not dotnet:
        print("[LỖI] Không tìm thấy dotnet CLI!")
        return 1

    build_ok = run_cmd([dotnet, "build", str(root_dir / "DTools.sln"), "-c", "Release"], cwd=str(root_dir))
    if not build_ok:
        return 1

    dll_path = root_dir / "bin" / "Release" / "net48" / "DTT_AutoCAD_Plugin.dll"
    if not dll_path.exists():
        print(f"[LỖI] Không tìm thấy file DLL tại: {dll_path}")
        return 1

    print(f"[THÀNH CÔNG] File DLL đã biên dịch: {dll_path} ({dll_path.stat().st_size:,} bytes)")

    # 2. ĐÓNG GÓI VÀO DTT_Tools.bundle
    print("\n[BƯỚC 2/4] Cập nhật cấu trúc Autodesk Autoloader Bundle (DTT_Tools.bundle)...")
    bundle_dir = root_dir / "DTT_Tools.bundle"
    contents_dir = bundle_dir / "Contents"
    contents_dir.mkdir(parents=True, exist_ok=True)

    # Sao chép toàn bộ các DLL của DTools (Plugin, Core, Application)
    dll_dir = root_dir / "bin" / "Release" / "net48"
    for dll_name in ["DTT_AutoCAD_Plugin.dll", "DTools.Core.dll", "DTools.Application.dll"]:
        src_file = dll_dir / dll_name
        if src_file.exists():
            shutil.copy2(src_file, contents_dir / dll_name)
            shutil.copy2(src_file, dist_dir / dll_name)
            print(f"[OK] Đã sao chép {dll_name} vào bundle và dist ({src_file.stat().st_size:,} bytes)")

    # Sao chép symbols_database.json
    symbols_json = root_dir / "symbols_database.json"
    if symbols_json.exists():
        shutil.copy2(symbols_json, contents_dir / "symbols_database.json")
        shutil.copy2(symbols_json, dist_dir / "symbols_database.json")
        print(f"[OK] Đã sao chép symbols_database.json vào bundle & dist")

    # Sao chép thư viện Dynamic Blocks
    blocks_src = root_dir / "Blocks"
    blocks_bundle = contents_dir / "Blocks"
    blocks_dist = dist_dir / "Blocks"
    if blocks_src.exists():
        blocks_bundle.mkdir(parents=True, exist_ok=True)
        blocks_dist.mkdir(parents=True, exist_ok=True)
        for f in blocks_src.glob("*.dwg"):
            shutil.copy2(f, blocks_bundle / f.name)
            shutil.copy2(f, blocks_dist / f.name)
        print(f"[OK] Đã đồng bộ thư viện Dynamic Blocks vào {blocks_bundle} và {blocks_dist}")

    # Sao chép thư viện mẫu mạch điện DWG (DOL, Star-Delta, v.v.)
    samples_src = dist_dir / "samples"
    samples_bundle = contents_dir / "samples"
    if samples_src.exists():
        samples_bundle.mkdir(parents=True, exist_ok=True)
        for f in samples_src.glob("*.dwg"):
            shutil.copy2(f, samples_bundle / f.name)
        print(f"[OK] Đã đồng bộ thư viện mạch mẫu DWG vào {samples_bundle}")

    # 3. TẠO TRÌNH CÀI ĐẶT TỰ ĐỘNG Setup_DTT_Tools.exe / Setup_DTools_Pro.exe
    print("\n[BƯỚC 3/4] Đóng gói toàn bộ Bundle (bao gồm trọn bộ YQArch & Tiếng Việt) vào installer...")
    import zipfile

    bundle_zip = dist_dir / "bundle_payload.zip"
    if bundle_zip.exists():
        bundle_zip.unlink()

    with zipfile.ZipFile(bundle_zip, "w", zipfile.ZIP_DEFLATED) as zf:
        for p in bundle_dir.rglob("*"):
            if p.is_file():
                rel_path = p.relative_to(bundle_dir)
                zf.write(p, arcname=str(rel_path).replace("\\", "/"))

    zip_size = bundle_zip.stat().st_size
    print(f"[OK] Đã nén toàn bộ DTT_Tools.bundle thành ZIP nhúng: {zip_size:,} bytes ({zip_size/1024/1024:.2f} MB)")

    installer_src = root_dir / "Build" / "Installer" / "SetupProgram.cs"

    csc_path = Path(r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe")
    if not csc_path.exists():
        csc_path = Path(r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe")

    setup_exe = dist_dir / "Setup_DTools_Pro.exe"

    if csc_path.exists():
        csc_cmd = [
            str(csc_path),
            "/target:winexe",
            f"/out:{setup_exe}",
            "/reference:System.dll",
            "/reference:System.Windows.Forms.dll",
            "/reference:System.Drawing.dll",
            "/reference:System.IO.Compression.dll",
            f"/resource:{bundle_zip},BundlePayload",
            str(installer_src)
        ]
        ok = run_cmd(csc_cmd)
        if ok and setup_exe.exists():
            shutil.copy2(setup_exe, dist_dir / "Setup_DTT_Tools.exe")
            print(f"[THÀNH CÔNG] Đã tạo trình cài đặt độc lập: {setup_exe} ({setup_exe.stat().st_size:,} bytes)")
            print(f"[OK] Đã tạo bản sao tương thích: {dist_dir / 'Setup_DTT_Tools.exe'}")
        else:
            print("[CẢNH BÁO] Không biên dịch được qua csc.exe")
    else:
        print("[CẢNH BÁO] Không tìm thấy csc.exe của .NET Framework.")

    # Dọn file zip tạm
    if bundle_zip.exists():
        try:
            bundle_zip.unlink()
        except Exception:
            pass

    # TỔNG KẾT THÀNH PHẨM
    print("\n" + "=" * 70)
    print("🎉 TỔNG KẾT BÀN GIAO THÀNH CÔNG DỰ ÁN DTT CAD TOOLSET PRO v3.0:")
    print("=" * 70)
    print(f"📦 1. File DLL Plugin:     {dist_dir / 'DTT_AutoCAD_Plugin.dll'}")
    print(f"📁 2. Thư mục AppBundle:   {bundle_dir}")
    print(f"🚀 3. File Cài Đặt Setup:  {setup_exe}")
    print("\n👉 CÁCH SỬ DỤNG CHO NGƯỜI DÙNG CUỐI:")
    print("1. Người dùng chỉ cần nhấp đúp chạy: Setup_DTT_Tools.exe")
    print("2. Giao diện Wizard Cyberpunk xuất hiện -> Bấm [⚡ CÀI ĐẶT NGAY]")
    print("3. Trình cài đặt tự động nạp DTT_Tools.bundle vào Autodesk ApplicationPlugins.")
    print("4. Khởi động AutoCAD (2020 - 2026) -> Bảng 3D Cyberpunk Palette tự động xuất hiện neo bên phải màn hình!")
    print("5. Bấm vào bất kỳ nút nào -> Lệnh CAD thực thi lập tức 100% không cần gõ phím!")
    print("=" * 70)
    return 0

if __name__ == "__main__":
    sys.exit(main())
