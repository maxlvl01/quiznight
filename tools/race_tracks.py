"""Draws the race track maps (MapPresets.cs, "Race tracks"): one opaque picture per circuit, the same look in every skin.

    py -I tools/race_tracks.py DATA_DIR [ID ...]

DATA_DIR holds what the research downloaded (2026-11-02):
  bacinger/f1-circuits.geojson   the track lines (github.com/bacinger/f1-circuits, MIT, © 2019-2025 Tomislav Bacinger):
                                 one closed line per circuit, its first point on the start/finish line
  <bacinger id>/overpass*.json   OpenStreetMap around the circuit (Overpass "out geom"; © OpenStreetMap contributors, ODbL):
                                 pit lane, water, trees, grass, buildings, roads
Writes assets/maps/f1-<id>.webp (the map) and assets/map-thumbs/f1-<id>.webp (400 px wide), and prints each map's
width in km for MapPresets.cs. The picture is the track's bounding box plus MARGIN_KM on every side, north up.

No corner names or numbers are drawn: they'd give the answer away ("pin Eau Rouge"). The start/finish line is a
chequered bar and an arrow after it shows the direction the race runs.
"""
import json, math, os, sys
from PIL import Image, ImageDraw

# (our id, bacinger id): the 2026 calendar; Bahrain and Jeddah are cancelled this year
TRACKS = [
    ("melbourne", "au-1953"), ("shanghai", "cn-2004"), ("suzuka", "jp-1962"), ("miami", "us-2022"),
    ("montreal", "ca-1978"), ("monaco", "mc-1929"), ("barcelona", "es-1991"), ("red-bull-ring", "at-1969"),
    ("silverstone", "gb-1948"), ("spa", "be-1925"), ("hungaroring", "hu-1986"), ("zandvoort", "nl-1948"),
    ("monza", "it-1922"), ("madrid", "es-2026"), ("baku", "az-2016"), ("singapore", "sg-2008"),
    ("cota", "us-2012"), ("mexico-city", "mx-1962"), ("interlagos", "br-1940"), ("las-vegas", "us-2023"),
    ("lusail", "qa-2004"), ("yas-marina", "ae-2009"),
]
# Lines drawn the wrong way round in the source (checked 2026-11-02 against the direction each circuit is known to run):
# Marina Bay runs anticlockwise
REVERSE = {"singapore"}
# Start/finish lines the source misses: OSM's "Départ Grand Prix de Monaco" (node, lon/lat)
START = {"monaco": (7.4212652, 43.7350269)}
MARGIN_KM = 0.3
LONG_SIDE = 2400   # px of the longer side
SS = 2             # drawn this much bigger, then scaled down (smooth edges)

# Colours: a light, calm map so the dark track stands out, whatever the skin around it
BG = (233, 238, 224)
GRASS = (214, 230, 192)
WOOD = (176, 208, 158)
WATER = (156, 202, 232)
WATER_EDGE = (120, 176, 214)
SAND = (238, 226, 192)
PAVED = (226, 223, 216)
BUILDING = (205, 199, 190)
BUILDING_EDGE = (182, 175, 165)
ROAD = (255, 255, 255)
ROAD_EDGE = (200, 198, 190)
RAIL = (170, 166, 160)
TRACK = (40, 44, 52)
TRACK_EDGE = (255, 255, 255)
PIT = (96, 102, 112)
ARROW = (255, 140, 30)

# Pit lanes of other layouts (Silverstone's Stowe/National, MotoGP entries, support series)
PIT_SKIP = ("stowe", "national", "motogp", "support", "west circuit", "karting", "kart")


def lane_name(tags):
    return (tags.get("name") or tags.get("name:en") or "").lower()


def is_pit(tags):
    n = lane_name(tags)
    if any(s in n for s in PIT_SKIP):
        return False
    return tags.get("raceway") == "pitlane" or tags.get("highway") == "raceway" and any(
        w in n for w in ("pit", "box", "stand", "boxen", "pitstraat", "bokszutca"))


ROADS = {"motorway": 7, "trunk": 7, "primary": 6, "secondary": 5, "tertiary": 4.5, "unclassified": 3.5, "residential": 3.5,
         "service": 2.5, "living_street": 3, "motorway_link": 4, "trunk_link": 4, "primary_link": 4, "secondary_link": 4}


def area_colour(tags):
    if tags.get("natural") == "water" or tags.get("waterway") in ("riverbank", "dock") or tags.get("landuse") in ("reservoir", "basin"):
        return WATER
    if tags.get("landuse") == "forest" or tags.get("natural") in ("wood", "scrub"):
        return WOOD
    if tags.get("landuse") in ("grass", "meadow", "recreation_ground", "village_green", "farmland") or \
            tags.get("natural") in ("grassland", "heath") or tags.get("leisure") in ("park", "golf_course", "pitch", "garden"):
        return GRASS
    if tags.get("natural") in ("sand", "beach", "shingle", "bare_rock", "scree"):
        return SAND
    if tags.get("amenity") == "parking" or tags.get("landuse") in ("industrial", "commercial", "retail", "residential", "construction") \
            or "area:highway" in tags:
        return PAVED
    return None


