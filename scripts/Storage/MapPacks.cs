// The built-in maps as an optional download (from 0.12.8b). Exported games no longer ship them: each built-in map is
// one pack ("skyrim", or "wow-azeroth" for the whole WoW Forever map with its tiles) on the public releases repo's "maps"
// release (test builds: the private test repo's, so maps are tried there first), listed in maps.json (each pack's zip with its SHA-256 and files; signed with Max's release key like updates,
// made by tools/map_packs.py). A map is downloaded the first time it's needed: picked in the editor, a quiz using it
// opened or hosted. It's kept in user://maps/ (installed.json says which packs are complete) until a quiz using it is
// deleted with "Also delete its maps". Players don't download anything: the host sends the maps a quiz uses with the
// rest of its files (Game.ShareFiles, "builtin:ID"); a player unpacks them into their game's media cache
// (ClientMedia), registered here with AddTemp, and they go with it after the night.
// Editor and `godot --path .` runs use res://assets/maps/ as before (every map always there), unless --map-packs
// makes them act like an export (user://maps/, downloads and all).

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using QuizNight.Server;
using QuizNight.Shared;
using HttpClient = System.Net.Http.HttpClient;

namespace QuizNight.Storage;

public static class MapPacks
{
	public const string MapsRes = "res://assets/maps/";
	public const string ZipMime = "application/zip";
	const string ReleaseTag = "maps";

	/// <summary>Dev runs acting like an export: maps from user://maps/, downloaded when missing (--map-packs).</summary>
	public static bool Simulate;
	/// <summary>Take the maps from the test repo like a test build (--maps-from-test-repo, with --map-packs-check).</summary>
	public static bool FromTestRepo;
	/// <summary>Maps live in user://maps/ and may be missing (exported builds, or Simulate).</summary>
	public static bool Optional => OS.HasFeature("template") || Simulate;
	static string Dir => ProjectSettings.GlobalizePath("user://maps");
	static string InstalledFile => Path.Combine(Dir, "installed.json");

	/// <summary>A map file's path under the maps folder ("skyrim.webp", "wow/stitched/3_4.webp").</summary>
	public static string Rel(string resPath) => resPath.StartsWith(MapsRes) ? resPath[MapsRes.Length..] : resPath;

	/// <summary>The pack a built-in map's files are in: the preset's id.</summary>
	public static string PackOf(MapPreset preset) => preset.Id;

	/// <summary>
	/// Where a map file is, for Images.BuiltinFile: res:// in dev runs; else user://maps/, or a game's unpacked copy
	/// from the host (AddTemp). The user://maps/ path if it's nowhere (it doesn't exist then).
	/// </summary>
	public static string FileFor(string resPath)
	{
		if (!Optional || !resPath.StartsWith(MapsRes)) return resPath;
		var rel = Rel(resPath);
		lock (Gate)
			foreach (var root in TempRoots.Values)
			{
				var temp = Path.Combine(root, rel);
				if (File.Exists(temp)) return temp;
			}
		return Path.Combine(Dir, rel).Replace('\\', '/');
	}

	// ---------- what's here ----------

	static readonly object Gate = new();
	/// <summary>Complete packs in user://maps/: id → its zip's SHA-256 ("" = moved there from an older install's maps folder).</summary>
	static Dictionary<string, (string Sha256, List<string> Files)>? _installed;
	/// <summary>Packs a player's game unpacked from the host's files: id → the folder.</summary>
	static readonly Dictionary<string, string> TempRoots = new();

	static Dictionary<string, (string Sha256, List<string> Files)> Installed
	{
		get
		{
			lock (Gate)
			{
				if (_installed != null) return _installed;
				_installed = new();
				try
				{
					if (File.Exists(InstalledFile) && JsonNode.Parse(File.ReadAllText(InstalledFile)) is JsonObject o)
						foreach (var (id, v) in o)
							if (v is JsonObject p)
								_installed[id] = (p["sha256"]?.GetValue<string>() ?? "",
									(p["files"] as JsonArray)?.Select(f => f?.GetValue<string>() ?? "").Where(f => f != "").ToList() ?? new());
				}
				catch (Exception e)
				{
					GD.Print($"Couldn't read the installed maps list: {e.Message}");
				}
				return _installed;
			}
		}
	}

