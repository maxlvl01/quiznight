// The 1.0 dev builds (1.0.0d …) keep their data apart from the beta's, so a 1.0 build can change file formats without
// breaking the beta installed beside it. On the first start, this copies what the player made over from the beta's folder.

using System;
using System.IO;
using Godot;

namespace QuizNight.Storage;

public static class BetaDataCopy
{
	/// <summary>The beta's data folder name (project.godot's custom_user_dir_name before 1.0.0d3).</summary>
	const string BetaFolder = "Quiznight";

	const string MarkerName = "copied-from-beta.txt";

	/// <summary>What is copied: quizzes, saved nights, podiums, settings, the cloud sync record and the player's own pictures
	/// and buzzer sound. Not the caches, logs, update leftovers or map packs (those are downloaded again when needed).</summary>
	static readonly string[] Folders = { "quizzes", "saves", "podiums", "backgrounds" };
	static readonly string[] Files = { "settings.cfg", "sync.json" };

	/// <summary>Once per data folder, before anything reads the settings; does nothing in the beta's own folder.</summary>
	public static void Run()
	{
		try
		{
			if (DisplayServer.GetName() == "headless") return;
			var mine = new DirectoryInfo(OS.GetUserDataDir());
			if (mine.Parent is not { } appData || mine.Name == BetaFolder) return;
			var marker = Path.Combine(mine.FullName, MarkerName);
			if (File.Exists(marker)) return;
			var beta = new DirectoryInfo(Path.Combine(appData.FullName, BetaFolder));
			if (beta.Exists)
			{
				foreach (var folder in Folders) CopyFolder(Path.Combine(beta.FullName, folder), Path.Combine(mine.FullName, folder));
				foreach (var file in Files) CopyFile(Path.Combine(beta.FullName, file), Path.Combine(mine.FullName, file));
				foreach (var custom in beta.GetFiles("buzzer-custom.*")) CopyFile(custom.FullName, Path.Combine(mine.FullName, custom.Name));
				GD.Print($"Copied the beta's data from {beta.FullName} to {mine.FullName}.");
			}
			Directory.CreateDirectory(mine.FullName);
			File.WriteAllText(marker, beta.Exists ? "copied" : "nothing to copy");
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			GD.PrintErr($"Couldn't copy the beta's data: {e.Message}");
		}
	}

	static void CopyFolder(string from, string to)
	{
		if (!Directory.Exists(from)) return;
		Directory.CreateDirectory(to);
		foreach (var file in Directory.GetFiles(from)) CopyFile(file, Path.Combine(to, Path.GetFileName(file)));
		foreach (var dir in Directory.GetDirectories(from)) CopyFolder(dir, Path.Combine(to, Path.GetFileName(dir)));
	}

	static void CopyFile(string from, string to)
	{
		if (File.Exists(from) && !File.Exists(to)) File.Copy(from, to);
	}
}