# Drawing order: big land first, then water, then buildings
LAYER = {PAVED: 0, GRASS: 1, SAND: 2, WOOD: 3, WATER: 4}


def main():
    data, *only = sys.argv[1:]
    with open(os.path.join(data, "bacinger", "f1-circuits.geojson"), encoding="utf-8") as f:
        lines = {x["properties"]["id"]: x for x in json.load(f)["features"]}
    os.makedirs("assets/maps", exist_ok=True)
    os.makedirs("assets/map-thumbs", exist_ok=True)
    for ours, theirs in TRACKS:
        if only and ours not in only:
            continue
        feature = lines[theirs]
        line = feature["geometry"]["coordinates"]
        if ours in REVERSE:
            line = line[:1] + line[1:][::-1]
        osm = []
        folder = os.path.join(data, theirs)
        # (the bigger download first: "_bbox" covers the whole picture)
        for name in ("overpass_bbox.json", "overpass.json"):
            path = os.path.join(folder, name)
            if os.path.isfile(path):
                with open(path, encoding="utf-8") as f:
                    osm = [e for e in json.load(f)["elements"] if e["type"] == "way" and "geometry" in e]
                break
        rels = []
        path = os.path.join(folder, "areas.json")
        if os.path.isfile(path):
            with open(path, encoding="utf-8") as f:
                got = json.load(f)["elements"]
            rels = [e for e in got if e["type"] == "relation"]
            # (coastlines come as ways; a way already in the first download isn't drawn twice)
            have = {e["id"] for e in osm}
            osm += [e for e in got if e["type"] == "way" and "geometry" in e and e["id"] not in have]
        line = start_at(ours, line)
        km = draw(ours, line, osm, rels)
        print(f'{ours}: {km:.3f} km wide, {len(osm)} OSM ways ({feature["properties"]["Name"]}, {feature["properties"]["length"]} m)')


def start_at(ours, line):
    """The line from its start/finish. Bacinger's lines begin on the start straight, beside the pit lane (checked: within
    350 m of the pit lane's middle, Hungaroring 500 m), except Monaco's (800 m off, on the pool section): those start at
    the line's point nearest START."""
    if ours not in START:
        return line
    kx = 111320 * math.cos(math.radians(line[0][1]))
    lon, lat = START[ours]
    ring = line[:-1] if line[0] == line[-1] else line
    i = min(range(len(ring)), key=lambda i: math.hypot((ring[i][0] - lon) * kx, (ring[i][1] - lat) * 110574))
    ring = ring[i:] + ring[:i]
    return ring + ring[:1]


def rings(members):
    """A relation's member ways joined into closed rings (OSM splits a lake's shore into many ways)."""
    open_ = [[(g["lon"], g["lat"]) for g in m["geometry"]] for m in members if m.get("geometry")]
    done = []
    while open_:
        ring = open_.pop()
        grown = True
        while ring[0] != ring[-1] and grown:
            grown = False
            for i, w in enumerate(open_):
                if w[0] == ring[-1]:
                    ring += w[1:]
                elif w[-1] == ring[-1]:
                    ring += w[::-1][1:]
                elif w[-1] == ring[0]:
                    ring = w + ring[1:]
                elif w[0] == ring[0]:
                    ring = w[::-1] + ring[1:]
                else:
                    continue
                open_.pop(i)
                grown = True
                break
        if len(ring) > 3:
            done.append(ring)
    return done