	static void SaveInstalled()
	{
		lock (Gate)
		{
			Directory.CreateDirectory(Dir);
			var o = new JsonObject();
			foreach (var (id, p) in Installed)
				o[id] = new JsonObject { ["sha256"] = p.Sha256, ["files"] = new JsonArray(p.Files.Select(f => (JsonNode)JsonValue.Create(f)!).ToArray()) };
			File.WriteAllText(InstalledFile, o.ToJsonString());
		}
	}

	/// <summary>Whether this built-in map can be shown right now (dev runs: always).</summary>
	public static bool Has(string? presetId)
	{
		if (!Optional || presetId == null) return true;
		lock (Gate) return Installed.ContainsKey(presetId) || TempRoots.ContainsKey(presetId);
	}

	/// <summary>Downloaded to this PC (not only unpacked for one game).</summary>
	public static bool IsDownloaded(string presetId) => !Optional || Installed.ContainsKey(presetId);

	/// <summary>The built-in maps a quiz's map pin questions use.</summary>
	public static List<MapPreset> Used(Quiz quiz) =>
		quiz.Questions.Where(q => q.Type == QuestionType.MapPin)
			.Select(q => MapPresets.Get(quiz.MapById(q.MapId)?.PresetId))
			.OfType<MapPreset>().Distinct().ToList();

	/// <summary>The built-in maps a quiz uses that aren't on this PC.</summary>
	public static List<MapPreset> Missing(Quiz quiz) => Used(quiz).Where(p => !Has(p.Id)).ToList();

	/// <summary>How big the downloaded maps are on disk (for the delete question), 0 if not downloaded.</summary>
	public static long SizeOnDisk(MapPreset preset)
	{
		if (!Optional || !Installed.TryGetValue(preset.Id, out var p)) return 0;
		return p.Files.Select(f => new FileInfo(Path.Combine(Dir, f))).Where(f => f.Exists).Sum(f => f.Length);
	}

	/// <summary>Deletes downloaded maps (Delete a quiz: "Also delete its maps").</summary>
	public static void Delete(IEnumerable<string> presetIds)
	{
		if (!Optional) return;
		lock (Gate)
		{
			foreach (var id in presetIds)
			{
				if (!Installed.Remove(id, out var p)) continue;
				foreach (var f in p.Files)
				{
					try
					{
						File.Delete(Path.Combine(Dir, f));
					}
					catch (Exception e)
					{
						GD.Print($"Couldn't delete map file {f}: {e.Message}");
					}
				}
			}
			SaveInstalled();
		}
		Forget();
	}

	/// <summary>Map files came or went: drop the cached pictures (missing ones were cached as missing).</summary>
	static void Forget() => Callable.From(() =>
	{
		Images.ForgetBuiltin(MapsRes);
		Ui.WowMapArt.Forget();
	}).CallDeferred();

	// ---------- a player's copies from the host ----------

	/// <summary>A pack the host sent, unpacked into folder (a player's media cache): usable until RemoveTemp.</summary>
	public static void AddTemp(string presetId, string folder)
	{
		lock (Gate) TempRoots[presetId] = folder;
		Forget();
	}

	/// <summary>The game's media cache is going: its unpacked maps with it.</summary>
	public static void RemoveTemp(string folderPrefix)
	{
		lock (Gate)
			foreach (var id in TempRoots.Where(kv => kv.Value.StartsWith(folderPrefix)).Select(kv => kv.Key).ToList())
				TempRoots.Remove(id);
		Forget();
	}

	/// <summary>Unpacks a pack zip into folder: only plain .webp paths inside it (checked like everything from outside).</summary>
	public static List<string> Unpack(byte[] zip, string folder)
	{
		var root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
		var files = new List<string>();
		using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
		if (archive.Entries.Count > 2000) throw new InvalidDataException("Too many files in the map pack.");
		long total = 0;
		foreach (var entry in archive.Entries)
		{
			if (entry.FullName.EndsWith('/')) continue;
			var name = entry.FullName.Replace('\\', '/');
			var target = Path.GetFullPath(Path.Combine(folder, name));
			if (!target.StartsWith(root, StringComparison.Ordinal) || !name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || name.Contains(".."))
				throw new InvalidDataException($"Bad file name in the map pack: {name}");
			total += entry.Length;
			if (entry.Length > 64L << 20 || total > 512L << 20) throw new InvalidDataException("The map pack is too big.");
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			entry.ExtractToFile(target, true);
			files.Add(name);
		}
		return files;
	}

