// The WoW Forever map (OpenMap's art; the zones and distances are Classic's, which Forever builds on) as one seamless, zoomable world, like the OpenMap addon: zoom out and you see
// Azeroth, zoom in and the continent comes in, then every zone stitched together, then the cities.
// Pins are placed anywhere and stored as a point on the world map image, as before (WowAtlas).
//
// Layers, bottom to top, and the zoom each fades in at (zoom 1 = the whole world fits; OpenMap's):
//   OpenMap's sea       3–5       its colour and tiled paper (assets/maps/wow/sea), as the world and continent maps go
//   world map           until 3–5
//   continent maps      1.3–2.2, out again at 3–5   soft edges, so their parchment frame never shows (what's left of them shows
//                                 only where the zone art has gaps)
//   zone art, whole     1.1–1.5   all the stitched zones as one map, downsized (assets/maps/wow/overview-2/-4,
//                                 tools/openmap_overview.py): the sharper one once a world-map pixel is over 2 px
//   stitched zones      4–6 px per world-map pixel: OpenMap's zone art baked into tiles (assets/maps/wow/stitched,
//                                 made by tools/openmap_tiles.gd), loaded in the background
//   cities              once the city fills a fifth to two fifths of the view, cut round

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui;

/// <summary>The seamless map's art: continents and cities cut to shape, and the stitched zone tiles.</summary>
public static class WowMapArt
{
	const string TileDir = "res://assets/maps/wow/stitched/";
	// OpenMap's bake: 16 px per world-map pixel, in 512-px tiles of which the middle 510 px are the
	// tile's own (the outer pixel is its neighbours' edge, there so tiles meet without seams).
	const double TilePx = 16, TileStep = 510;

	public sealed record Tile(string Path, Rect2 World);

	/// <summary>Every tile, with the part of the world map (0–1) it covers.</summary>
	public static IReadOnlyList<Tile> Tiles => _tiles ??= FindTiles();
	static List<Tile>? _tiles;

	/// <summary>The map's files came or went (MapPacks): look for the tiles again, drop what was loaded.</summary>
	public static void Forget()
	{
		_tiles = null;
		_minimap = null;
		Loaded.Clear();
		Recent.Clear();
		Cut.Clear();
	}
	public static readonly Rect2 TileSource = new(1, 1, (float)TileStep, (float)TileStep);

	/// <summary>All the tiles as one map, downsized to px pixels per world-map pixel (2 or 4; tools/openmap_overview.py).</summary>
	public static string Overview(int px) => $"res://assets/maps/wow/overview-{px}.webp";

	/// <summary>OpenMap's sea (assets/maps/wow/sea, from its Media/Textures): "field" is the sea's colour as the world map
	/// paints it, stretched over the whole world; "dark" and "light" its paper mottling, tiled.</summary>
	public static string Sea(string name) => $"res://assets/maps/wow/sea/{name}.webp";

	/// <summary>The game's minimap (tools/wow_minimap.py): one picture a minimap tile of 533⅓ yards, at 256 px, named
	/// <continent>_<tx>_<ty>; with the part of the world map (0–1) each covers.</summary>
	public static IReadOnlyList<Tile> Minimap => _minimap ??= FindMinimap();
	static List<Tile>? _minimap;

	static List<Tile> FindMinimap()
	{
		const double yards = 1600.0 / 3;
		const string dir = "res://assets/maps/wow/mini/";
		var list = new List<Tile>();
		foreach (var file in DirAccess.GetFilesAt(Images.BuiltinFile(dir)))
		{
			var parts = file.EndsWith(".webp") ? file[..^5].Split('_') : Array.Empty<string>();
			if (parts.Length != 3 || !int.TryParse(parts[0], out var c) || !int.TryParse(parts[1], out var tx) || !int.TryParse(parts[2], out var ty)) continue;
			// (tile 32 starts at 0 yards; tx runs east = west yards falling, ty south = north yards falling)
			var nw = WowAtlas.LocationToWorld(new WowAtlas.Location(c, (32 - ty) * yards, (32 - tx) * yards));
			var se = WowAtlas.LocationToWorld(new WowAtlas.Location(c, (31 - ty) * yards, (31 - tx) * yards));
			list.Add(new Tile(dir + file, new Rect2((float)nw.X, (float)nw.Y, (float)(se.X - nw.X), (float)(se.Y - nw.Y))));
		}
		return list;
	}

