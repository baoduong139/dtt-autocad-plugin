import os
import sys
import zipfile
from datetime import datetime
from pathlib import Path

def create_backup():
    root = Path(__file__).resolve().parent.parent
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_file = root / f"_backup_dtt_tools_{ts}.zip"
    
    include_patterns = [
        "AppEntry.cs", "DTools.sln", "DTools.slnx", "DTT_AutoCAD_Plugin.csproj",
        "README_HDSD.md", "symbols_database.json",
        "Blocks", "Build", "Common", "Modules", "src", "tests", "UI",
        "DTT_Tools.bundle", "docs"
    ]
    
    print(f"Creating backup: {backup_file.name}...")
    with zipfile.ZipFile(backup_file, "w", zipfile.ZIP_DEFLATED) as zf:
        for item in include_patterns:
            p = root / item
            if not p.exists():
                continue
            if p.is_file():
                zf.write(p, arcname=p.name)
            elif p.is_dir():
                for sub in p.rglob("*"):
                    # Exclude bin and obj
                    if any(part in ("bin", "obj", "__pycache__") for part in sub.parts):
                        continue
                    if sub.is_file():
                        rel = sub.relative_to(root)
                        zf.write(sub, arcname=str(rel).replace("\\", "/"))
                        
    size_mb = backup_file.stat().st_size / (1024 * 1024)
    print(f"Backup created successfully: {backup_file.name} ({size_mb:.2f} MB)")
    return backup_file

if __name__ == "__main__":
    create_backup()

