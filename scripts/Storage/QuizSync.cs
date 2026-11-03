// Syncing My Quiznights between your own PCs (0.15, the Sync button): each quiz goes up as an export zip (QuizStore.Export,
// sounds and videos included) to the "sync" release of Max's results repo, named by your Steam ID, so a friend's
// unfinished quiz never lands in your list. Release files, not repo files: replacing one keeps no old copy, so videos
// don't pile up in git. Syncing is manual and goes both ways; a quiz changed on both PCs asks which one to keep.
// Deleting stays on that PC: the cloud keeps its copy, and this PC doesn't take a quiz it deleted back unless it was
// changed elsewhere since. (The Steam ID folder keeps quizzes apart, it isn't a lock: the shipped token reads them all.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
using QuizNight.Net;
using QuizNight.Shared;

namespace QuizNight.Storage;

public static class QuizSync
{
	const string Tag = "sync";
	static string Api => $"https://api.github.com/repos/{ResultsUpload.Repo}";

	public static bool Available => ResultsUpload.Token != null;

	/// <summary>Whose quizzes these are: your Steam ID (null without Steam). --sync-check uses a test account.</summary>
	public static string? Account => TestAccount ?? (SteamService.MySteamId is var id and > 0 ? id.ToString() : null);
	internal static string? TestAccount;

	// ---------- what to do ----------

	public enum Kind { Upload, Download, Conflict }
	/// <summary>What the player picked for a conflict: this PC's, the cloud's, both (the cloud's as a copy), or neither.</summary>
	public enum Choice { Mine, Theirs, Both, Skip }

	/// <summary>A quiz as the cloud has it: its release file, and the quiz's last save then.</summary>
	public sealed record Remote(long AssetId, string QuizId, long UpdatedAt, string Title);

	public sealed class Step
	{
		public Kind Kind;
		public string QuizId = "";
		public string Title = "";
		public Remote? Remote;
		/// <summary>Conflicts: what to do (asked before Run).</summary>
		public Choice Choice = Choice.Skip;
	}

	public sealed class Plan
	{
		public string Account = "";
		public long ReleaseId;
		public readonly List<Step> Steps = new();
		/// <summary>The sync record afterwards for the quizzes that were already the same on both sides.</summary>
		public readonly Dictionary<string, long> Same = new();
	}

	/// <summary>
	/// What to do with each quiz, from this PC's quizzes (id → last save), the cloud's, and the record of what each quiz
	/// was at the last sync (id → last save then). Changed on one side only: that side wins. Changed on both: a conflict.
	/// Not here but synced before: deleted on this PC, so it stays away unless the cloud has a newer one.
	/// </summary>
	public static (List<Step> Steps, Dictionary<string, long> Same) Decide(IReadOnlyDictionary<string, (long UpdatedAt, string Title)> local,
		IReadOnlyDictionary<string, Remote> remote, IReadOnlyDictionary<string, long> synced)
	{
		var steps = new List<Step>();
		var same = new Dictionary<string, long>();
		foreach (var (id, mine) in local)
		{
			if (!remote.TryGetValue(id, out var theirs))
			{
				steps.Add(new Step { Kind = Kind.Upload, QuizId = id, Title = mine.Title });
				continue;
			}
			if (mine.UpdatedAt == theirs.UpdatedAt)
			{
				same[id] = mine.UpdatedAt;
				continue;
			}
			var known = synced.TryGetValue(id, out var at) ? at : (long?)null;
			var kind = known == theirs.UpdatedAt ? Kind.Upload : known == mine.UpdatedAt ? Kind.Download : Kind.Conflict;
			steps.Add(new Step { Kind = kind, QuizId = id, Title = mine.Title, Remote = theirs });
		}
		foreach (var (id, theirs) in remote)
		{
			if (local.ContainsKey(id)) continue;
			// (deleted here since the last sync, and nobody changed it since: it stays deleted on this PC)
			if (synced.TryGetValue(id, out var at) && at == theirs.UpdatedAt) continue;
			steps.Add(new Step { Kind = Kind.Download, QuizId = id, Title = theirs.Title, Remote = theirs });
		}
		return (steps, same);
	}

