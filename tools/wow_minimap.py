# Bakes the game's minimap tiles (512 px for 533⅓ yards, taken from the WoW Forever install's files with TACTSharp:
# FileDataIDs in the OpenMap addon's Data/Minimap.lua, [continent][tx * 64 + ty]; saved as <continent>_<tx>_<ty>.png) into
# assets/maps/wow/mini/<continent>_<tx>_<ty>.webp at 256 px, for the WoW map's satellite-like detail when zoomed far in
# (WowMapCanvas). Graded like OpenMap draws its minimap detail (Map/Close.lua: desaturated 0.35, tinted toward the map's
# parchment) and with the sea cut away by OpenMap's land mask (Media/Textures/MapMiniLand/<continent map>.tga, which spans
# the continent map's region), so only land shows.
# Run: py -I tools/wow_minimap.py "<folder of extracted tiles>" "<OpenMap folder>"   (then reimport: Godot --headless --editor --path . --quit)
import os
import sys
from PIL import Image, ImageChops, ImageEnhance

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "assets", "maps", "wow", "mini")
TILE = 1600 / 3          # yards a minimap tile
PX = 256                 # pixels a tile in the game's files: 512
# Continent → its continent map's id and region in yards (XMin, YMin, XMax, YMax; X north, Y west), as in WowAtlas.cs.
CONTINENTS = {
    0: (1415, (-16000, -19199.900390625, 7466.6000976562, 16000)),
    1: (1414, (-11733.299804688, -19733.2109375, 12799.900390625, 17066.599609375)),
}
# OpenMap's tint: the parchment (1, .93, .80) a quarter of the way to the zone art's average colour at full brightness.
PAPER, TONE, SHARE = (1, 0.93, 0.80), (0.56, 0.45, 0.17), 0.25
TINT = tuple(p + (t / max(TONE) - p) * SHARE for p, t in zip(PAPER, TONE))


def main():
    src, addon = sys.argv[1], sys.argv[2]
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        os.remove(os.path.join(OUT, old))   # (the tile set replaces what was there)
    masks = {c: Image.open(os.path.join(addon, "Media", "Textures", "MapMiniLand", f"{m}.tga")).getchannel("A")
             for c, (m, _) in CONTINENTS.items()}
    n = 0
    for name in sorted(os.listdir(src)):
        parts = name[:-4].split("_")
        if not name.endswith(".png") or len(parts) != 3:
            continue
        cont, col, row = (int(p) for p in parts)
        img = Image.open(os.path.join(src, name)).convert("RGBA").resize((PX, PX), Image.Resampling.LANCZOS)
        rgb = ImageEnhance.Color(img.convert("RGB")).enhance(1 - 0.35)
        r, g, b = (ch.point(lambda v, k=k: round(v * k)) for ch, k in zip(rgb.split(), TINT))
        # The tile's corners in yards, then on the continent map (0–1), where the land mask lies.
        # (tx runs east = west yards falling, ty south = north yards falling; tile 32 starts at 0 yards)
        x_n, x_s = (32 - row) * TILE, (31 - row) * TILE
        y_w, y_e = (32 - col) * TILE, (31 - col) * TILE
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
        out.save(os.path.join(OUT, f"{cont}_{col}_{row}.webp"), "WEBP", quality=80, method=6)
        with open(os.path.join(OUT, f"{cont}_{col}_{row}.webp.import"), "w", newline="\n") as imp:
            imp.write('[remap]\n\nimporter="keep"\n')   # (decoded by Images.LoadBuiltinImage, like all map art)
        n += 1
    print(f"{n} tiles in {OUT}")


if __name__ == "__main__":
    main()
