from PIL import Image, ImageDraw, ImageFilter
from pathlib import Path

root = Path(__file__).parent
S = 1024
im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
px = im.load()
for y in range(S):
    t = y / (S - 1)
    col = tuple(round(a * (1 - t) + b * t) for a, b in zip((91, 70, 56), (48, 39, 33)))
    for x in range(S):
        px[x, y] = (*col, 255)
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle((12, 12, S - 12, S - 12), radius=226, fill=255)
im.putalpha(mask)

# A warm, high contrast timer mark that matches the clay and ceramic UI.
d = ImageDraw.Draw(im)
cream = (248, 238, 220, 255)
terracotta = (210, 127, 89, 255)
cx, cy, radius = 512, 545, 300
d.rounded_rectangle((cx - 84, 110, cx + 84, 276), radius=68, fill=terracotta)
d.ellipse((cx-radius, cy-radius, cx+radius, cy+radius), fill=cream)
d.ellipse((cx-radius+48, cy-radius+48, cx+radius-48, cy+radius-48), fill=(69, 54, 44, 255))
d.line((cx, cy, cx, cy - 152), fill=terracotta, width=56)
d.line((cx, cy, cx + 145, cy + 94), fill=terracotta, width=56)
d.ellipse((cx-42, cy-42, cx+42, cy+42), fill=cream)
im.save(root / "WindowsTimer.ico", format="ICO", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
