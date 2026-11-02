// The guided tour's sample quiz (from 0.13.5): "Test Night", one of every question type with pictures, sound and video, is
// too big to ship with the game (13 MB), so it's downloaded in the background while the tour's first steps are on home and
// the quiz list. It sits on the "maps" release next to the map packs (tour.quiznight.zip, signed with Max's release key
// like them; tools/tour_quiz.sh puts it there), and comes the same way: the public releases repo, test builds the private
// test repo, debug runs QUIZNIGHT_MAPS_FEED. It's kept in user://tour only while the tour runs and read into memory, never
// saved to My quizzes (QuizStore.Save skips its Id). Not there in time, offline or not signed: the tour uses the small
// built-in sample quiz instead (Tutorial.TourQuiz).

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Storage;

public static class TourDownload
{
	/// <summary>The tour's quiz id, whichever quiz it is: QuizStore never saves a quiz with it.</summary>
	public const string Id = "guided-tour-sample";

	const string FileName = "tour.quiznight.zip";
	const long MaxBytes = 40L << 20;
	static string Dir => ProjectSettings.GlobalizePath("user://tour");

	/// <summary>The running (or finished) download; null when none was started since the last Clear.</summary>
	static Task<Quiz?>? _download;
	/// <summary>Counts tours (Start and Clear): a download that ends after its tour did deletes its file again.</summary>
	static int _tour;

	/// <summary>Starts the download in the background (once per tour).</summary>
	public static void Start()
	{
		if (_download != null || DisplayServer.GetName() == "headless") return;
		var tour = ++_tour;
		_download = Task.Run(() => Download(tour));
	}

	/// <summary>The downloaded quiz if it's ready by now, else null (the tour takes the built-in one, it never waits).</summary>
	/// <summary>Whether the download has ended (with the quiz or without), or none runs.</summary>
	public static bool Settled => _download is not { IsCompleted: false };

	public static Quiz? IfReady() => _download is { IsCompletedSuccessfully: true } done ? done.Result : null;

	/// <summary>The tour is over: forgets the quiz and deletes the downloaded file (a download still running is dropped
	/// when it ends; it checks whether it's still wanted).</summary>
	public static void Clear()
	{
		_download = null;
		_tour++;
		DeleteFiles();
	}

	static async Task<Quiz?> Download(int tour)
	{
		try
		{
			using var http = MapPacks.NewClient(TimeSpan.FromMinutes(5));
			var zip = await MapPacks.Fetch(http, FileName, max: MaxBytes);
			var sig = await MapPacks.Fetch(http, FileName + ".sig");
			// (only a quiz Max signed is opened, like the map packs and updates)
			if (!Updater.SignedByMax(SHA256.HashData(zip), sig)) throw new InvalidDataException("not signed with Max's release key");
			Directory.CreateDirectory(Dir);
			var path = Path.Combine(Dir, FileName);
			await File.WriteAllBytesAsync(path, zip);
			var quiz = QuizStore.ReadZip(path);
			quiz.Id = Id;
			Trace.Log("tour", $"sample quiz downloaded: {quiz.Questions.Count} questions, {zip.Length / 1_000_000} MB");
			return quiz;
		}
		catch (Exception e)
		{
			GD.Print($"The tour's sample quiz didn't download: {e.Message}");
			Trace.Log("tour", $"sample quiz failed: {e.Message}");
			return null;
		}
		finally
		{
			// (the tour ended while it downloaded: nothing left behind)
			if (System.Threading.Volatile.Read(ref _tour) != tour) DeleteFiles();
		}
	}

	/// <summary>Deletes user://tour (also a leftover from a tour the game was closed during).</summary>
	public static void DeleteFiles()
	{
		try
		{
			if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			GD.Print($"Couldn't delete the tour's quiz: {e.Message}");
		}
	}
}
