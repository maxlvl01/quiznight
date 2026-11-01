// A quiz night's award pack: the season award pack's pictures (SeasonPoster) for one night, made in the background while
// the podium plays. A 1920×1080 poster of the night's standings and a square card per award, zipped into
// Documents/Quiznight/Pictures under the night's date and quiz. The podium and the match history's "Your podiums" open it.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Ui.Screens;

public static class NightPack
{
	/// <summary>Packs being drawn right now (by their ZIP's path).</summary>
	static readonly HashSet<string> Drawing = new();

	static string Safe(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '-').Take(40).ToArray()).Trim('-') is { Length: > 0 } t ? t : "quiz";

	public static string PathOf(FinalResults r) => System.IO.Path.Combine(SeasonPoster.Folder, $"{r.Date:yyyy-MM-dd_HH-mm} {Safe(r.QuizTitle)} night awards.zip");

	public static bool Exists(FinalResults r) => File.Exists(PathOf(r));

	public static bool Making(FinalResults r) => Drawing.Contains(PathOf(r));

	/// <summary>Draws and saves the pack (once at a time per night). Not in headless runs, which can't draw.</summary>
	public static async void Make(FinalResults r)
	{
		if (DisplayServer.GetName() == "headless") return;
		var path = PathOf(r);
		if (!Drawing.Add(path)) return;
		try
		{
			// (drawn on the app, not the podium: it keeps going when the podium closes)
			await SeasonPoster.Save(App.I, Pack(r), zipPath: path, show: false);
		}
		finally
		{
			Drawing.Remove(path);
		}
	}

	/// <summary>Shows the pack in the file manager.</summary>
	public static void Open(FinalResults r)
	{
		if (Exists(r)) OS.ShellShowInFileManager(PathOf(r));
	}

	public static SeasonPoster.Pack Pack(FinalResults r)
	{
		var date = r.Date.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
		var standings = r.Players.Select(p => new SeasonPoster.Standing(p.Id, p.Name, p.Score, 0, 1, p.Place)).ToList();
		string Name(string id) => r.Player(id)?.Name ?? "?";
		var awards = new List<SeasonPoster.Award>();
		foreach (var a in r.Awards)
		{
			if (NightAwards.Of(a.Kind) is not { } info || a.Players.Count == 0) continue;
			// (winners who share it are one name, "Ann & Bob", with the first one's picture)
			var top = new List<(string Id, string Name, string Value)> { (a.Players[0], string.Join(" & ", a.Players.Select(Name)), info.Format(a.Value)) };
			top.AddRange(a.Runners.Select(x => (x.Id, Name(x.Id), info.Format(x.Value))));
			awards.Add(new SeasonPoster.Award(a.Kind, Lang.T(info.Title), Lang.F(info.Line, info.Format(a.Value)), top));
		}
		var ranking = r.Host != "" ? Lang.F("{0} · hosted by {1}", date, r.Host) : date;
		return new SeasonPoster.Pack(r.QuizTitle, ranking, "points", standings,
			Lang.F("{0} players · {1} questions", r.Players.Count, r.Questions), awards, Night: true);
	}
}