	/// <summary>Looks at both sides. Throws UserError with what went wrong.</summary>
	public static async Task<Plan> MakePlan(string account)
	{
		var local = QuizStore.List().ToDictionary(q => q.Id, q => (q.UpdatedAt, q.Title));
		var synced = Record(account);
		var (releaseId, remote) = await Net(() => Task.Run(async () =>
		{
			using var http = Client();
			var release = await ReleaseId(http);
			var remote = new Dictionary<string, Remote>();
			foreach (var asset in await Assets(http, release))
			{
				var name = asset["name"]?.GetValue<string>() ?? "";
				var assetId = asset["id"]?.GetValue<long>() ?? 0;
				if (!name.StartsWith(account + "-")) continue;
				// (an upload that never got its name: from a sync that was cut off)
				if (name.Contains(".part")) (await http.DeleteAsync($"{Api}/releases/assets/{assetId}")).Dispose();
				else if (name.EndsWith(".zip")) remote[name[(account.Length + 1)..^4]] = ParseRemote(assetId, name[(account.Length + 1)..^4], asset["label"]?.GetValue<string>());
			}
			return (release, remote);
		}));
		var plan = new Plan { Account = account, ReleaseId = releaseId };
		var (steps, same) = Decide(local, remote, synced);
		plan.Steps.AddRange(steps);
		foreach (var (id, at) in same) plan.Same[id] = at;
		return plan;
	}

	/// <summary>The label carries the quiz's last save and its title: "1793660571189 Pub quiz #3".</summary>
	static Remote ParseRemote(long assetId, string quizId, string? label)
	{
		var text = label ?? "";
		var space = text.IndexOf(' ');
		var at = long.TryParse(space < 0 ? text : text[..space], out var ms) ? ms : 0;
		return new Remote(assetId, quizId, at, space < 0 ? quizId : text[(space + 1)..]);
	}

	static string Label(Quiz quiz) => $"{quiz.UpdatedAt} {(quiz.Title.Length > 100 ? quiz.Title[..100] : quiz.Title)}";

	public sealed class Report
	{
		public readonly List<string> Uploaded = new(), Downloaded = new(), Failed = new();
	}

