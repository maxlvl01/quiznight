// Bug reports, sent by hand (the 🐞 button, Ui/BugReport), from the same template every time. Since 0.12.9 a bug report is
// a GitHub Issue (since 0.15.2 on the public repo maxlvl01/quiznight with issues-token.txt, no screenshot in it; the private
// results repo's issue with the screenshot is the fallback, then a file) in Max's private results repo (title "<category> (<version>)", label "bug", the log folded into a
// <details> block: IssueTitle/IssueBody); its screenshot is still a file, bugs/Reported/<date> <version> <category> <id>.png,
// shown in the issue. Max adds wip/pending labels and closes the issue once the fix is out. If the issue can't be made
// (the token lacks "Issues: read and write" → 403/404), the report goes as a Markdown file next to the screenshot as before
// (bugs/Reported, .md); old files in bugs/ (WIP, Pending, Resolved) stay where they are.
// Feedback (ideas: maps, question types, quality of life …) stays a file in features/Requested/ (<kind> in the name),
// without the log; Max moves those on by hand (WIP, Pending, Resolved). If sending fails (no internet), the report waits in
// user://bug-reports and goes on the next start (a bug trying the issue first again).
// Every report says who sent it (Steam name, no ID, so ideas can be credited in the changelog), and once it's in a push
// goes to Max's phone and PC through ntfy.sh (Push), opening the issue (or the file) when tapped.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;

namespace QuizNight.Storage;

public static class BugReports
{
	const string Waiting = "user://bug-reports";
	/// <summary>Where new reports land in the repo: bugs, and feedback (feature requests).</summary>
	public const string Reported = "bugs/Reported", Requested = "features/Requested";
	const int LogLines = 200;
	/// <summary>The ntfy.sh topic Max's phone and PC listen to (https://ntfy.sh/quiznight-k8x2m9q4v7). Anyone who knows it can
	/// read and send pushes, so it's kept only here.</summary>
	const string NtfyTopic = "quiznight-k8x2m9q4v7";
	/// <summary>How much of the player's text the push shows (the whole report is a tap away).</summary>
	const int PushTextMax = 600;
	/// <summary>GitHub takes at most 65 536 characters in an issue body; the log is cut from its start to stay under this.</summary>
	const int IssueBodyMax = 60_000;

	const string PublicRepo = "maxlvl01/quiznight", IssuesTokenFile = "res://issues-token.txt";

	/// <summary>The token that can only open issues on the public repo (issues-token.txt, shipped like the results token).</summary>
	static string? IssuesToken
	{
		get
		{
			if (!Godot.FileAccess.FileExists(IssuesTokenFile)) return null;
			var token = Godot.FileAccess.GetFileAsString(IssuesTokenFile).Trim();
			return token == "" ? null : token;
		}
	}

	/// <summary>Reports can be sent (the game has the results token).</summary>
	public static bool Enabled => ResultsUpload.Token != null;

	/// <summary>The end of this run's log (Godot's user://logs/godot.log, open while the game runs), user name left out.</summary>
	public static string LogTail()
	{
		try
		{
			var path = ProjectSettings.GlobalizePath("user://logs/godot.log");
			if (!File.Exists(path)) return "(no log file)";
			using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
			using var reader = new StreamReader(stream);
			var lines = reader.ReadToEnd().Split('\n');
			return NoSteamIds(CrashReports.Anonymise(string.Join('\n', lines.Skip(Math.Max(0, lines.Length - LogLines))), System.Environment.UserName));
		}
		catch (Exception e)
		{
			return $"(couldn't read the log: {e.Message})";
		}
	}

	/// <summary>Takes Steam IDs out of text (the join messages in the log name them): "steam:7656...", "steam-7656...", "Steam ID 7656..."
	/// or a bare 17-digit ID all become "&lt;steam id&gt;", so a report can be read by anyone.</summary>
	public static string NoSteamIds(string text) =>
		System.Text.RegularExpressions.Regex.Replace(text, @"(?i)(steam[:\- ]|steam id )?\b7656119\d{10}\b", "<steam id>");

	/// <summary>Who sent it: the Steam name only (to credit ideas), no ID, so reports could be public.</summary>
	public static string Sender()
	{
		var name = (Net.SteamService.MySteamId != 0 ? Net.SteamService.PlayerName : Settings.PlayerName).Trim();
		return name == "" ? "(no name)" : name;
	}