	/// <summary>The mottling's tile in world-map pixels (OpenMap: 128 continent-art pixels, a continent pixel ≈ 0.4 of the world map's).</summary>
	public const float SeaTile = 128 * 0.4f;

	static List<Tile> FindTiles()
	{
		var list = new List<Tile>();
		var size = TileStep / TilePx; // world-map pixels per tile
		for (var i = 0; i < 40; i++)
		for (var j = 0; j < 30; j++)
		{
			var path = $"{TileDir}{i}_{j}.webp";
			if (!FileAccess.FileExists(Images.BuiltinFile(path))) continue;
			list.Add(new Tile(path, new Rect2(
				(float)(i * size / WowAtlas.ImageWidth), (float)(j * size / WowAtlas.ImageHeight),
				(float)(size / WowAtlas.ImageWidth), (float)(size / WowAtlas.ImageHeight))));
		}
		return list;
	}

	// Decoded tiles, most recently drawn last. Only the newest MaxTiles are kept (each is ~1.4 MB of video
	// memory; a full view at the zoom where zones appear needs about 45).
	const int MaxTiles = 120;
	static readonly Dictionary<string, Texture2D?> Loaded = new();
	static readonly LinkedList<string> Recent = new();
	static readonly Dictionary<string, Task<Image?>> Loading = new();

	/// <summary>A tile's texture, or null while it loads in the background (call again next frame).</summary>
	public static Texture2D? TileTexture(string path)
	{
		if (Loaded.TryGetValue(path, out var tex))
		{
			Recent.Remove(path);
			Recent.AddLast(path);
			return tex;
		}
		if (!Loading.TryGetValue(path, out var task))
		{
			Loading[path] = Task.Run(() => Images.LoadBuiltinImage(path));
			return null;
		}
		if (!task.IsCompleted) return null;
		Loading.Remove(path);
		Loaded[path] = tex = task.IsCompletedSuccessfully && task.Result is { } img ? ImageTexture.CreateFromImage(img) : null;
		Recent.AddLast(path);
		while (Recent.Count > MaxTiles)
		{
			Loaded.Remove(Recent.First!.Value);
			Recent.RemoveFirst();
		}
		return tex;
	}

	static readonly Dictionary<int, Texture2D?> Cut = new();

	/// <summary>A continent or city map with transparent edges (made once, on first use).</summary>
	public static Texture2D? Shaped(WowAtlas.Map map)
	{
		if (Cut.TryGetValue(map.Id, out var tex)) return tex;
		var source = Images.BuiltinTexture(WowAtlas.ImagePath(map.Id))?.GetImage();
		if (source == null) return Cut[map.Id] = null;
		var img = (Image)source.Duplicate();
		if (img.IsCompressed()) img.Decompress();
		img.ClearMipmaps();
		img.Convert(Image.Format.Rgba8);
		int w = img.GetWidth(), h = img.GetHeight();
		var data = img.GetData();
		var rect = WowAtlas.WorldRect(map);
		var place = map.Kind == WowAtlas.Kind.Continent ? WowAtlas.WorldPlacementOf(map.Continent!.Value) : default;
		for (var y = 0; y < h; y++)
		for (var x = 0; x < w; x++)
		{
			double u = (x + 0.5) / w, v = (y + 0.5) / h;
			double alpha;
			if (map.Kind == WowAtlas.Kind.Continent)
			{
				// Only where the world map draws this continent, fading out before the map's own frame.
				double wx = rect.X + u * rect.W, wy = rect.Y + v * rect.H;
				var inside = Math.Min(Math.Min(wx - place.U0, place.U1 - wx), Math.Min(wy - place.V0, place.V1 - wy));
				var edge = Math.Min(Math.Min(u, 1 - u), Math.Min(v, 1 - v));
				alpha = Smooth(-0.015, 0.02, inside) * Smooth(0, 0.05, edge);
			}
			else
			{
				// A city: round, so its map's dull surroundings drop away into the zone around it.
				var d = Math.Sqrt((u - 0.5) * (u - 0.5) + (v - 0.5) * (v - 0.5)) * 2;
				alpha = Smooth(0.95, 0.55, d);
			}
			data[(y * w + x) * 4 + 3] = (byte)(data[(y * w + x) * 4 + 3] * alpha);
		}
		var shaped = Image.CreateFromData(w, h, false, Image.Format.Rgba8, data);
		shaped.GenerateMipmaps();
		return Cut[map.Id] = ImageTexture.CreateFromImage(shaped);
	}

