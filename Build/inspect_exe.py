import subprocess
import sys
from pathlib import Path

# Let's inspect DTools.Tests.exe and DTools.Preview.exe and DTools.exe using ildasm or python
root = Path(__file__).resolve().parent.parent
print("Root:", root)

# Find ildasm in Windows SDK or .NET Framework
ildasm_candidates = list(Path(r"C:\Program Files (x86)\Microsoft SDKs\Windows").glob("**/ildasm.exe"))
print("ildasm candidates:", ildasm_candidates)
if ildasm_candidates:
    ildasm = ildasm_candidates[0]
    out_il = root / "dist" / "tests_decompiled.il"
    subprocess.run([str(ildasm), "/text", str(root / "dist" / "DTools.Tests.exe"), f"/out={out_il}"])
    if out_il.exists():
        print("Decompiled tests IL to", out_il, "size:", out_il.stat().st_size)