	/// <summary>Does the plan, one quiz after another; progress hears what's happening. Doesn't throw: what fails is in the report.</summary>
	public static async Task<Report> Run(Plan plan, Action<string> progress)
	{
		var report = new Report();
		var record = Record(plan.Account);
		foreach (var (id, at) in plan.Same) record[id] = at;
		var temp = ProjectSettings.GlobalizePath("user://sync-temp");
		Directory.CreateDirectory(temp);
		var n = 0;
		foreach (var step in plan.Steps)
		{
			n++;
			var choice = step.Kind switch { Kind.Upload => Choice.Mine, Kind.Download => Choice.Theirs, _ => step.Choice };
			if (choice == Choice.Skip) continue;
			try
			{
				if (choice is Choice.Theirs or Choice.Both)
				{
					progress(Lang.F("Downloading {0} of {1}: {2}", n, plan.Steps.Count, step.Remote!.Title));
					var quiz = await Download(step.Remote!, Path.Combine(temp, step.QuizId + ".zip"));
					if (choice == Choice.Theirs)
					{
						QuizStore.SaveSynced(quiz);
						record[step.QuizId] = quiz.UpdatedAt;
					}
					else
					{
						// (keep both: the cloud's comes in as a copy of its own, and goes up as one)
						quiz.Id = QuizStore.MakeId();
						quiz.Title = Lang.F("{0} (other PC)", quiz.Title);
						QuizStore.Save(quiz);
						await Upload(plan, quiz, null, Path.Combine(temp, quiz.Id + ".zip"));
						record[quiz.Id] = quiz.UpdatedAt;
					}
					report.Downloaded.Add(quiz.Title);
				}
				if (choice is Choice.Mine or Choice.Both && QuizStore.Get(step.QuizId) is { } mine)
				{
					progress(Lang.F("Uploading {0} of {1}: {2}", n, plan.Steps.Count, mine.Title));
					await Upload(plan, mine, step.Remote, Path.Combine(temp, step.QuizId + ".zip"));
					record[step.QuizId] = mine.UpdatedAt;
					report.Uploaded.Add(mine.Title);
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Sync: {step.Title}: {e.Message}");
				report.Failed.Add(step.Title);
			}
		}
		SaveRecord(plan.Account, record);
		try { Directory.Delete(temp, true); } catch (IOException) { }
		return report;
	}

	/// <summary>Up under a name of its own first, then the old one goes and the new one takes its name: a sync that's cut
	/// off never leaves the cloud without the quiz.</summary>
	static async Task Upload(Plan plan, Quiz quiz, Remote? old, string zipPath)
	{
		var name = $"{plan.Account}-{quiz.Id}.zip";
		var label = Label(quiz);
		await Net(() => Task.Run(async () =>
		{
			QuizStore.Export(quiz, zipPath);
			var bytes = await File.ReadAllBytesAsync(zipPath);
			File.Delete(zipPath);
			using var http = Client();
			var part = $"{plan.Account}-{quiz.Id}.{Guid.NewGuid().ToString("N")[..6]}.part.zip";
			var content = new ByteArrayContent(bytes);
			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
			using var sent = await http.PostAsync($"https://uploads.github.com/repos/{ResultsUpload.Repo}/releases/{plan.ReleaseId}/assets?name={Uri.EscapeDataString(part)}", content);
			await Ok(sent);
			var newId = JsonNode.Parse(await sent.Content.ReadAsStringAsync())?["id"]?.GetValue<long>() ?? throw new HttpRequestException("No asset id");
			if (old != null)
			{
				using var gone = await http.DeleteAsync($"{Api}/releases/assets/{old.AssetId}");
				if (gone.StatusCode != HttpStatusCode.NotFound) await Ok(gone);
			}
			var body = new JsonObject { ["name"] = name, ["label"] = label }.ToJsonString();
			using var renamed = await http.PatchAsync($"{Api}/releases/assets/{newId}", new StringContent(body, Encoding.UTF8, "application/json"));
			await Ok(renamed);
		}));
	}

	static Task<Quiz> Download(Remote remote, string zipPath) => Net(() => Task.Run(async () =>
	{
		using var http = Client("application/octet-stream");
		// (GitHub answers with a link to the file's storage; the client follows it and leaves the token behind)
		using var response = await http.GetAsync($"{Api}/releases/assets/{remote.AssetId}");
		await Ok(response);
		await using (var file = File.Create(zipPath)) await response.Content.CopyToAsync(file);
		var quiz = QuizStore.ReadZip(zipPath);
		File.Delete(zipPath);
		quiz.Id = remote.QuizId;
		return quiz;
	}));

	/// <summary>--sync-check: the quiz up under a test account, back down, compared, and gone again (My Quiznights isn't
	/// touched). True if it came back the same.</summary>
	internal static async Task<bool> Check(Quiz quiz)
	{
		var plan = new Plan { Account = "selftest" };
		var temp = ProjectSettings.GlobalizePath("user://sync-temp");
		Directory.CreateDirectory(temp);
		plan.ReleaseId = await Net(() => Task.Run(async () =>
		{
			using var http = Client();
			return await ReleaseId(http);
		}));
		await Upload(plan, quiz, null, Path.Combine(temp, "up.zip"));
		var name = $"{plan.Account}-{quiz.Id}.zip";
		var asset = await Net(() => Task.Run(async () =>
		{
			using var http = Client();
			return (await Assets(http, plan.ReleaseId)).First(a => a["name"]?.GetValue<string>() == name);
		}));
		var remote = ParseRemote(asset["id"]!.GetValue<long>(), quiz.Id, asset["label"]?.GetValue<string>());
		var back = await Download(remote, Path.Combine(temp, "down.zip"));
		await Net(() => Task.Run(async () =>
		{
			using var http = Client();
			await Ok(await http.DeleteAsync($"{Api}/releases/assets/{remote.AssetId}"));
		}));
		Directory.Delete(temp, true);
		return remote.UpdatedAt == quiz.UpdatedAt && QuizJson.Write(back) == QuizJson.Write(quiz);
	}

	// ---------- GitHub ----------

	static HttpClient Client(string accept = "application/vnd.github+json")
	{
		var http = ResultsUpload.Client(ResultsUpload.Token ?? throw new UserError(Lang.T("This game can't sync: it has no key for the cloud.")), accept);
		// (a quiz with a few videos is hundreds of MB)
		http.Timeout = TimeSpan.FromMinutes(30);
		return http;
	}

	static async Task Ok(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode) throw new HttpRequestException($"GitHub said {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
	}

	/// <summary>The "sync" release's id; made the first time anyone syncs.</summary>
	static async Task<long> ReleaseId(HttpClient http)
	{
		for (var attempt = 0; attempt < 2; attempt++)
		{
			using (var found = await http.GetAsync($"{Api}/releases/tags/{Tag}"))
			{
				if (found.IsSuccessStatusCode) return JsonNode.Parse(await found.Content.ReadAsStringAsync())!["id"]!.GetValue<long>();
				if (found.StatusCode != HttpStatusCode.NotFound) await Ok(found);
			}
			var body = new JsonObject
			{
				["tag_name"] = Tag,
				["name"] = "Quiz sync",
				["body"] = "Quizzes synced between players' own PCs (My Quiznights > Sync), one file per quiz named <Steam ID>-<quiz id>.zip. Don't delete.",
				["prerelease"] = true,
			}.ToJsonString();
			using var made = await http.PostAsync($"{Api}/releases", new StringContent(body, Encoding.UTF8, "application/json"));
			if (made.IsSuccessStatusCode) return JsonNode.Parse(await made.Content.ReadAsStringAsync())!["id"]!.GetValue<long>();
			// (422: someone else made it at the same moment; look again)
			if (made.StatusCode != HttpStatusCode.UnprocessableEntity) await Ok(made);
		}
		throw new HttpRequestException("No sync release");
	}

	static async Task<List<JsonNode>> Assets(HttpClient http, long release)
	{
		var all = new List<JsonNode>();
		for (var page = 1; ; page++)
		{
			using var response = await http.GetAsync($"{Api}/releases/{release}/assets?per_page=100&page={page}");
			await Ok(response);
			var list = JsonNode.Parse(await response.Content.ReadAsStringAsync()) as JsonArray ?? new JsonArray();
			all.AddRange(list.Where(a => a != null)!);
			if (list.Count < 100) return all;
		}
	}

	/// <summary>Network trouble as a message for the player.</summary>
	static async Task<T> Net<T>(Func<Task<T>> work)
	{
		try
		{
			return await work();
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
		{
			GD.PrintErr($"Sync: {e.Message}");
			throw new UserError(Lang.T("Couldn't reach the cloud. Check your internet connection and try again."));
		}
	}

	static async Task Net(Func<Task> work) => await Net(async () =>
	{
		await work();
		return true;
	});

	// ---------- what each quiz was at the last sync (user://sync.json: account → quiz id → last save) ----------

	static string RecordPath => ProjectSettings.GlobalizePath("user://sync.json");

	static Dictionary<string, long> Record(string account)
	{
		try
		{
			if (File.Exists(RecordPath) && JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, long>>>(File.ReadAllText(RecordPath)) is { } all
				&& all.TryGetValue(account, out var mine))
				return mine;
		}
		catch (Exception e) when (e is IOException or JsonException) { }
		return new();
	}

	static void SaveRecord(string account, Dictionary<string, long> record)
	{
		try
		{
			var all = File.Exists(RecordPath) ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, long>>>(File.ReadAllText(RecordPath)) ?? new() : new();
			all[account] = record;
			File.WriteAllText(RecordPath, JsonSerializer.Serialize(all));
		}
		catch (Exception e) when (e is IOException or JsonException)
		{
			GD.PrintErr($"Couldn't save the sync record: {e.Message}");
		}
	}
}