	static double Smooth(double from, double to, double x)
	{
		var t = Math.Clamp((x - from) / (to - from), 0, 1);
		return t * t * (3 - 2 * t);
	}
}

/// <summary>The seamless WoW map canvas: zoom from the whole world down into a city.</summary>
public partial class WowMapCanvas : MapCanvas
{
	static readonly WowAtlas.Map[] Continents = WowAtlas.Maps.Where(m => m.Kind == WowAtlas.Kind.Continent).ToArray();
	static readonly WowAtlas.Map[] Cities = WowAtlas.Maps.Where(m => m.City).ToArray();
	static readonly bool Headless = DisplayServer.GetName() == "headless";

	// The view set by Show, kept while the canvas settles its size (it resizes a few times while the
	// screen lays out) until the user zooms or drags themselves.
	(NormPoint Center, float Zoom)? _shown;

	public WowMapCanvas()
	{
		MaxZoom = 40;
		WheelStep = 0.2f;
		// (for the sea's tiled mottling; everything else is drawn from inside its texture, where repeat changes nothing)
		TextureRepeat = TextureRepeatEnum.Enabled;
		HoverLabel = p => DoorAt(FromNorm(p)) is { } door ? Lang.F("{0} (click to go in)", door.Label) : PlaceName(p);
	}

	/// <summary>Zooms to a spot, also before the canvas has its final size.</summary>
	public void Show(NormPoint center, float zoom)
	{
		_shown = (center, zoom);
		if (Size.X > 0) ZoomTo(center, zoom);
	}