	// ---------- what the host sends ----------

	static readonly Dictionary<string, string?> Bundles = new();

	/// <summary>
	/// A built-in map as one file for players (Game.ShareFiles): the map's WebP, or for a map of many files (WoW) a zip of
	/// them. Null if this PC doesn't have it. Cached per run.
	/// </summary>
	public static string? BundleDataUrl(MapPreset preset)
	{
		lock (Gate)
			if (Bundles.TryGetValue(preset.Id, out var cached)) return cached;
		string? url = null;
		try
		{
			var files = FilesOf(preset);
			if (files.Count == 1 && Images.BuiltinFile(MapsRes + files[0]) is var one && Godot.FileAccess.FileExists(one))
				url = DataUrl.Make("image/webp", Godot.FileAccess.GetFileAsBytes(one));
			else if (files.Count > 1)
			{
				using var ms = new MemoryStream();
				using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
					foreach (var f in files)
					{
						var bytes = Godot.FileAccess.GetFileAsBytes(Images.BuiltinFile(MapsRes + f));
						if (bytes.Length == 0) throw new FileNotFoundException(f);
						using var to = zip.CreateEntry(f, CompressionLevel.NoCompression).Open();
						to.Write(bytes);
					}
				url = DataUrl.Make(ZipMime, ms.ToArray());
			}
		}
		catch (Exception e)
		{
			GD.Print($"Couldn't pack map {preset.Id} for the players: {e.Message}");
			url = null;
		}
		lock (Gate) Bundles[preset.Id] = url;
		return url;
	}

