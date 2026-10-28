#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DTT CAD TOOLSET 2027 - MASTER BUILD & BUNDLE PACKAGER (AUTOCAD ELECTRICAL 2027)
Biên dịch độc lập nhánh .NET 8 x64 (không ghi đè file gốc của máy chủ)
Đóng gói Standalone Bundle: DTT_Tools_2027.bundle & File Zip phát hành
"""

import os
import sys
import shutil
import zipfile
import subprocess
from pathlib import Path

def print_banner():
    print("=" * 75)
    print("⚡ DTT TOOLS 2027 - BUILD PROFILE ĐỘC LẬP CHO AUTOCAD ELECTRICAL 2027")
    print("🚀 Nền tảng: .NET 8 (net8.0-windows) | AutoCAD Series R25.0 - R25.1")
    print("🔒 Công nghệ: In-Memory Dynamic Loader & GitHub Releases Auto-Updater")
    print("=" * 75)

def run_cmd(cmd, cwd=None):
    print(f"[RUN] {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    res = subprocess.run(cmd, cwd=cwd, shell=isinstance(cmd, str))
    if res.returncode != 0:
        print(f"[LỖI] Lệnh thực thi thất bại với mã lỗi {res.returncode}")
        return False
    return True

def main():
    print_banner()
    root_dir = Path(__file__).resolve().parent.parent
    proj_2027_dir = root_dir / "DTT_AutoCAD_Plugin_2027"
    dist_2027_dir = root_dir / "dist_2027"
    bundle_dir = dist_2027_dir / "DTT_Tools_2027.bundle"
    contents_dir = bundle_dir / "Contents"

    dist_2027_dir.mkdir(parents=True, exist_ok=True)
    contents_dir.mkdir(parents=True, exist_ok=True)

    dotnet = shutil.which("dotnet")
    if not dotnet:
        print("[LỖI] Không tìm thấy dotnet CLI!")
        return 1

    # 1. BIÊN DỊCH PLUGIN CHÍNH DTT_AutoCAD_Plugin_2027.csproj
    print("\n[BƯỚC 1/5] Biên dịch DTT_AutoCAD_Plugin_2027.csproj (.NET 8)...")
    ok1 = run_cmd([dotnet, "build", str(proj_2027_dir / "DTT_AutoCAD_Plugin_2027.csproj"), "-c", "Release"], cwd=str(root_dir))
    if not ok1:
        return 1

    # 2. BIÊN DỊCH LOADER KHỞI TẠO DTT_Loader_2027.csproj
    print("\n[BƯỚC 2/5] Biên dịch DTT_Loader_2027.csproj (.NET 8 In-Memory Loader)...")
    ok2 = run_cmd([dotnet, "build", str(proj_2027_dir / "DTT_Loader_2027.csproj"), "-c", "Release"], cwd=str(root_dir))
    if not ok2:
        return 1

    bin_dir = dist_2027_dir / "bin" / "Release" / "net8.0-windows"

    # 3. ĐỒNG BỘ CÁC DLL ĐẦU RA VÀO BUNDLE CONTENTS
    print("\n[BƯỚC 3/5] Đồng bộ các tệp nhị phân vào dist_2027 và DTT_Tools_2027.bundle...")
    
    # Copy DTT_AutoCAD_Plugin_2027.dll và DTT_Loader_2027.dll
    for f in ["DTT_AutoCAD_Plugin_2027.dll", "DTT_Loader_2027.dll"]:
        src = bin_dir / f
        if src.exists():
            shutil.copy2(src, contents_dir / f)
            shutil.copy2(src, dist_2027_dir / f)
            print(f"[OK] Đã chép {f} ({src.stat().st_size:,} bytes)")

    # Tạo bản sao tương thích DTT_AutoCAD_Plugin.dll
    plugin_2027_src = bin_dir / "DTT_AutoCAD_Plugin_2027.dll"
    if plugin_2027_src.exists():
        shutil.copy2(plugin_2027_src, contents_dir / "DTT_AutoCAD_Plugin.dll")
        shutil.copy2(plugin_2027_src, dist_2027_dir / "DTT_AutoCAD_Plugin.dll")

    # Copy DTools.Core.dll và DTools.Application.dll
    dtools_bin = root_dir / "bin" / "Release" / "net48"
    if not dtools_bin.exists():
        dtools_bin = root_dir / "dist"
    for f in ["DTools.Core.dll", "DTools.Application.dll", "DTools.dll"]:
        src = dtools_bin / f
        if not src.exists():
            src = root_dir / "dist" / f
        if src.exists():
            shutil.copy2(src, contents_dir / f)
            shutil.copy2(src, dist_2027_dir / f)
            print(f"[OK] Đã đồng bộ {f} ({src.stat().st_size:,} bytes)")

    # 4. ĐỒNG BỘ TÀI NGUYÊN (Blocks, samples, symbols_database.json, yqarch)
    print("\n[BƯỚC 4/5] Đồng bộ thư viện tài nguyên (Ký hiệu điện, Dynamic Blocks, Mạch mẫu DWG, YQArch)...")
    
    # symbols_database.json
    sym_json = root_dir / "symbols_database.json"
    if sym_json.exists():
        shutil.copy2(sym_json, contents_dir / "symbols_database.json")
        shutil.copy2(sym_json, dist_2027_dir / "symbols_database.json")
        print(f"[OK] Đã sao chép symbols_database.json")

    # Blocks/
    blocks_src = root_dir / "Blocks"
    blocks_dest = contents_dir / "Blocks"
    if blocks_src.exists():
        blocks_dest.mkdir(parents=True, exist_ok=True)
        (dist_2027_dir / "Blocks").mkdir(parents=True, exist_ok=True)
        for dwg in blocks_src.glob("*.dwg"):
            shutil.copy2(dwg, blocks_dest / dwg.name)
            shutil.copy2(dwg, dist_2027_dir / "Blocks" / dwg.name)
        print(f"[OK] Đã sao chép thư viện Dynamic Blocks ({len(list(blocks_src.glob('*.dwg')))} file)")

    # samples/ (sample_dol.dwg, sample_stardelta.dwg, v.v.)
    samples_src = root_dir / "dist" / "samples"
    samples_dest = contents_dir / "samples"
    if samples_src.exists():
        samples_dest.mkdir(parents=True, exist_ok=True)
        (dist_2027_dir / "samples").mkdir(parents=True, exist_ok=True)
        for dwg in samples_src.glob("*.*"):
            shutil.copy2(dwg, samples_dest / dwg.name)
            shutil.copy2(dwg, dist_2027_dir / "samples" / dwg.name)
        print(f"[OK] Đã sao chép thư viện Mạch mẫu DWG chuẩn ({len(list(samples_src.glob('*.*')))} file)")

    # YQArch library
    yqarch_src = root_dir / "DTT_Tools.bundle" / "Contents" / "yqarch"
    yqarch_dest = contents_dir / "yqarch"
    if yqarch_src.exists() and not yqarch_dest.exists():
        shutil.copytree(yqarch_src, yqarch_dest)
        print(f"[OK] Đã đồng bộ trọn bộ thư viện kiến trúc YQArch")

    # Đảm bảo PackageContents.xml có sẵn
    pkg_xml = bundle_dir / "PackageContents.xml"
    if not pkg_xml.exists():
        print("[CẢNH BÁO] PackageContents.xml chưa tồn tại trong bundle!")

    # 5. ĐÓNG GÓI THÀNH FILE ZIP ĐỘC LẬP ĐỂ GỬI KHÁCH
    print("\n[BƯỚC 5/5] Đóng gói Standalone Bundle thành tệp ZIP phân phối...")
    zip_output = dist_2027_dir / "DTT_Tools_2027_Electrical_v2027.1.0.zip"
    if zip_output.exists():
        zip_output.unlink()

    with zipfile.ZipFile(zip_output, "w", zipfile.ZIP_DEFLATED) as zf:
        # Ghi toàn bộ thư mục DTT_Tools_2027.bundle vào trong file ZIP
        for p in bundle_dir.rglob("*"):
            if p.is_file():
                rel_path = Path("DTT_Tools_2027.bundle") / p.relative_to(bundle_dir)
                zf.write(p, arcname=str(rel_path).replace("\\", "/"))
        
        # Thêm file bat cài đặt 1-click cho bạn bè
        bat_installer = dist_2027_dir / "Cai_Dat_DTT_Tools_2027.bat"
        if bat_installer.exists():
            zf.write(bat_installer, arcname="Cai_Dat_DTT_Tools_2027.bat")

    zip_size = zip_output.stat().st_size
    print(f"\n[HOÀN TẤT] File Zip gửi cho khách: {zip_output} ({zip_size:,} bytes | {zip_size/1024/1024:.2f} MB)")

    print("\n" + "=" * 75)
    print("🎉 BÀN GIAO THÀNH CÔNG GÓI BUILD CHO AUTOCAD ELECTRICAL 2027:")
    print("=" * 75)
    print(f"📦 Thư mục Bundle:     {bundle_dir}")
    print(f"📁 Tệp ZIP gửi khách:   {zip_output}")
    print(f"🔧 DLL Khởi chạy (RAM): {contents_dir / 'DTT_Loader_2027.dll'}")
    print(f"⚡ DLL Nghiệp vụ .NET 8:{contents_dir / 'DTT_AutoCAD_Plugin_2027.dll'}")
    print("=" * 75)
    return 0

if __name__ == "__main__":
    sys.exit(main())