	/// <summary>The report from the template: what the game knows, then the player's text, then the log.</summary>
	public static string Make(string category, string text, string details) => new StringBuilder()
		.AppendLine($"# Bug: {category}")
		.AppendLine()
		.AppendLine($"- **Sent by:** {Sender()}")
		.AppendLine($"- **Version:** {App.Version}{(Updater.TestChannel ? " (test build)" : OS.HasFeature("template") ? "" : " (editor)")}")
		.AppendLine($"- **Date:** {DateTime.Now:yyyy-MM-dd HH:mm} ({DateTime.UtcNow:HH:mm} UTC)")
		.AppendLine($"- **System:** {OS.GetName()} {OS.GetVersion()} · window {DisplayServer.WindowGetSize().X}×{DisplayServer.WindowGetSize().Y}")
		.AppendLine($"- **Skin:** {Settings.Skin}")
		.Append(details)
		.AppendLine()
		.AppendLine("## What happened")
		.AppendLine()
		.AppendLine(text.Trim())
		.AppendLine()
		.AppendLine($"## Log (last {LogLines} lines)")
		.AppendLine()
		.AppendLine("```")
		.AppendLine(LogTail())
		.AppendLine("```")
		.ToString();

	/// <summary>Feedback from the template: what it's about, where it was sent from, then the idea (no log: it's not a bug).</summary>
	public static string MakeFeedback(string kind, string where, string text) => new StringBuilder()
		.AppendLine($"# Feedback: {kind}")
		.AppendLine()
		.AppendLine($"- **Sent by:** {Sender()}")
		.AppendLine($"- **Version:** {App.Version}{(Updater.TestChannel ? " (test build)" : OS.HasFeature("template") ? "" : " (editor)")}")
		.AppendLine($"- **Date:** {DateTime.Now:yyyy-MM-dd HH:mm} ({DateTime.UtcNow:HH:mm} UTC)")
		.AppendLine($"- **Skin:** {Settings.Skin}")
		.AppendLine($"- **Sent from:** {where}")
		.AppendLine()
		.AppendLine("## Idea")
		.AppendLine()
		.AppendLine(text.Trim())
		.ToString();

