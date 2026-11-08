# Bakes OpenMap's small copies of the game's minimap (its Media/Textures/MapMiniLOD, the 4-tile pieces: 256 px for 4 x 4
# minimap tiles, 64 px a tile) into assets/maps/wow/mini/<continent>_<col>_<row>.webp, for the WoW map's satellite-like
# detail when zoomed far in (WowMapCanvas). Graded like OpenMap draws them (Map/Close.lua: desaturated 0.35, tinted toward
# the map's parchment) and with the sea cut away by its land mask (Media/Textures/MapMiniLand/<continent map>.tga, which
# spans the continent map's region), so only land shows.
# Run: py -I tools/openmap_minimap.py "<OpenMap folder>"   (then reimport: Godot --headless --editor --path . --quit)
import os
import sys
from PIL import Image, ImageChops, ImageEnhance

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "assets", "maps", "wow", "mini")
TILE = 1600 / 3          # yards a minimap tile
PER = 4                  # minimap tiles a piece, each way
# Continent → its continent map's id and region in yards (XMin, YMin, XMax, YMax; X north, Y west), as in WowAtlas.cs.
CONTINENTS = {
    0: (1415, (-16000, -19199.900390625, 7466.6000976562, 16000)),
    1: (1414, (-11733.299804688, -19733.2109375, 12799.900390625, 17066.599609375)),
}
# OpenMap's tint: the parchment (1, .93, .80) a quarter of the way to the zone art's average colour at full brightness.
PAPER, TONE, SHARE = (1, 0.93, 0.80), (0.56, 0.45, 0.17), 0.25
TINT = tuple(p + (t / max(TONE) - p) * SHARE for p, t in zip(PAPER, TONE))


def main():
    addon = sys.argv[1]
    src = os.path.join(addon, "Media", "Textures", "MapMiniLOD")
    os.makedirs(OUT, exist_ok=True)
    masks = {c: Image.open(os.path.join(addon, "Media", "Textures", "MapMiniLand", f"{m}.tga")).getchannel("A")
             for c, (m, _) in CONTINENTS.items()}
    n = 0
    for name in sorted(os.listdir(src)):
        parts = name[:-4].split("_")
        if not name.endswith(".tga") or len(parts) != 4 or int(parts[1]) != PER:
            continue
        cont, col, row = int(parts[0]), int(parts[2]), int(parts[3])
        img = Image.open(os.path.join(src, name)).convert("RGBA")
        rgb = ImageEnhance.Color(img.convert("RGB")).enhance(1 - 0.35)
        r, g, b = (ch.point(lambda v, k=k: round(v * k)) for ch, k in zip(rgb.split(), TINT))
        # The piece's corners in yards, then on the continent map (0–1), where the land mask lies.
        x_n, x_s = (32 - row * PER) * TILE, (32 - (row + 1) * PER) * TILE
        y_w, y_e = (32 - col * PER) * TILE, (32 - (col + 1) * PER) * TILE
        _, (xmin, ymin, xmax, ymax) = CONTINENTS[cont]
        mask = masks[cont]
        mw, mh = mask.size
        box = ((ymax - y_w) / (ymax - ymin) * mw, (xmax - x_n) / (xmax - xmin) * mh,
               (ymax - y_e) / (ymax - ymin) * mw, (xmax - x_s) / (xmax - xmin) * mh)
        land = mask.transform(img.size, Image.Transform.EXTENT, box, Image.Resampling.BILINEAR)
        a = ImageChops.multiply(img.getchannel("A"), land)
        if a.getextrema()[1] < 8:
            continue   # (all sea)
        out = Image.merge("RGBA", (r, g, b, a))
        out.save(os.path.join(OUT, f"{cont}_{col}_{row}.webp"), "WEBP", quality=82, method=6)
        n += 1
    print(f"{n} pieces in {OUT}")


if __name__ == "__main__":
    main()