	/// <summary>A pack's files under the maps folder: from installed.json, or (dev runs) the preset's file / the WoW folder.</summary>
	public static List<string> FilesOf(MapPreset preset)
	{
		if (Optional && Installed.TryGetValue(preset.Id, out var p)) return p.Files;
		if (Optional) return new();
		if (preset.Id != WowAtlas.PresetId) return new() { Rel(preset.ImagePath) };
		var list = new List<string>();
		void Walk(string res)
		{
			using var dir = DirAccess.Open(res);
			if (dir == null) return;
			foreach (var f in dir.GetFiles()) if (f.EndsWith(".webp")) list.Add(Rel(res + f));
			foreach (var d in dir.GetDirectories()) Walk(res + d + "/");
		}
		Walk(MapsRes + "wow/");
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	// ---------- downloading ----------

	sealed record Pack(string File, long Size, string Sha256, List<(string Path, long Size, string Sha256)> Files);

	/// <summary>The signed list of packs, fetched once per run.</summary>
	static Task<Dictionary<string, Pack>?>? _list;

	static async Task<Dictionary<string, Pack>?> FetchList()
	{
		try
		{
			using var http = NewClient(TimeSpan.FromSeconds(20));
			var bytes = await Fetch(http, "maps.json");
			var sig = await Fetch(http, "maps.json.sig");
			if (!Updater.SignedByMax(SHA256.HashData(bytes), sig))
			{
				GD.Print("The map list isn't signed with Max's release key: not used.");
				return null;
			}
			var packs = new Dictionary<string, Pack>();
			if ((JsonNode.Parse(bytes) as JsonObject)?["packs"] is not JsonObject list) return packs;
			foreach (var (id, v) in list)
				if (v is JsonObject p)
					packs[id] = new Pack(p["file"]!.GetValue<string>(), p["size"]!.GetValue<long>(), p["sha256"]!.GetValue<string>(),
						(p["files"] as JsonArray)?.OfType<JsonObject>()
							.Select(f => (f["path"]!.GetValue<string>(), f["size"]!.GetValue<long>(), f["sha256"]!.GetValue<string>())).ToList() ?? new());
			return packs;
		}
		catch (Exception e)
		{
			GD.Print($"Couldn't get the map list: {e.Message}");
			Trace.Log("maps", $"list failed: {e.Message}");
			_testAssets = null;
			return null;
		}
	}

	static HttpClient NewClient(TimeSpan timeout)
	{
		var http = new HttpClient { Timeout = timeout };
		http.DefaultRequestHeaders.UserAgent.ParseAdd("Quiznight-Maps");
		return http;
	}

	/// <summary>Test builds: the private test repo's "maps" release, file name → its API address (fetched once).</summary>
	static Dictionary<string, string>? _testAssets;

	/// <summary>
	/// One file of the maps release (maps.json, its .sig, a pack), with progress. From the public releases repo; test
	/// builds take it from the private test repo instead (with the token they ship, like their updates), so new maps are
	/// tried in a test build before they go public (tools/map_packs.sh --test). Debug builds: QUIZNIGHT_MAPS_FEED, a
	/// folder or address with the files (tools/map_packs.sh --dry-run makes one).
	/// </summary>
	static async Task<byte[]> Fetch(HttpClient http, string name, Action<int>? read = null, long max = 64L << 20)
	{
		var feed = OS.IsDebugBuild() ? OS.GetEnvironment("QUIZNIGHT_MAPS_FEED").TrimEnd('/', '\\') : "";
		if (feed.Length > 0 && !feed.StartsWith("http"))
		{
			var bytes = await File.ReadAllBytesAsync(Path.Combine(feed, name));
			read?.Invoke(bytes.Length);
			return bytes;
		}
		HttpRequestMessage request;
		if (feed.Length > 0) request = new HttpRequestMessage(HttpMethod.Get, feed + "/" + name);
		else if (Updater.TestChannel || FromTestRepo)
		{
			if (ResultsUpload.Token is not { } token) throw new InvalidOperationException("no token for the test repo");
			if (_testAssets == null)
			{
				using var ask = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Updater.TestRepo}/releases/tags/{ReleaseTag}");
				ask.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				using var answer = await http.SendAsync(ask);
				answer.EnsureSuccessStatusCode();
				_testAssets = ((JsonNode.Parse(await answer.Content.ReadAsStringAsync()) as JsonObject)?["assets"] as JsonArray)?.OfType<JsonObject>()
					.Where(a => a["name"] != null && a["url"] != null)
					.ToDictionary(a => a["name"]!.GetValue<string>(), a => a["url"]!.GetValue<string>()) ?? new();
			}
			if (!_testAssets.TryGetValue(name, out var url)) throw new FileNotFoundException($"{name} isn't on the test repo's maps release");
			// (the asset's API address: with the token and "octet-stream" it sends you on to the file; the token isn't passed on)
			request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
			request.Headers.Accept.ParseAdd("application/octet-stream");
		}
		else request = new HttpRequestMessage(HttpMethod.Get, $"https://github.com/{Updater.ReleasesRepo}/releases/download/{ReleaseTag}/{name}");
		using (request)
		{
			using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();
			await using var from = await response.Content.ReadAsStreamAsync();
			using var ms = new MemoryStream();
			var buffer = new byte[1 << 16];
			int n;
			while ((n = await from.ReadAsync(buffer)) > 0)
			{
				ms.Write(buffer, 0, n);
				read?.Invoke(n);
				if (ms.Length > max) throw new InvalidDataException("bigger than the list says");
			}
			return ms.ToArray();
		}
	}

	/// <summary>How much downloading these maps takes (bytes; 0 if they're here, or the list can't be had).</summary>
	public static async Task<long> DownloadSize(IEnumerable<MapPreset> presets)
	{
		var need = presets.Where(p => !IsDownloaded(p.Id)).ToList();
		if (need.Count == 0) return 0;
		_list ??= Task.Run(FetchList);
		var list = await _list;
		return list == null ? 0 : need.Sum(p => list.TryGetValue(PackOf(p), out var pack) ? pack.Size : 0);
	}

