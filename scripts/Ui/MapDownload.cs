// Getting built-in maps onto this PC (MapPacks) with a small "Downloading…" window, from wherever they're needed:
// the editor's map picker, a quiz's page, opening a lobby.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui;

public static class MapDownload
{
	/// <summary>
	/// Makes sure the maps are here (downloads them, or a newer version of them, showing how far it is). False if that
	/// didn't work: the player was told why.
	/// </summary>
	public static async Task<bool> Ensure(Node owner, IReadOnlyCollection<MapPreset> presets)
	{
		if (!MapPacks.Optional || presets.Count == 0) return true;
		var names = string.Join(", ", presets.Select(p => p.Name));
		AcceptDialog? wait = null;
		// (only shown when it takes a moment: maps that are here and current are checked in a blink)
		var shownAt = Time.GetTicksMsec() + 400;
		var task = MapPacks.EnsureAsync(presets, p =>
		{
			if (wait == null && Time.GetTicksMsec() < shownAt) return;
			var text = Lang.F("Downloading the maps: {0} … {1} %", names, (int)(p * 100));
			if (wait == null) wait = Dialogs.Wait(owner, text, "Maps");
			else if (GodotObject.IsInstanceValid(wait)) wait.DialogText = text;
		});
		var error = await task;
		if (wait != null) Dialogs.Close(wait);
		if (error == null) return true;
		if (GodotObject.IsInstanceValid(owner)) Dialogs.Info(owner, error, "Maps");
		return false;
	}

	/// <summary>A short line about maps a quiz uses that aren't downloaded ("" if none).</summary>
	public static string MissingText(Quiz quiz) =>
		MapPacks.Missing(quiz) is { Count: > 0 } missing
			? Lang.F("This quiz uses maps that aren't downloaded yet: {0}. They're downloaded when you open the lobby, or now.", string.Join(", ", missing.Select(m => m.Name)))
			: "";
}
