// The dungeons and raids on the WoW map: their entrances on the world map and Blizzard's own map of each floor inside
// (the data in WowDungeons.Data.cs, made by tools/wow_dungeons.py from the OpenMap addon; the floor pictures in
// res://assets/maps/wow/dungeons/, from the game's files).
//
// A pin inside a dungeon is stored like every WoW pin, as one NormPoint, so quiz files and the protocol stay as they
// are: X = 10 × the dungeon's map id + the spot's x on the floor's picture (0–1), Y = 10 × the floor (0-based) + its y.
// World pins have X below 1, so anything from 10 on is inside a dungeon.
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuizNight.Shared;

public static partial class WowDungeons
{
	public sealed record Floor(string? Name, string? Wing);

	public sealed record Dungeon(int Id, string Name, bool Raid, int? MinLevel, int? MaxLevel, Floor[] Floors)
	{
		/// <summary>The floor's name for lists: its own name (or "Floor 2"), with its wing in front if the dungeon has several.</summary>
		public string FloorLabel(int i)
		{
			var f = Floors[i];
			var name = f.Name ?? (Floors.Length == 1 ? Name : Lang.F("Floor {0}", i + 1));
			if (f.Wing is not { } wing) return name;
			// ("Dire Maul - North" → "North")
			var shortWing = wing.StartsWith(Name + " - ") ? wing[(Name.Length + 3)..] : wing;
			return $"{shortWing}: {name}";
		}
	}

	/// <summary>A dungeon entrance in the world (yards, X north, Y west); one spot can lead into several (Blackrock Mountain).</summary>
	public sealed record Entrance(int Continent, double X, double Y, (int Id, string Name)[] Ways)
	{
		public NormPoint OnWorld => WowAtlas.LocationToWorld(new WowAtlas.Location(Continent, X, Y));
		/// <summary>The dungeons behind it, each once (Scarlet Monastery's four wings are one map).</summary>
		public IEnumerable<Dungeon> Dungeons => Ways.Select(w => w.Id).Distinct().Select(id => Get(id)!);
		public string Label => string.Join(", ", Dungeons.Select(d => d.Name));
	}

	public static IReadOnlyList<Dungeon> List => All;
	public static IReadOnlyList<Entrance> Entrances => Doors;

	public static Dungeon? Get(int id) => All.FirstOrDefault(d => d.Id == id);
	public static Entrance EntranceOf(int id) => Doors.First(d => d.Ways.Any(w => w.Id == id));

	public static string ArtPath(int id, int floor) => $"res://assets/maps/wow/dungeons/{id}_{floor + 1}.webp";

	// ---------- pins inside ----------

	const double Step = 10;

	/// <summary>A spot inside: the dungeon, its floor (0-based) and where on the floor's picture (0–1).</summary>
	public readonly record struct Spot(int Dungeon, int Floor, NormPoint At);

	public static bool IsInside(NormPoint p) => p.X >= Step;

	public static NormPoint Encode(int dungeon, int floor, NormPoint at) =>
		new(dungeon * Step + Math.Clamp(at.X, 0, 0.9999), floor * Step + Math.Clamp(at.Y, 0, 0.9999));

	/// <summary>The spot a stored pin is inside, or null for a pin in the world (or one that names no dungeon we know).</summary>
	public static Spot? Decode(NormPoint p)
	{
		if (!IsInside(p) || !double.IsFinite(p.X) || !double.IsFinite(p.Y) || p.Y < 0) return null;
		var id = (int)Math.Floor(p.X / Step);
		var floor = (int)Math.Floor(p.Y / Step);
		if (Get(id) is not { } d || floor >= d.Floors.Length) return null;
		return new Spot(id, floor, new NormPoint(p.X - id * Step, p.Y - floor * Step));
	}

	/// <summary>A player's pin made valid: inside a known dungeon's floor, or on the world map (0–1).</summary>
	public static NormPoint Clamp(NormPoint p) =>
		Decode(p) is { } s ? Encode(s.Dungeon, s.Floor, s.At) : new NormPoint(Math.Clamp(p.X, 0, 1), Math.Clamp(p.Y, 0, 1));

	/// <summary>Where a pin is on the world map: a pin inside a dungeon is at its entrance.</summary>
	public static NormPoint OnWorld(NormPoint p) => Decode(p) is { } s ? EntranceOf(s.Dungeon).OnWorld : p;

	// The floors' pictures have no scale in the game's tables, so distances inside use a typical one: a floor's picture
	// about 500 yards wide. The wrong floor and the wrong place always rank behind the right one (Max's call: "find the
	// dungeon yourself"), nearer guesses still before farther ones.
	const double FloorYards = 500;
	const double WrongFloorYards = 700;   // (more than any spot on the right floor: 500 × 1.2 at most)
	const double OutsideYards = 2000;     // (more than any spot in the right dungeon)
	const double YardKm = 0.9144 / 1000;

	/// <summary>Distance between two pins when at least one is inside a dungeon (else null: WowAtlas measures it).</summary>
	public static double? DistanceKm(NormPoint a, NormPoint b)
	{
		var sa = Decode(a);
		var sb = Decode(b);
		if (sa == null && sb == null) return null;
		if (sa is { } x && sb is { } y && x.Dungeon == y.Dungeon)
		{
			var aspect = (double)WowAtlas.ImageHeight / WowAtlas.ImageWidth;
			var local = Math.Sqrt(Math.Pow(x.At.X - y.At.X, 2) + Math.Pow((x.At.Y - y.At.Y) * aspect, 2)) * FloorYards;
			return (x.Floor == y.Floor ? local : WrongFloorYards + local) * YardKm;
		}
		return OutsideYards * YardKm + WowAtlas.DistanceKm(OnWorld(a), OnWorld(b));
	}
}
