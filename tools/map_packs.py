"""Makes the built-in maps' download packs (see scripts/Storage/MapPacks.cs).

    py -I tools/map_packs.py OUT_DIR [PREVIOUS_MAPS_JSON]

One pack per built-in map: assets/maps/<id>.webp is the pack "<id>", everything under assets/maps/wow/ the pack
"wow-azeroth" (WowAtlas.PresetId). Each pack is a zip (stored, the WebPs are compressed already, with fixed dates so
the same maps always give the same zip) named "map-<id>-<first 12 of its SHA-256>.zip"; OUT_DIR gets the zips and
maps.json (every pack with its file name, size, SHA-256 and files). Prints the zips that are new compared with
PREVIOUS_MAPS_JSON (the ones to upload). tools/map_packs.sh signs maps.json and publishes it all.
"""
import hashlib, io, json, os, sys, zipfile

out, *prev = sys.argv[1:]
MAPS = "assets/maps"
WOW = "wow-azeroth"
os.makedirs(out, exist_ok=True)
known = set()
if prev and os.path.isfile(prev[0]):
    with open(prev[0], encoding="utf-8") as f:
        known = {p["file"] for p in json.load(f)["packs"].values()}

packs = {}
for name in sorted(os.listdir(MAPS)):
    if name.endswith(".webp"):
        packs[name[:-5]] = [name]
packs[WOW] = sorted(
    os.path.relpath(os.path.join(root, n), MAPS).replace(os.sep, "/")
    for root, _, names in os.walk(os.path.join(MAPS, "wow")) for n in names if n.endswith(".webp"))


def sha(data):
    return hashlib.sha256(data).hexdigest()


listed, new = {}, []
for pid, files in packs.items():
    buf = io.BytesIO()
    entries = []
    with zipfile.ZipFile(buf, "w", zipfile.ZIP_STORED) as z:
        for rel in files:
            with open(os.path.join(MAPS, rel), "rb") as f:
                data = f.read()
            info = zipfile.ZipInfo(rel, date_time=(2026, 1, 1, 0, 0, 0))
            info.external_attr = 0o644 << 16
            z.writestr(info, data)
            entries.append({"path": rel, "size": len(data), "sha256": sha(data)})
    data = buf.getvalue()
    digest = sha(data)
    file = f"map-{pid}-{digest[:12]}.zip"
    with open(os.path.join(out, file), "wb") as f:
        f.write(data)
    listed[pid] = {"file": file, "size": len(data), "sha256": digest, "files": entries}
    if file not in known:
        new.append(file)

with open(os.path.join(out, "maps.json"), "w", encoding="utf-8", newline="\n") as f:
    json.dump({"version": 1, "packs": listed}, f, indent=1)
total = sum(p["size"] for p in listed.values())
print(f"{len(listed)} map packs, {total / 1e6:.1f} MB; new: {len(new)}", file=sys.stderr)
for n in new:
    print(n)
