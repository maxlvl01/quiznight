// Quizzes live in the user folder (user://quizzes, one .quiznight.json per quiz).
// Export/import copies those files to and from anywhere, e.g. to share with friends.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Storage;

public static class QuizStore
{
	const string Dir = "user://quizzes";
	public const string Extension = ".quiznight.json";
	public static readonly string[] FileFilters = { "*.json ; Quiz files" };
	/// <summary>Exports: a zip with the quiz file and its sound and video files next to it.</summary>
	public const string ZipExtension = ".quiznight.zip";
	public static readonly string[] ZipFilters = { "*.zip ; Quiz files" };
	/// <summary>For importing: exported quizzes (zip, or the older single .json) and share codes saved as text (Discord sends long messages as message.txt).</summary>
	public static readonly string[] ImportFilters = { "*.zip, *.json, *.txt ; Quiz files and share codes" };

	// Parsing files with big images is slow, so loaded quizzes are kept in memory.
	static readonly Dictionary<string, Quiz> Cache = new();
	// (each file's write time when it was read or saved: a rescan only reads files that are new or changed since)
	static readonly Dictionary<string, DateTime> Stamps = new();
	static bool _scanned;

	/// <summary>Random id (24 hex characters).</summary>
	public static string MakeId() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

	public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	static string FolderPath => ProjectSettings.GlobalizePath(Dir);
	static string PathFor(string id) => Path.Combine(FolderPath, id + Extension);

	/// <summary>Where the files are on disk, for "Open folder".</summary>
	public static string Folder
	{
		get
		{
			Directory.CreateDirectory(FolderPath);
			return FolderPath;
		}
	}

	public static List<Quiz> List()
	{
		if (!_scanned) Rescan();
		return Cache.Values.OrderByDescending(q => q.UpdatedAt).ToList();
	}

	/// <summary>
	/// Reads the quizzes folder again: files that are new or changed since they were read (dropped in by hand, copied over
	/// from another PC) come in, deleted ones go. Before 0.13.5 the folder was read once per run, so those only showed
	/// after a restart. My quizzes does this each time it opens and on its refresh button.
	/// </summary>
	public static void Rescan()
	{
		Directory.CreateDirectory(FolderPath);
		var seen = new HashSet<string>();
		foreach (var file in Directory.GetFiles(FolderPath, "*" + Extension))
		{
			var id = Path.GetFileName(file)[..^Extension.Length];
			seen.Add(id);
			try
			{
				var stamp = File.GetLastWriteTimeUtc(file);
				if (Cache.ContainsKey(id) && Stamps.TryGetValue(id, out var known) && known == stamp) continue;
				var quiz = QuizJson.Parse(File.ReadAllText(file), out _);
				if (quiz != null && quiz.Id == id)
				{
					DateQuestions(quiz);
					Cache[id] = quiz;
					Stamps[id] = stamp;
				}
			}
			catch (IOException e)
			{
				GD.PrintErr($"Couldn't read {file}: {e.Message}");
			}
		}
		foreach (var gone in Cache.Keys.Where(id => !seen.Contains(id)).ToList())
		{
			Cache.Remove(gone);
			Stamps.Remove(gone);
		}
		_scanned = true;
	}

	public static Quiz? Get(string id)
	{
		List();
		return Cache.TryGetValue(id, out var q) ? q : null;
	}

	/// <summary>Questions made before 0.12.0d5 don't know when they were made. They get a made time from where they are
	/// now (the quiz's own made time + their place, in ms: before every question made since), so "Order made" stays
	/// the same after the play order changes (it followed the play order, so "Play in this order" seemed to undo it).</summary>
	public static void DateQuestions(Quiz quiz)
	{
		var start = Math.Max(1, quiz.CreatedAt);
		for (var i = 0; i < quiz.Questions.Count; i++)
			if (quiz.Questions[i].CreatedAt <= 0) quiz.Questions[i].CreatedAt = start + i;
	}

	/// <summary>Saves a quiz. Throws UserError when the disk says no.</summary>
	/// <param name="touch">Whether this is a change (its last-saved time moves on); not for a quiz synced from the cloud as it was.</param>
	public static Quiz Save(Quiz quiz, bool touch = true)
	{
		// (the guided tour's sample quiz only lives in memory: never written to My quizzes, even by Ctrl+S during the tour)
		if (quiz.Id == TourDownload.Id) return quiz;
		DateQuestions(quiz);
		// Drop maps no question uses anymore, so saved and exported quizzes stay small.
		var used = quiz.Questions.Where(q => q.Type == QuestionType.MapPin && q.MapId != null).Select(q => q.MapId).ToHashSet();
		quiz.Maps = quiz.Maps.Where(m => used.Contains(m.Id)).ToList();
		// Built-in maps ship with the game; their pictures don't need to be in the file.
		foreach (var m in quiz.Maps)
			if (MapPresets.Get(m.PresetId) != null) m.Image.DataUrl = "";
		if (touch) quiz.UpdatedAt = NowMs();
		Directory.CreateDirectory(FolderPath);
		var path = PathFor(quiz.Id);
		var tmp = path + ".tmp";
		try
		{
			File.WriteAllText(tmp, QuizJson.Write(quiz));
			File.Move(tmp, path, true);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			throw new UserError(Lang.F("Saving failed: {0}", e.Message));
		}
		Cache[quiz.Id] = quiz;
		Stamps[quiz.Id] = File.GetLastWriteTimeUtc(path);
		return quiz;
	}