	/// <summary>
	/// Makes sure these maps are on this PC and up to date (a pack whose zip changed on GitHub comes again). Downloads in
	/// the background; progress 0–1 comes on the main thread. Offline, maps already here count as fine.
	/// Returns null when all is well, else what went wrong (for the player).
	/// </summary>
	public static async Task<string?> EnsureAsync(IReadOnlyCollection<MapPreset> presets, Action<float>? progress = null)
	{
		if (!Optional || presets.Count == 0) return null;
		void Report(float p) => Callable.From(() => progress?.Invoke(p)).CallDeferred();
		return await Task.Run(async () =>
		{
			_list ??= FetchList();
			var list = await _list;
			if (list == null)
			{
				// (offline, or GitHub away: try again next time)
				_list = null;
				var missing = presets.Where(p => !Installed.ContainsKey(p.Id)).ToList();
				return missing.Count == 0 ? null : Lang.F("Couldn't download the maps ({0}). Check your internet connection and try again.", string.Join(", ", missing.Select(m => m.Name)));
			}
			var todo = new List<(MapPreset Preset, Pack Pack)>();
			foreach (var preset in presets)
			{
				if (!list.TryGetValue(PackOf(preset), out var pack))
				{
					if (Installed.ContainsKey(preset.Id)) continue;
					return Lang.F("The map {0} isn't available for download yet.", preset.Name);
				}
				if (Installed.TryGetValue(preset.Id, out var have) && (have.Sha256 == pack.Sha256 || have.Sha256 == "" && SameFiles(pack)))
				{
					if (have.Sha256 == "") Adopt(preset.Id, pack);
					continue;
				}
				todo.Add((preset, pack));
			}
			if (todo.Count == 0) return null;
			var total = Math.Max(1, todo.Sum(t => t.Pack.Size));
			long done = 0;
			using var http = NewClient(TimeSpan.FromMinutes(20));
			foreach (var (preset, pack) in todo)
			{
				try
				{
					var zip = await Fetch(http, pack.File, n =>
					{
						done += n;
						Report((float)Math.Min(1, (double)done / total));
					}, pack.Size + (1 << 20));
					// (the signed list's SHA-256: nothing else is unpacked)
					if (!Convert.ToHexString(SHA256.HashData(zip)).Equals(pack.Sha256, StringComparison.OrdinalIgnoreCase))
						throw new InvalidDataException("it arrived damaged");
					var files = Unpack(zip, Dir);
					lock (Gate)
					{
						Installed[preset.Id] = (pack.Sha256, files);
						SaveInstalled();
					}
				}
				catch (Exception e)
				{
					GD.Print($"Map {preset.Id} didn't download: {e.Message}");
					Trace.Log("maps", $"{preset.Id} failed: {e.Message}");
					Forget();
					return Lang.F("Couldn't download the map {0}. Check your internet connection and try again.", preset.Name);
				}
			}
			Trace.Log("maps", $"downloaded {string.Join(", ", todo.Select(t => t.Preset.Id))}");
			Forget();
			return null;
		});
	}

	/// <summary>A pack moved from an older install: is every file the one the list has?</summary>
	static bool SameFiles(Pack pack) => pack.Files.Count > 0 && pack.Files.All(f =>
	{
		var path = Path.Combine(Dir, f.Path);
		if (!File.Exists(path) || new FileInfo(path).Length != f.Size) return false;
		using var s = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(s)).Equals(f.Sha256, StringComparison.OrdinalIgnoreCase);
	});

	static void Adopt(string id, Pack pack)
	{
		lock (Gate)
		{
			Installed[id] = (pack.Sha256, pack.Files.Select(f => f.Path).ToList());
			SaveInstalled();
		}
	}

	// ---------- updating from an install that still had them ----------

	/// <summary>
	/// At startup (exported builds): an install from before 0.12.8b has every map in a "maps" folder next to the game.
	/// The maps the saved quizzes use move to user://maps/ (so they keep working offline); the folder goes.
	/// </summary>
	public static void MoveOldMaps()
	{
		if (!OS.HasFeature("template")) return;
		var old = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? "", "maps");
		if (!Directory.Exists(old)) return;
		try
		{
			var used = QuizStore.List().SelectMany(Used).Distinct().ToList();
			foreach (var preset in used)
			{
				if (Installed.ContainsKey(preset.Id)) continue;
				var files = preset.Id == WowAtlas.PresetId
					? Directory.Exists(Path.Combine(old, "wow"))
						? Directory.GetFiles(Path.Combine(old, "wow"), "*.webp", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(old, f).Replace('\\', '/')).ToList()
						: new()
					: new List<string> { Rel(preset.ImagePath) };
				if (files.Count == 0 || !files.All(f => File.Exists(Path.Combine(old, f)))) continue;
				foreach (var f in files)
				{
					var to = Path.Combine(Dir, f);
					Directory.CreateDirectory(Path.GetDirectoryName(to)!);
					File.Copy(Path.Combine(old, f), to, true);
				}
				lock (Gate) Installed[preset.Id] = ("", files);
			}
			SaveInstalled();
			Directory.Delete(old, true);
			Trace.Log("maps", $"moved the install's maps folder away (kept {string.Join(", ", used.Select(p => p.Id))})");
		}
		catch (Exception e)
		{
			GD.Print($"Couldn't move the old maps folder: {e.Message}");
		}
	}
}