	/// <summary>Zoom that fits a part of the world map (0–1 coordinates) into the view, with some room round it.</summary>
	public static float ZoomFor(double width, double height) =>
		(float)Math.Min(0.8 / Math.Max(width, 0.004), 0.8 / Math.Max(height, 0.006));

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized && _shown is { } v && Size.X > 0) ZoomTo(v.Center, v.Zoom);
	}

	public override void _GuiInput(InputEvent e)
	{
		if (e is InputEventMouseButton { Pressed: true } or InputEventMagnifyGesture or InputEventPanGesture) _shown = null;
		base._GuiInput(e);
	}

	static string? PlaceName(NormPoint p)
	{
		if (WowAtlas.WorldToLocation(p) is not { } loc) return null;
		return WowAtlas.ExtraZoneAt(p) ?? WowAtlas.ZoneAt(loc)?.Name ?? WowAtlas.ContinentMap(loc.Continent).Name;
	}

	static float Ramp(float from, float to, float x) => Mathf.SmoothStep(from, to, x);

	protected override void DrawMap(Rect2 rect)
	{
		// (no renderer without a window, e.g. bots: nothing to draw)
		if (Headless) return;
		var view = VisibleNorm();
		var zoom = Zoom;
		Rect2 OnScreen(Rect2 n) => new(rect.Position + n.Position * rect.Size, n.Size * rect.Size);
		static Rect2 R((double X, double Y, double W, double H) r) => new((float)r.X, (float)r.Y, (float)r.W, (float)r.H);
		var waiting = false;

		// OpenMap's own sea, taking over from the world map's (a 1002 px picture: blurry once zoomed in, and its coasts
		// soft) as the zone art comes in. The land is the zone art's by then (overview, then tiles), which carries its
		// coasts and the sea just off them.
		var sea = Ramp(3f, 5f, zoom);
		if (sea > 0)
		{
			if (WowMapArt.TileTexture(WowMapArt.Sea("field")) is { } field) DrawTextureRect(field, rect, false);
			else waiting = true;
			// Its paper mottling, fixed on the world; past zoom 4 the tile halves each time the zoom doubles,
			// crossfading to the finer one, so it keeps its size on screen (OpenMap's octaves).
			var octave = Mathf.Max(0, Mathf.Log(zoom / 4f) / Mathf.Log(2));
			var n = Mathf.FloorToInt(octave);
			var fine = octave - n;
			var size = WowMapArt.SeaTile / Mathf.Pow(2, n);
			var part = view.Intersection(new Rect2(0, 0, 1, 1));
			foreach (var name in new[] { "dark", "light" })
			{
				if (WowMapArt.TileTexture(WowMapArt.Sea(name)) is not { } paper) { waiting = true; continue; }
				SeaPaper(paper, OnScreen(part), part, size, sea * (1 - fine), n);
				SeaPaper(paper, OnScreen(part), part, size / 2, sea * fine, n + 1);
			}
		}
		if (sea < 1 && Images.BuiltinTexture(WowAtlas.ImagePath(WowAtlas.World)) is { } world)
			DrawTextureRect(world, rect, false, new Color(1, 1, 1, 1 - sea));

		var continents = Ramp(1.3f, 2.2f, zoom) * (1 - sea);
		if (continents > 0)
			foreach (var c in Continents)
			{
				var at = R(WowAtlas.WorldRect(c));
				if (at.Intersects(view) && WowMapArt.Shaped(c) is { } tex) DrawTextureRect(tex, OnScreen(at), false, new Color(1, 1, 1, continents));
			}

		// The zone art put together (tools/openmap_overview.py), the size that's sharp enough for this zoom: from the
		// first zoom steps on, the land looks the same as in the zone tiles, only less detailed.
		var perWorldPx = rect.Size.X * GetViewport().GetFinalTransform().Scale.X / WowAtlas.ImageWidth;
		var overview = Ramp(1.1f, 1.5f, zoom);
		if (overview > 0)
		{
			var sharp = perWorldPx > 2 ? WowMapArt.TileTexture(WowMapArt.Overview(4)) : null;
			var art = sharp ?? WowMapArt.TileTexture(WowMapArt.Overview(2));
			if (art != null) DrawTextureRect(art, rect, false, new Color(1, 1, 1, overview));
			if (art == null || perWorldPx > 2 && sharp == null) waiting = true;
		}

		// The tiles themselves once the overview's pixels would show (OpenMap's 16 px per world-map pixel).
		var zones = Ramp(4f, 6f, perWorldPx);
		if (zones > 0)
			foreach (var tile in WowMapArt.Tiles)
			{
				if (!tile.World.Intersects(view)) continue;
				// (a hair larger, so neighbouring tiles overlap instead of leaving a hairline between them)
				var at = OnScreen(tile.World).Grow(0.5f);
				if (WowMapArt.TileTexture(tile.Path) is { } tex) DrawTextureRectRegion(tex, at, WowMapArt.TileSource, new Color(1, 1, 1, zones));
				else waiting = true;
			}

		// The game's minimap over the painted zones from 16× on, like OpenMap's minimap detail: a plain blend (adding
		// would blow out pale zones), half strength by 20×, more as you go in. Graded and cut to the land in the bake.
		var mini = Ramp(16f, 20f, zoom) * 0.5f + Ramp(26f, 40f, zoom) * 0.25f;
		if (mini > 0)
			foreach (var piece in WowMapArt.Minimap)
			{
				if (!piece.World.Intersects(view)) continue;
				// (edges on whole pixels, so neighbours meet exactly: half see-through, any overlap would show as a dark
				// line; and half a pixel in from the texture's edge, as the canvas repeats textures)
				if (WowMapArt.TileTexture(piece.Path) is { } tex)
				{
					var s = OnScreen(piece.World);
					var at = new Rect2(s.Position.Round(), s.End.Round() - s.Position.Round());
					DrawTextureRectRegion(tex, at, new Rect2(0.5f, 0.5f, tex.GetWidth() - 1, tex.GetHeight() - 1), new Color(1, 1, 1, mini));
				}
				else waiting = true;
			}

		foreach (var city in Cities)
		{
			var at = R(WowAtlas.WorldRect(city));
			if (!at.Intersects(view)) continue;
			// Its own map takes over once the city fills a fifth to two fifths of the view.
			var share = Mathf.Max(at.Size.X / view.Size.X, at.Size.Y / view.Size.Y);
			var a = Ramp(0.2f, 0.4f, share) * zones;
			if (a > 0 && WowMapArt.Shaped(city) is { } tex) DrawTextureRect(tex, OnScreen(at), false, new Color(1, 1, 1, a));
		}

		DrawDoors();

		// Tiles still loading: look again next frame.
		if (waiting) Callable.From(QueueRedraw).CallDeferred();
	}

	// ---------- dungeon entrances ----------

	/// <summary>A click on a dungeon entrance (WowAtlasView goes in).</summary>
	public Action<WowDungeons.Entrance>? OnEntrance;
	const float DoorSize = 7.5f;
	// (only zoomed far in, where you'd look for an entrance; Max's call)
	float DoorAlpha => Ramp(14f, 16f, Zoom);

	WowDungeons.Entrance? DoorAt(Vector2 at) =>
		OnEntrance == null || DoorAlpha < 0.5f ? null
			: WowDungeons.Entrances.Select(e => (e, d: FromNorm(e.OnWorld).DistanceTo(at))).Where(x => x.d <= DoorSize + 4)
				.OrderBy(x => x.d).Select(x => x.e).FirstOrDefault();

	protected override bool ClickAt(Vector2 at)
	{
		if (DoorAt(at) is not { } door) return false;
		OnEntrance!(door);
		return true;
	}

	/// <summary>Each entrance as a little swirling portal, like the game's own entrance icon: blue for dungeons, orange for raids.</summary>
	void DrawDoors()
	{
		var alpha = DoorAlpha;
		if (alpha <= 0.01f || OnEntrance == null) return;
		var ink = new Color("#14151b", alpha);
		var rim = new Color("#d8c9a0", alpha);
		foreach (var door in WowDungeons.Entrances)
		{
			var c = FromNorm(door.OnWorld);
			if (!new Rect2(Vector2.Zero, Size).Grow(DoorSize * 2).HasPoint(c)) continue;
			var raid = door.Dungeons.Any(d => d.Raid);
			var deep = raid ? new Color("#5a1d08", alpha) : new Color("#10264f", alpha);
			var glow = raid ? new Color("#ffa040", alpha) : new Color("#8cc4ff", alpha);
			DrawCircle(c, DoorSize + 1.5f, ink, true, -1, true);
			DrawCircle(c, DoorSize, rim, true, -1, true);
			DrawCircle(c, DoorSize - 1.6f, deep, true, -1, true);
			// Three arms winding in towards a bright eye, thick inside and thinning out.
			const int arms = 3, steps = 14;
			for (var k = 0; k < arms; k++)
			{
				var prev = c;
				for (var i = 1; i <= steps; i++)
				{
					var t = (float)i / steps;
					var angle = k * Mathf.Tau / arms + t * Mathf.Pi * 1.35f;
					var at = c + Vector2.FromAngle(angle) * (DoorSize - 2.4f) * t;
					DrawLine(prev, at, glow, Mathf.Lerp(2.4f, 0.9f, t), true);
					prev = at;
				}
			}
			DrawCircle(c, 1.8f, new Color(1, 1, 1, alpha), true, -1, true);
		}
	}

	/// <summary>One octave of the sea's mottling over <paramref name="part"/> of the world (0–1), in tiles of
	/// <paramref name="size"/> world-map pixels. Each octave has its own offset, so the finer one taking over as the zoom
	/// doubles is the same pattern in the same place (OpenMap's shift).</summary>
	void SeaPaper(Texture2D tex, Rect2 at, Rect2 part, float size, float alpha, int octave)
	{
		if (alpha <= 0.01f || part.Size.X <= 0 || part.Size.Y <= 0) return;
		var shift = octave * 0.37f % 1;
		var tw = tex.GetWidth();
		var th = tex.GetHeight();
		float u0 = part.Position.X * WowAtlas.ImageWidth / size + shift, v0 = part.Position.Y * WowAtlas.ImageHeight / size + shift * 1.6f;
		float u1 = part.End.X * WowAtlas.ImageWidth / size + shift, v1 = part.End.Y * WowAtlas.ImageHeight / size + shift * 1.6f;
		// (the canvas repeats textures, so a source past the texture's edge tiles it)
		DrawTextureRectRegion(tex, at, new Rect2(u0 * tw, v0 * th, (u1 - u0) * tw, (v1 - v0) * th), new Color(1, 1, 1, alpha));
	}
}