	/// <summary>A quiz from the cloud (QuizSync), as it was saved on the other PC. If this PC had deleted it, the deleted copy
	/// goes: it's back in the list.</summary>
	public static Quiz SaveSynced(Quiz quiz)
	{
		Save(quiz, touch: false);
		var deleted = Path.Combine(DeletedPath, quiz.Id + Extension);
		if (File.Exists(deleted)) File.Delete(deleted);
		return quiz;
	}

	/// <summary>Deleted quizzes wait here for <see cref="KeepDeletedDays"/> days (0.14), so a delete can be undone.</summary>
	static string DeletedPath => Path.Combine(FolderPath, "deleted");
	public const int KeepDeletedDays = 30;

	/// <summary>
	/// Deletes a quiz: into the "deleted" folder, from where <see cref="Restore"/> brings it back (before 0.14 the file was
	/// gone for good, and with "Don't ask again" one click lost a quiz). Its saved night stays until it's purged.
	/// </summary>
	public static void Delete(string id)
	{
		Cache.Remove(id);
		Stamps.Remove(id);
		var path = PathFor(id);
		if (!File.Exists(path)) return;
		try
		{
			Directory.CreateDirectory(DeletedPath);
			var to = Path.Combine(DeletedPath, id + Extension);
			File.Move(path, to, true);
			// (the date it was deleted, for the 30 days)
			File.SetLastWriteTimeUtc(to, DateTime.UtcNow);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			throw new UserError(Lang.F("Couldn't delete the quiz: {0}", e.Message));
		}
	}

	/// <summary>The deleted quizzes still kept, newest first: id, title and when.</summary>
	public static List<(string Id, string Title, DateTime DeletedAt)> Deleted()
	{
		var list = new List<(string, string, DateTime)>();
		if (!Directory.Exists(DeletedPath)) return list;
		foreach (var file in Directory.GetFiles(DeletedPath, "*" + Extension))
		{
			var at = File.GetLastWriteTimeUtc(file);
			var id = Path.GetFileName(file)[..^Extension.Length];
			if (at < DateTime.UtcNow.AddDays(-KeepDeletedDays))
			{
				// (older than 30 days: gone for good now, with its saved night)
				try { File.Delete(file); NightSaves.Delete(id); } catch (IOException) { }
				continue;
			}
			try
			{
				if (QuizJson.Parse(File.ReadAllText(file), out _) is { } quiz) list.Add((id, quiz.Title, at.ToLocalTime()));
			}
			catch (IOException) { }
		}
		return list.OrderByDescending(d => d.Item3).ToList();
	}

	/// <summary>Brings a deleted quiz back into My quizzes.</summary>
	public static Quiz? Restore(string id)
	{
		var from = Path.Combine(DeletedPath, id + Extension);
		if (!File.Exists(from) || File.Exists(PathFor(id))) return null;
		File.Move(from, PathFor(id));
		Rescan();
		return Get(id);
	}

	public static Quiz Create(string title = "New quiz")
	{
		var now = NowMs();
		return Save(new Quiz { Id = MakeId(), Title = title, CreatedAt = now, UpdatedAt = now });
	}

