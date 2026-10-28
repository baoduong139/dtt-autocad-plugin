import os
import re
from pathlib import Path

def audit_codebase():
    root = Path(__file__).resolve().parent.parent
    
    # 1. Collect all [CommandMethod("...")]
    commands = {}
    cs_files = list(root.glob("*.cs")) + list(root.glob("Modules/**/*.cs")) + list(root.glob("UI/**/*.cs")) + list(root.glob("Common/**/*.cs")) + list(root.glob("src/**/*.cs"))
    
    cmd_pattern = re.compile(r'\[CommandMethod\s*\(\s*"([^"]+)"(?:\s*,\s*"([^"]+)")?(?:\s*,\s*CommandFlags\.([^)]+))?\s*\)\]')
    
    for cs in cs_files:
        try:
            content = cs.read_text(encoding="utf-8")
        except Exception:
            try:
                content = cs.read_text(encoding="latin1")
            except Exception:
                continue
                
        for match in cmd_pattern.finditer(content):
            cmd_name = match.group(1)
            commands.setdefault(cmd_name, []).append(cs.relative_to(root))
            
    print(f"Total Unique Registered Commands in C#: {len(commands)}")
    
    # 2. Check PackageContents.xml commands
    pkg_xml = root / "DTT_Tools.bundle" / "PackageContents.xml"
    pkg_commands = set()
    if pkg_xml.exists():
        xml_content = pkg_xml.read_text(encoding="utf-8")
        for m in re.finditer(r'<Command\s+Global="([^"]+)"', xml_content):
            pkg_commands.add(m.group(1))
            
    print(f"Total Commands in PackageContents.xml: {len(pkg_commands)}")
    
    missing_in_pkg = [c for c in commands if c not in pkg_commands]
    missing_in_code = [c for c in pkg_commands if c not in commands]
    
    print(f"Commands in C# but NOT in PackageContents.xml: {len(missing_in_pkg)}")
    for c in sorted(missing_in_pkg):
        print(f"  + {c} (in {commands[c]})")
        
    print(f"\nCommands in PackageContents.xml but NOT found in C#: {len(missing_in_code)}")
    for c in sorted(missing_in_code):
        print(f"  - {c}")
        
    # 3. Check hardcoded paths in C# and scripts
    hardcoded_patterns = [
        re.compile(r'C:\\Users\\[a-zA-Z0-9_.]+', re.IGNORECASE),
        re.compile(r'C:\\Program Files\\Autodesk\\AutoCAD [0-9]+', re.IGNORECASE),
        re.compile(r'D:\\Program Files\\Autodesk\\AutoCAD [0-9]+', re.IGNORECASE),
        re.compile(r'C:\\ProgramData\\Autodesk', re.IGNORECASE)
    ]
    
    print("\n--- HARDCODED PATH AUDIT ---")
    files_to_check = cs_files + list(root.glob("*.ps1")) + list(root.glob("Build/**/*.ps1")) + list(root.glob("Build/**/*.py")) + list(root.glob("UI/**/*.xaml"))
    hardcoded_findings = []
    
    for f in files_to_check:
        if "bin" in f.parts or "obj" in f.parts or "dist" in f.parts:
            continue
        try:
            lines = f.read_text(encoding="utf-8").splitlines()
        except Exception:
            continue
            
        for idx, line in enumerate(lines, 1):
            for pat in hardcoded_patterns:
                if pat.search(line):
                    hardcoded_findings.append((f.relative_to(root), idx, line.strip()))
                    
    print(f"Found {len(hardcoded_findings)} lines with hardcoded machine paths:")
    for file, line_no, text in hardcoded_findings[:30]:
        print(f"  {file}:{line_no} -> {text}")

if __name__ == "__main__":
    audit_codebase()
