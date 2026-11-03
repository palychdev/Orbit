"""Regenerate Orbit's native multi-resolution icon from its simple geometric mark.

Development only: requires Pillow. The application ships the generated ICO.
"""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
scale = 4
image = Image.new("RGBA", (256 * scale, 256 * scale))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((0, 0, 256 * scale - 1, 256 * scale - 1), radius=52 * scale, fill="#1C1C1E")
box = tuple(v * scale for v in (47, 47, 209, 209))
for start in (-88, 2, 92, 182):
    draw.arc(box, start=start, end=start + 66, fill="#F5F5F7", width=17 * scale)
draw.ellipse(tuple(v * scale for v in (112, 112, 144, 144)), fill="#F5F5F7")
image = image.resize((256, 256), Image.Resampling.LANCZOS)
(root / "Assets").mkdir(exist_ok=True)
image.save(root / "Assets" / "orbit.ico", sizes=[(n, n) for n in (16, 24, 32, 48, 64, 128, 256)])