	public static string SuggestedFileName(Quiz quiz)
	{
		var clean = new string(quiz.Title.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_').ToArray()).Trim();
		return (clean == "" ? "quiz" : clean) + ZipExtension;
	}

	/// <summary>
	/// Writes a copy of the quiz to any path (backups, sharing) as a zip: quiz.json, and each sound or video as a file of its
	/// own in media/ (named by its question), instead of as text inside the quiz file (a third smaller).
	/// </summary>
	public static void Export(Quiz quiz, string path)
	{
		try
		{
			var copy = QuizJson.Parse(QuizJson.Write(quiz), out _)!;
			using var zip = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
			foreach (var q in copy.Questions)
			{
				if (!q.IsMedia || q.Media is not { DataUrl.Length: > 0 } media || Server.DataUrl.Bytes(media.DataUrl, out var mime) is not { } bytes) continue;
				// (sound and video are already compressed: stored as they are)
				var entry = zip.CreateEntry($"media/{q.Id}{MediaExtension(mime, media.Name)}", System.IO.Compression.CompressionLevel.NoCompression);
				using (var s = entry.Open()) s.Write(bytes);
				media.Mime = mime;
				media.DataUrl = "";
			}
			var json = zip.CreateEntry("quiz.json", System.IO.Compression.CompressionLevel.Optimal);
			using (var w = new StreamWriter(json.Open())) w.Write(QuizJson.Write(copy, true));
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			throw new UserError(Lang.F("Export failed: {0}", e.Message));
		}
	}

	static string MediaExtension(string mime, string name) =>
		Path.GetExtension(name) is { Length: > 1 } ext ? ext.ToLowerInvariant()
		: mime switch { "audio/mpeg" => ".mp3", "audio/wav" or "audio/x-wav" => ".wav", "video/ogg" => ".ogv", _ => ".ogg" };

	/// <summary>An exported zip back: quiz.json, with each media/ file put back into its question.</summary>
	public static Quiz ReadZip(string path)
	{
		try
		{
			using var zip = System.IO.Compression.ZipFile.OpenRead(path);
			var json = zip.GetEntry("quiz.json") ?? throw new UserError(QuizJson.NotAQuiz);
			// (zips can unpack to far more than they are: only as much as a quiz and its files can be)
			string text;
			using (var r = json.Open()) text = System.Text.Encoding.UTF8.GetString(Guard.ReadCapped(r, Guard.MaxQuizJsonBytes));
			var quiz = QuizJson.Parse(text, out var error) ?? throw new UserError(error ?? QuizJson.NotAQuiz);
			foreach (var q in quiz.Questions)
			{
				if (q.Media == null || zip.Entries.FirstOrDefault(e => e.FullName.StartsWith($"media/{q.Id}.")) is not { } file) continue;
				using var s = file.Open();
				q.Media.DataUrl = Server.DataUrl.Make(q.Media.Mime, Guard.ReadCapped(s, Guard.MaxSharedFileBytes));
			}
			return quiz;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
		{
			throw new UserError(Lang.F("Couldn't read that file: {0}", e.Message));
		}
	}

	/// <summary>Imports a quiz file (zip or .json) as a new quiz (a fresh id, so importing twice gives two copies).</summary>
	public static Quiz Import(string path) => SaveImported(ReadImport(path));

	/// <summary>Reads a quiz file (zip, .json or a long share code in a text file) without saving it, so the caller can look
	/// for a double first (FindDouble).</summary>
	public static Quiz ReadImport(string path)
	{
		if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			var fromZip = ReadZip(path);
			if (fromZip.Title.Trim() == "") fromZip.Title = Path.GetFileName(path).Replace(ZipExtension, "");
			return fromZip;
		}
		string text;
		try
		{
			text = File.ReadAllText(path);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			throw new UserError(Lang.F("Couldn't read that file: {0}", e.Message));
		}
		if (ShareCode.LooksLong(text)) return ReadCode(text);
		var quiz = QuizJson.Parse(text, out var error) ?? throw new UserError(error ?? QuizJson.NotAQuiz);
		if (quiz.Title.Trim() == "") quiz.Title = Path.GetFileName(path).Replace(Extension, "");
		return quiz;
	}

	/// <summary>Imports a long share code (ShareCode) as a new quiz.</summary>
	public static Quiz ImportCode(string code) => SaveImported(ReadCode(code));

	/// <summary>Reads a long share code without saving it.</summary>
	public static Quiz ReadCode(string code) =>
		ShareCode.Read(code, out var error) ?? throw new UserError(error ?? QuizJson.NotAQuiz);

	/// <summary>Saves a quiz from elsewhere (a file, a share code) as a new quiz: a fresh id, so importing twice gives two copies.</summary>
	/// <param name="numbered">A double the user chose to import anyway: its title gets the first free " (1)", " (2)" … behind it.</param>
	public static Quiz SaveImported(Quiz quiz, bool numbered = false)
	{
		quiz.Id = MakeId();
		if (quiz.CreatedAt == 0) quiz.CreatedAt = NowMs();
		if (numbered) quiz.Title = NumberedTitle(quiz.Title);
		return Save(quiz);
	}

	/// <summary>The quiz in My quizzes with exactly these questions (the title doesn't count: a numbered copy has another one),
	/// if there is one. Question made-times don't count either, as a copy may have been dated again.</summary>
	public static Quiz? FindDouble(Quiz quiz)
	{
		if (quiz.Questions.Count == 0) return null;
		var print = Fingerprint(quiz);
		return List().FirstOrDefault(q => Fingerprint(q) == print);
	}

	static string Fingerprint(Quiz quiz)
	{
		var text = string.Join("\n", quiz.Questions.Select(q =>
		{
			var o = QuizJson.WriteQuestion(q);
			o.Remove("createdAt");
			return o.ToJsonString();
		}));
		return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
	}

	/// <summary>"Title (1)", or the first of (2), (3) … that no quiz has yet; a title that already ends in a number is counted on from it.</summary>
	static string NumberedTitle(string title)
	{
		var baseTitle = System.Text.RegularExpressions.Regex.Replace(title.Trim(), @" \(\d+\)$", "");
		var taken = List().Select(q => q.Title.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
		for (var n = 1; ; n++)
		{
			var candidate = $"{baseTitle} ({n})";
			if (!taken.Contains(candidate)) return candidate;
		}
	}
}
