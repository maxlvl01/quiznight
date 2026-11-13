// Every podium this PC saw (Shared.FinalResults), one file each in user://podiums, so the match history can show it
// again with its credits: host or player, Steam or LAN. Pictures of it go to Documents/Quiznight/Pictures.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Storage;

public static class Podiums
{
	const string Dir = "user://podiums";

	static string Folder => ProjectSettings.GlobalizePath(Dir);

	/// <summary>Keeps it (once: the file is named by the night's end and the quiz).</summary>
	public static void Save(FinalResults results)
	{
		try
		{
			Directory.CreateDirectory(Folder);
			var safe = new string(results.QuizTitle.Select(c => char.IsLetterOrDigit(c) ? c : '-').Take(40).ToArray()).Trim('-');
			QuizStore.WriteSafely(System.IO.Path.Combine(Folder, $"{results.Date:yyyyMMdd-HHmmss}-{(safe == "" ? "quiz" : safe)}.json"), results.ToJson());
		}
		catch (Exception e)
		{
			GD.PushWarning($"Couldn't keep the podium: {e.Message}");
		}
	}

	/// <summary>Dev runs (--screen history --pretend-podium): a made-up podium in the list, never written.</summary>
	public static FinalResults? Pretend;

	/// <summary>Newest first.</summary>
	public static List<FinalResults> All()
	{
		try
		{
			if (!Directory.Exists(Folder)) return Pretend != null ? new() { Pretend } : new();
			return Directory.GetFiles(Folder, "*.json")
				.Select(f => { try { return FinalResults.FromJson(File.ReadAllText(f)); } catch (Exception) { return null; } })
				.OfType<FinalResults>().Concat(Pretend != null ? new[] { Pretend } : Array.Empty<FinalResults>()).OrderByDescending(r => r.Date).ToList();
		}
		catch (Exception)
		{
			return new();
		}
	}
}