	/// <summary>Sends a report (and its screenshot) to root/ (Reported or Requested), the category in the file name; if that
	/// fails it waits for the next start. Calls back with whether it went.</summary>
	public static void Send(string category, string report, byte[]? png, Action<bool> done, string root = Reported)
	{
		var label = string.Concat(category.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' ? c : '-')).Replace("  ", " ").Trim(' ', '-');
		var name = $"{root}/{DateTime.Now:yyyy-MM-dd HH-mm} {App.Version} {label} {Guid.NewGuid().ToString("N")[..6]}";
		_ = Task.Run(async () =>
		{
			var ok = true;
			try
			{
				await Push(await Deliver(name, report, png), report, false);
			}
			catch (Exception e)
			{
				GD.PrintErr($"Couldn't send the bug report: {e.Message}");
				ok = false;
				Keep(name, report, png);
			}
			Callable.From(() => done(ok)).CallDeferred();
		});
	}

	/// <summary>At the start: reports that couldn't go last time go now.</summary>
	public static void SendWaiting()
	{
		if (!Enabled || !DirAccess.DirExistsAbsolute(Waiting)) return;
		var dir = ProjectSettings.GlobalizePath(Waiting);
		_ = Task.Run(async () =>
		{
			foreach (var md in Directory.GetFiles(dir, "*.md"))
			{
				try
				{
					// (the repo path is in the file name, "/" as "~")
					var name = Moved(Path.GetFileNameWithoutExtension(md).Replace('~', '/'));
					var report = await File.ReadAllTextAsync(md);
					var png = Path.ChangeExtension(md, ".png");
					var url = await Deliver(name, report, File.Exists(png) ? await File.ReadAllBytesAsync(png) : null);
					File.Delete(md);
					if (File.Exists(png)) File.Delete(png);
					await Push(url, report, true);
				}
				catch (Exception e)
				{
					GD.PrintErr($"Couldn't send a waiting bug report: {e.Message}");
					return;
				}
			}
		});
	}

	/// <summary>
	/// Puts a report into the repo and says where to open it: the screenshot first (a file, also for bugs), then a bug as an
	/// issue, or as name.md if that fails (no Issues permission, …) or it's feedback. Throws if nothing went (offline): the
	/// caller keeps it. A screenshot already there (sent before the rest failed) counts as sent.
	/// </summary>
	static async Task<string> Deliver(string name, string report, byte[]? png)
	{
		if (png != null) await Put(name + ".png", png, alreadyThereOk: true);
		if (name.StartsWith(Reported + "/"))
		{
			try
			{
				// (public first: its text has no screenshot, that stays in the private repo; the private issue shows it)
				if (IssuesToken is { } issues)
				{
					try
					{
						var shot = png != null ? "\n\n_A screenshot was sent too; Max has it._" : "";
						return await CreateIssue(PublicRepo, issues, IssueTitle(report), IssueBody(report, null) + shot);
					}
					catch (Exception e)
					{
						GD.PrintErr($"Couldn't open a public issue for the bug report, trying the private repo: {e.Message}");
					}
				}
				return await CreateIssue(ResultsUpload.Repo, ResultsUpload.Token!, IssueTitle(report), IssueBody(report, png != null ? name + ".png" : null));
			}
			catch (Exception e)
			{
				GD.PrintErr($"Couldn't open an issue for the bug report, sending it as a file: {e.Message}");
			}
		}
		await Put(name + ".md", Encoding.UTF8.GetBytes(report));
		return FileUrl(name + ".md");
	}

	static string FileUrl(string file) =>
		$"https://github.com/{ResultsUpload.Repo}/blob/HEAD/{string.Join('/', file.Split('/').Select(Uri.EscapeDataString))}";

	/// <summary>The issue's title, read from the report: "&lt;category&gt; (&lt;version&gt;)" (the version without "(test build)").</summary>
	public static string IssueTitle(string report)
	{
		var lines = report.Replace("\r", "").Split('\n');
		var category = (lines.FirstOrDefault() ?? "").TrimStart('#', ' ');
		if (category.StartsWith("Bug:")) category = category[4..].Trim();
		var version = lines.FirstOrDefault(l => l.StartsWith("- **Version:** "))?["- **Version:** ".Length..].Split(' ')[0] ?? "";
		return version == "" ? category : $"{category} ({version})";
	}

	/// <summary>
	/// The issue's text: the report without its heading (that's the title), the screenshot (a file in the repo) shown under
	/// the player's text, and the log folded away in a &lt;details&gt; block, cut from its start if the body would be too long.
	/// </summary>
	public static string IssueBody(string report, string? screenshot)
	{
		var lines = report.Replace("\r", "").Split('\n').ToList();
		if (lines.Count > 0 && lines[0].StartsWith("# ")) lines.RemoveAt(0);
		var logAt = lines.FindIndex(l => l.StartsWith("## Log"));
		var head = string.Join('\n', logAt < 0 ? lines : lines.Take(logAt)).Trim('\n');
		var log = logAt < 0 ? "" : string.Join('\n', lines.Skip(logAt + 1)).Trim('\n');
		if (log.StartsWith("```")) log = log[3..].TrimStart('\n');
		if (log.EndsWith("```")) log = log[..^3].TrimEnd('\n');
		var body = new StringBuilder(head).Append("\n\n");
		if (screenshot != null)
		{
			var url = FileUrl(screenshot);
			body.Append($"## Screenshot\n\n[![Screenshot]({url}?raw=true)]({url})\n\n");
		}
		if (logAt >= 0)
		{
			var summary = lines[logAt].TrimStart('#', ' ');
			var room = Math.Max(0, IssueBodyMax - body.Length - summary.Length - 60);
			if (log.Length > room) log = "…" + log[^room..];
			// (a longer fence, in case the log holds ``` itself)
			body.Append($"<details><summary>{summary}</summary>\n\n````\n{log}\n````\n\n</details>\n");
		}
		return body.ToString();
	}

	/// <summary>Opens the issue (label "bug", which GitHub makes if it's missing) and returns its page.</summary>
	static async Task<string> CreateIssue(string repo, string token, string title, string body)
	{
		using var http = ResultsUpload.Client(token);
		var json = new JsonObject { ["title"] = title, ["body"] = body, ["labels"] = new JsonArray("bug") }.ToJsonString();
		using var response = await http.PostAsync($"https://api.github.com/repos/{repo}/issues", new StringContent(json, Encoding.UTF8, "application/json"));
		if (!response.IsSuccessStatusCode) throw new HttpRequestException($"GitHub said {(int)response.StatusCode}");
		return JsonNode.Parse(await response.Content.ReadAsStringAsync())?["html_url"]?.GetValue<string>()
			?? throw new HttpRequestException("GitHub's answer had no html_url");
	}

	/// <summary>A report kept by 0.12.3b or older (bug-reports/&lt;category&gt;/… or feedback/&lt;kind&gt;/…) goes where new ones
	/// go, the category moved into the name before the id.</summary>
	static string Moved(string name)
	{
		var parts = name.Split('/');
		if (parts.Length != 3 || parts[0] is not ("bug-reports" or "feedback")) return name;
		var id = parts[2].LastIndexOf(' ');
		if (id < 0) return name;
		return $"{(parts[0] == "feedback" ? Requested : Reported)}/{parts[2][..id]} {parts[1]}{parts[2][id..]}";
	}

	static void Keep(string name, string report, byte[]? png)
	{
		try
		{
			var dir = ProjectSettings.GlobalizePath(Waiting);
			Directory.CreateDirectory(dir);
			var file = Path.Combine(dir, name.Replace('/', '~'));
			File.WriteAllText(file + ".md", report);
			if (png != null) File.WriteAllBytes(file + ".png", png);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Couldn't keep the bug report: {e.Message}");
		}
	}

	/// <summary>
	/// The push to Max's phone and PC (ntfy.sh), read from the report itself so late ones look the same: "Bug" or "Feature
	/// request" and where, who sent it, the start of the text; tapping it opens the issue (or the report's file). A push
	/// that fails is only logged: the report is already in.
	/// </summary>
	static async Task Push(string click, string report, bool late)
	{
		try
		{
			var lines = report.Replace("\r", "").Split('\n');
			var head = lines.FirstOrDefault()?.TrimStart('#', ' ') ?? "";
			var bug = head.StartsWith("Bug:");
			var title = (bug ? "Bug: " + head[4..].Trim() : "Feature request: " + head.Replace("Feedback:", "").Trim()) + (late ? " (sent late)" : "");
			string Field(string key) => lines.FirstOrDefault(l => l.StartsWith($"- **{key}:** "))?[($"- **{key}:** ").Length..] ?? "";
			// (the player's text: the section after "What happened" / "Idea", up to the next one)
			var text = string.Join('\n', lines.SkipWhile(l => l is not ("## What happened" or "## Idea")).Skip(1).TakeWhile(l => !l.StartsWith("## "))).Trim();
			if (text.Length > PushTextMax) text = text[..PushTextMax] + " …";
			var message = $"From {Field("Sent by")}\n{Field("Version")}{(bug ? "" : " · " + Field("Sent from"))}\n\n{text}";
			var body = new JsonObject
			{
				["topic"] = NtfyTopic,
				["title"] = title,
				["message"] = message,
				["tags"] = new JsonArray(bug ? "lady_beetle" : "bulb"),
				["priority"] = bug ? 4 : 3,
				["click"] = click,
			}.ToJsonString();
			using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
			using var response = await http.PostAsync("https://ntfy.sh/", new StringContent(body, Encoding.UTF8, "application/json"));
			if (!response.IsSuccessStatusCode) GD.PrintErr($"Couldn't send the push for the report: ntfy said {(int)response.StatusCode}");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Couldn't send the push for the report: {e.Message}");
		}
	}

	static async Task Put(string file, byte[] content, bool alreadyThereOk = false)
	{
		using var http = ResultsUpload.Client(ResultsUpload.Token!);
		var body = new JsonObject { ["message"] = $"Bug report: {file}", ["content"] = Convert.ToBase64String(content) }.ToJsonString();
		var url = $"https://api.github.com/repos/{ResultsUpload.Repo}/contents/{string.Join('/', file.Split('/').Select(Uri.EscapeDataString))}";
		using var response = await http.PutAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
		// (422: the file is there already, GitHub wants its sha to replace it)
		if (alreadyThereOk && (int)response.StatusCode == 422) return;
		if (!response.IsSuccessStatusCode) throw new HttpRequestException($"GitHub said {(int)response.StatusCode}");
	}
}