/// <summary>
/// The WoW Forever map for map pin questions: one seamless map from the whole world down to the cities
/// (scroll to zoom, drag to move, or jump straight to a zone). Pins are stored as a point on the world
/// map (see WowAtlas), or inside a dungeon: its entrances lead to the floor maps (WowDungeons).
/// </summary>
public partial class WowAtlasView : VBoxContainer
{
	public readonly WowMapCanvas Canvas = new();
	// Inside a dungeon: Blizzard's map of the floor, with its own pins (WowDungeons).
	readonly MapCanvas _floor = new() { MaxZoom = 4 };
	readonly HBoxContainer _jumpRow;
	readonly MapView _world;
	readonly VBoxContainer _inside = new();
	readonly HBoxContainer _head = U.H(8);
	readonly List<MapPin> _pins;
	readonly Action<NormPoint>? _onPick;
	NormPoint? _target;
	(int Dungeon, int Floor)? _in;

	public WowAtlasView() : this(null, null) { }

	public WowAtlasView(NormPoint? target, Action<NormPoint>? onPick, List<MapPin>? pins = null, float maxHeight = 560)
	{
		AddThemeConstantOverride("separation", 8);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_pins = pins ?? new List<MapPin>();
		_target = target;
		_onPick = onPick;

		var places = WowAtlas.Maps.Where(m => m.Kind == WowAtlas.Kind.Zone).OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
		var items = new List<(string, string)> { ("", "Jump to zone…") };
		// (WoW Forever's new zones too, by name: they have no Classic zone map)
		items.AddRange(places.Select(z => (z.Id.ToString(), z.Name))
			.Concat(WowAtlas.ExtraZones.Select(z => ("extra:" + z.Name, z.Name))).OrderBy(i => i.Item2, StringComparer.Ordinal));
		var jump = U.Select(items.ToArray(), "", id =>
		{
			if (id.StartsWith("extra:"))
			{
				var e = WowAtlas.ExtraZoneRect(id[6..]);
				Canvas.Show(new NormPoint(e.X + e.W / 2, e.Y + e.H / 2), WowMapCanvas.ZoomFor(e.W, e.H));
				return;
			}
			if (!int.TryParse(id, out var zoneId)) return;
			var r = WowAtlas.WorldRect(WowAtlas.Get(zoneId));
			Canvas.Show(new NormPoint(r.X + r.W / 2, r.Y + r.H / 2), WowMapCanvas.ZoomFor(r.W, r.H));
		});
		jump.CustomMinimumSize = new Vector2(220, 0);
		_jumpRow = U.H(8, U.Fill(), jump);
		AddChild(_jumpRow);

		// On the world map a pin inside a dungeon stands at its entrance.
		Canvas.Pins = _pins.Select(p => p with { At = WowDungeons.OnWorld(p.At) }).ToList();
		Canvas.Target = target is { } t0 ? WowDungeons.OnWorld(t0) : null;
		Canvas.OnEntrance = door => Enter(door.Dungeons.First().Id, 0);
		if (onPick != null)
		{
			Canvas.OnPick = Pick;
			_floor.OnPick = p =>
			{
				if (_in is { } at) Pick(WowDungeons.Encode(at.Dungeon, at.Floor, p));
			};
		}
		_floor.OnBack = Leave;
		_world = new MapView("", null, WowAtlas.ImageWidth, WowAtlas.ImageHeight, maxHeight, Canvas);
		AddChild(_world);

		_inside.AddThemeConstantOverride("separation", 8);
		_inside.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_inside.AddChild(_head);
		_inside.AddChild(new MapView("", null, WowAtlas.ImageWidth, WowAtlas.ImageHeight, maxHeight, _floor));
		_inside.Visible = false;
		if (maxHeight <= 0) _inside.SizeFlagsVertical = SizeFlags.ExpandFill;
		AddChild(_inside);

		if (maxHeight <= 0) SizeFlagsVertical = SizeFlags.ExpandFill;
		if (onPick != null)
			AddChild(U.Muted("Zoom in for more detail: first the zones, then the cities. From 16× the swirls are dungeon and raid entrances: click one to go inside. Click to place the pin.", 12));

		// Start on what matters: the pin's dungeon floor, the pin and everyone's guesses, or the zone of an existing pin.
		if (target is { } tt && WowDungeons.Decode(tt) is { } spot)
		{
			Canvas.Show(WowDungeons.EntranceOf(spot.Dungeon).OnWorld, 8);
			Enter(spot.Dungeon, spot.Floor);
			return;
		}
		var points = Canvas.Pins.Select(p => p.At).ToList();
		if (Canvas.Target is { } t) points.Add(t);
		if (points.Count == 1 && WowAtlas.WorldToLocation(points[0]) is { } loc && WowAtlas.ZoneAt(loc) is { } zone)
		{
			var r = WowAtlas.WorldRect(zone);
			Canvas.Show(points[0], WowMapCanvas.ZoomFor(r.W, r.H));
		}
		else if (points.Count > 1)
		{
			double x0 = points.Min(p => p.X), x1 = points.Max(p => p.X), y0 = points.Min(p => p.Y), y1 = points.Max(p => p.Y);
			Canvas.Show(new NormPoint((x0 + x1) / 2, (y0 + y1) / 2), Math.Min(WowMapCanvas.ZoomFor(x1 - x0, y1 - y0), 16));
		}
	}

