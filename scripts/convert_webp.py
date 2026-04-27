# /// script
# dependencies = ["Pillow"]
# ///

from pathlib import Path
from PIL import Image

BASE = Path(__file__).parent.parent

conversions = [
    (BASE / "Content/Items/Terraprisma.webp", BASE / "Content/Items/GrowableTerraprismaItem.png"),
    (BASE / "Content/Buffs/Terraprisma_(buff).webp", BASE / "Content/Buffs/GrowableTerraprismaBuff.png"),
]

for src, dst in conversions:
    if src.exists():
        img = Image.open(src)
        img.save(dst, "PNG")
        print(f"✅ {src.name} → {dst.name} ({img.size[0]}x{img.size[1]})")
    else:
        print(f"❌ {src.name} 不存在")