def draw(ours, line, osm, rels=()):
    """Draws the map, and the thumbnail as a small map of its own (the track keeps its pixel width, so it reads)."""
    img = render(line, osm, rels, LONG_SIDE, ours)
    img.save(f"assets/maps/f1-{ours}.webp", "WEBP", quality=88, method=6)
    # (the editor's cards are wide and crop to fill: the whole track goes in a 2:1 thumbnail, the map colour around it)
    small = render(line, osm, rels, 900, None)
    small.thumbnail((392, 192), Image.LANCZOS)
    th = Image.new("RGB", (400, 200), BG)
    th.paste(small, ((400 - small.width) // 2, (200 - small.height) // 2))
    th.save(f"assets/map-thumbs/f1-{ours}.webp", "WEBP", quality=85, method=6)
    return img.width / img.info["px_per_km"]


def render(line, osm, rels, long_side, ours):
    lon0 = sum(p[0] for p in line) / len(line)
    lat0 = sum(p[1] for p in line) / len(line)
    kx = 111.320 * math.cos(math.radians(lat0))
    ky = 110.574

    def km(lon, lat):
        return (lon - lon0) * kx, (lat0 - lat) * ky

    pts = [km(*p) for p in line]
    x0 = min(p[0] for p in pts) - MARGIN_KM
    x1 = max(p[0] for p in pts) + MARGIN_KM
    y0 = min(p[1] for p in pts) - MARGIN_KM
    y1 = max(p[1] for p in pts) + MARGIN_KM
    wkm, hkm = x1 - x0, y1 - y0
    scale = long_side / max(wkm, hkm)          # px per km
    W, H = round(wkm * scale), round(hkm * scale)
    s = scale * SS
    m_px = s / 1000                            # px per metre (drawn size)

    def px(lon, lat):
        x, y = km(lon, lat)
        return ((x - x0) * s, (y - y0) * s)

    img = Image.new("RGB", (W * SS, H * SS), BG)
    d = ImageDraw.Draw(img)

    def way_pts(e):
        return [px(g["lon"], g["lat"]) for g in e["geometry"]]

    def closed(e):
        g = e["geometry"]
        return len(g) > 3 and g[0]["lat"] == g[-1]["lat"] and g[0]["lon"] == g[-1]["lon"]

    # The sea: coastlines run with the land on their left, so the water is flooded in from just right of each one
    coast = [way_pts(e) for e in osm if e.get("tags", {}).get("natural") == "coastline"]
    if coast:
        mask = Image.new("L", img.size, 0)
        md = ImageDraw.Draw(mask)
        for c in coast:
            md.line(c, fill=255, width=3 * SS)
        for c in coast:
            for (ax, ay), (bx, by) in zip(c, c[1:]):
                n = math.hypot(bx - ax, by - ay)
                if n < 1:
                    continue
                qx, qy = (ax + bx) / 2 - (by - ay) / n * 6 * SS, (ay + by) / 2 + (bx - ax) / n * 6 * SS
                if 0 <= qx < img.width and 0 <= qy < img.height and mask.getpixel((int(qx), int(qy))) == 0:
                    ImageDraw.floodfill(mask, (int(qx), int(qy)), 128)
        sea = mask.point(lambda v: 255 if v == 128 else 0)
        share = sum(sea.histogram()[255:]) / (img.width * img.height)
        if share < 0.9:
            img.paste(WATER, mask=sea)
        else:
            print(f"  {ours or '(thumb)'}: the coastline doesn't close, no sea drawn")
    # Areas
    areas = []
    for r in rels:
        t = r.get("tags", {})
        if (c := area_colour(t)) is not None:
            areas.append((LAYER[c], c, r))
    for e in osm:
        t = e.get("tags", {})
        if closed(e) and (c := area_colour(t)) is not None:
            areas.append((LAYER[c], c, e))
    for _, c, e in sorted(areas, key=lambda a: a[0]):
        if e["type"] == "way":
            d.polygon(way_pts(e), fill=c, outline=WATER_EDGE if c == WATER else None)
            continue
        # (a relation: its outer rings filled, its inner rings (islands) cut out again)
        mask = Image.new("L", img.size, 0)
        md = ImageDraw.Draw(mask)
        for role, fill in (("outer", 255), ("inner", 0)):
            for ring in rings([m for m in e.get("members", []) if m.get("type") == "way" and (m.get("role") or "outer") == role]):
                md.polygon([px(*q) for q in ring], fill=fill)
        img.paste(c, mask=mask)
    # Rivers and streams
    for e in osm:
        t = e.get("tags", {})
        if t.get("waterway") in ("river", "canal", "stream", "drain"):
            w = {"river": 14, "canal": 10}.get(t["waterway"], 4) * m_px
            d.line(way_pts(e), fill=WATER, width=max(2, round(w)), joint="curve")
    # Buildings (at most the biggest 4000: Interlagos has 10,000)
    blds = [e for e in osm if "building" in e.get("tags", {}) and closed(e)]
    blds.sort(key=lambda e: -len(e["geometry"]))
    for e in blds[:4000]:
        d.polygon(way_pts(e), fill=BUILDING, outline=BUILDING_EDGE)
    # Roads and rails, under the track
    roads = [(ROADS[e["tags"]["highway"]], e) for e in osm if e.get("tags", {}).get("highway") in ROADS]
    for w, e in roads:
        d.line(way_pts(e), fill=ROAD_EDGE, width=max(2, round(w * m_px * 1.6 + 2 * SS)), joint="curve")
    for w, e in roads:
        d.line(way_pts(e), fill=ROAD, width=max(1, round(w * m_px * 1.6)), joint="curve")
    for e in osm:
        if e.get("tags", {}).get("railway") in ("rail", "light_rail", "tram"):
            d.line(way_pts(e), fill=RAIL, width=max(1, round(1.5 * SS)))

    # The track, drawn wider than life so it reads at any zoom
    tw = max((14 if long_side > 1000 else 13) * SS, round(16 * m_px))
    track = [((x - x0) * s, (y - y0) * s) for x, y in pts]
    if track[0] != track[-1]:
        track.append(track[0])
    # Pit lane: near the start/finish (first point), never other layouts'
    sx, sy = track[0]
    for e in osm:
        t = e.get("tags", {})
        if not is_pit(t):
            continue
        p = way_pts(e)
        cx = sum(q[0] for q in p) / len(p)
        cy = sum(q[1] for q in p) / len(p)
        if math.hypot(cx - sx, cy - sy) / s > 0.9:
            continue
        d.line(p, fill=TRACK_EDGE, width=round(tw * 0.7), joint="curve")
        d.line(p, fill=PIT, width=round(tw * 0.45), joint="curve")
    d.line(track, fill=TRACK_EDGE, width=tw + 6 * SS, joint="curve")
    for q in track:
        r = (tw + 6 * SS) / 2
        d.ellipse((q[0] - r, q[1] - r, q[0] + r, q[1] + r), fill=TRACK_EDGE)
    d.line(track, fill=TRACK, width=tw, joint="curve")
    for q in track:
        r = tw / 2
        d.ellipse((q[0] - r, q[1] - r, q[0] + r, q[1] + r), fill=TRACK)

    # Start/finish: a chequered bar across the track at the first point
    (ax, ay), (bx, by) = track[0], next(q for q in track[1:] if math.hypot(q[0] - track[0][0], q[1] - track[0][1]) > tw)
    dx, dy = bx - ax, by - ay
    n = math.hypot(dx, dy)
    ux, uy = dx / n, dy / n                    # along the track
    nx, ny = -uy, ux                           # across
    half = tw / 2 + 4 * SS
    sq = (2 * half) / 6
    for i in range(6):
        for j in range(3):
            c = (255, 255, 255) if (i + j) % 2 == 0 else (20, 20, 20)
            a = -half + i * sq
            b = (j - 1.5) * sq
            quad = [(ax + nx * (a + da) + ux * (b + db), ay + ny * (a + da) + uy * (b + db))
                    for da, db in ((0, 0), (sq, 0), (sq, sq), (0, sq))]
            d.polygon(quad, fill=c)
    # Direction: an arrow beside the track a little after the start
    dist, k = 0.0, 1
    while k < len(track) - 1 and dist < 0.12 * s:
        dist += math.hypot(track[k][0] - track[k - 1][0], track[k][1] - track[k - 1][1])
        k += 1
    (px0, py0), (px1, py1) = track[k - 1], track[k]
    n = math.hypot(px1 - px0, py1 - py0) or 1
    ux, uy = (px1 - px0) / n, (py1 - py0) / n
    nx, ny = -uy, ux
    off = tw * 2.2
    # (on the side with less track nearby: the outside of the lap, roughly)
    cx = sum(q[0] for q in track) / len(track)
    cy = sum(q[1] for q in track) / len(track)
    if (px0 + nx * off - cx) ** 2 + (py0 + ny * off - cy) ** 2 < (px0 - nx * off - cx) ** 2 + (py0 - ny * off - cy) ** 2:
        nx, ny = -nx, -ny
    mx, my = px0 + nx * off, py0 + ny * off
    L = tw * 3.2
    tip = (mx + ux * L / 2, my + uy * L / 2)
    tail = (mx - ux * L / 2, my - uy * L / 2)
    head = [tip, (tip[0] - ux * L * 0.5 + nx * L * 0.35, tip[1] - uy * L * 0.5 + ny * L * 0.35),
            (tip[0] - ux * L * 0.5 - nx * L * 0.35, tip[1] - uy * L * 0.5 - ny * L * 0.35)]
    d.line([tail, (tip[0] - ux * L * 0.4, tip[1] - uy * L * 0.4)], fill=TRACK, width=round(L * 0.34))
    d.polygon(head, fill=TRACK)
    d.line([tail, (tip[0] - ux * L * 0.4, tip[1] - uy * L * 0.4)], fill=ARROW, width=round(L * 0.2))
    inner = [(tip[0] - ux * L * 0.08, tip[1] - uy * L * 0.08),
             (tip[0] - ux * L * 0.44 + nx * L * 0.24, tip[1] - uy * L * 0.44 + ny * L * 0.24),
             (tip[0] - ux * L * 0.44 - nx * L * 0.24, tip[1] - uy * L * 0.44 - ny * L * 0.24)]
    d.polygon(inner, fill=ARROW)

    img = img.resize((W, H), Image.LANCZOS)
    img.info["px_per_km"] = scale
    return img


if __name__ == "__main__":
    main()