	/// <summary>The dungeon and floor shown, or null on the world map.</summary>
	public (int Dungeon, int Floor)? Inside => _in;

	void Pick(NormPoint p)
	{
		_target = p;
		Canvas.Target = WowDungeons.OnWorld(p);
		Canvas.QueueRedraw();
		ShowFloorPins();
		_onPick!(p);
	}

	/// <summary>Goes into a dungeon's floor (0-based), with a way back, its floors and the other dungeons at the same entrance.</summary>
	public void Enter(int dungeon, int floor)
	{
		var d = WowDungeons.Get(dungeon)!;
		_in = (dungeon, floor);
		foreach (var kid in _head.GetChildren())
		{
			_head.RemoveChild(kid);
			kid.QueueFree();
		}
		_head.AddChild(U.Small(Lang.T("Back to the map"), Leave).On(Glyph.Kind.Left, 14).Tip("Or right-click the map"));
		// (the drop-downs share the row and cut their text short rather than widening the screen)
		static OptionButton Narrow(OptionButton o, float ratio)
		{
			o.ClipText = true;
			o.FitToLongestItem = false;
			o.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			o.SizeFlagsStretchRatio = ratio;
			o.CustomMinimumSize = new Vector2(90, 0);
			return o;
		}
		var door = WowDungeons.EntranceOf(dungeon);
		var others = door.Dungeons.ToList();
		if (others.Count > 1)
		{
			// (its name is in the drop-down: Blackrock Mountain's and Ahn'Qiraj's entrances lead into several places)
			var pick = Narrow(U.Select(others.Select(o => (o.Id.ToString(), o.Name)).ToArray(), dungeon.ToString(), id => Enter(int.Parse(id), 0)), 1);
			pick.TooltipText = Lang.T("This entrance leads into more than one place");
			_head.AddChild(pick);
		}
		else
		{
			var title = U.Bold(d.Name, 15);
			title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			title.ClipText = true;
			_head.AddChild(title);
		}
		if (d.Floors.Length > 1)
		{
			// (how many pins are on each floor, so a reveal shows where everyone went)
			var floors = Enumerable.Range(0, d.Floors.Length).Select(i =>
			{
				var n = _pins.Count(p => WowDungeons.Decode(p.At) is { } s && s.Dungeon == dungeon && s.Floor == i);
				return (i.ToString(), n > 0 ? $"{d.FloorLabel(i)}  ({n})" : d.FloorLabel(i));
			}).ToArray();
			_head.AddChild(Narrow(U.Select(floors, floor.ToString(), id => Enter(dungeon, int.Parse(id))), 1.6f));
		}
		_floor.SetTexture(Images.BuiltinTexture(WowDungeons.ArtPath(dungeon, floor)), WowAtlas.ImageWidth, WowAtlas.ImageHeight);
		ShowFloorPins();
		_jumpRow.Visible = false;
		_world.Visible = false;
		_inside.Visible = true;
	}

	void ShowFloorPins()
	{
		if (_in is not { } at) return;
		WowDungeons.Spot? Here(NormPoint p) => WowDungeons.Decode(p) is { } s && s.Dungeon == at.Dungeon && s.Floor == at.Floor ? s : null;
		_floor.Pins = _pins.Where(p => Here(p.At) != null).Select(p => p with { At = Here(p.At)!.Value.At }).ToList();
		_floor.Target = _target is { } t && Here(t) is { } spot ? spot.At : null;
		_floor.QueueRedraw();
	}

	/// <summary>Back out to the world map, at the dungeon's entrance.</summary>
	public void Leave()
	{
		if (_in is not { } at) return;
		_in = null;
		_inside.Visible = false;
		_world.Visible = true;
		_jumpRow.Visible = true;
		Canvas.Show(WowDungeons.EntranceOf(at.Dungeon).OnWorld, Mathf.Max(Canvas.Zoom, 6));
	}
}
