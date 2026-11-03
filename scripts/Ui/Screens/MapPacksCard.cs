// Settings' "Map packs" card: every built-in map with what it takes on this PC or to download, a button to get it
// ahead of time or free the space, and "Download all". Maps still download by themselves when a quiz needs one
// (MapPacks); this is only for looking and tidying. Dev runs have every map built in (--map-packs acts like an export).

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

public static class MapPacksCard
{
	/// <summary>A pack being downloaded from this card, and how far (0–1); shared so a rebuilt card keeps showing it.</summary>
	static readonly Dictionary<string, float> Busy = new();
	static Dictionary<string, long>? _sizes;

	public static Control Make()
	{
		var list = U.V(6);
		var total = U.Muted("", 12);
		var error = U.Error();
		var all = U.Small("⬇ Download all");
		var card = U.Card(10, U.Eyebrow("Map packs"),
			U.Muted("The built-in maps download by themselves when a quiz needs one. Here you can get them ahead of time, or free the space.", 12),
			list, U.H(10, all, total.Grow()), error);

		void Fill()
		{
			if (!GodotObject.IsInstanceValid(list)) return;
			foreach (var child in list.GetChildren()) child.QueueFree();
			long onDisk = 0;
			var here = 0;
			foreach (var preset in MapPresets.All)
			{
				// (the race tracks under their own heading, after the game maps)
				if (preset.Category != null && preset == MapPresets.All.First(m => m.Category == preset.Category))
					list.AddChild(U.Eyebrow(preset.Category));
				var downloaded = MapPacks.IsDownloaded(preset.Id);
				var size = MapPacks.SizeOnDisk(preset);
				onDisk += size;
				if (downloaded) here++;
				var download = _sizes != null && _sizes.TryGetValue(preset.Id, out var s) ? s : 0;
				string status;
				if (Busy.TryGetValue(preset.Id, out var p)) status = Lang.F("Downloading… {0} %", (int)(p * 100));
				else if (!MapPacks.Optional) status = Lang.T("Built in (dev run)");
				else if (downloaded) status = Lang.F("On this PC · {0}", Mb(size));
				else status = download > 0 ? Lang.F("Not downloaded · {0}", Mb(download)) : Lang.T("Not downloaded");
				Button? action = null;
				if (MapPacks.Optional && !Busy.ContainsKey(preset.Id))
					action = downloaded
						? U.Small("Delete", () => Dialogs.Confirm(card, Lang.F("Delete the map {0} from this PC? It downloads again when a quiz needs it.", preset.Name), "Delete", () =>
						{
							MapPacks.Delete(new[] { preset.Id });
							Fill();
						}))
						: U.Small("⬇ Download", () => Get(new[] { preset }));
				var row = U.H(10, U.Text(preset.Name, 14, C.Text, Fonts.Bold).Raw().Grow(), U.Muted(status, 12, false));
				if (action != null) row.AddChild(action);
				list.AddChild(row);
			}
			total.Text = MapPacks.Optional ? Lang.F("{0} of {1} maps on this PC · {2}", here, MapPresets.All.Count, Mb(onDisk)) : "";
			all.Disabled = !MapPacks.Optional || Busy.Count > 0 || here == MapPresets.All.Count;
		}

		async void Get(IReadOnlyCollection<MapPreset> presets)
		{
			error.Text = "";
			foreach (var p in presets) Busy[p.Id] = 0;
			Fill();
			foreach (var preset in presets)
			{
				var problem = await MapPacks.EnsureAsync(new[] { preset }, f =>
				{
					// (a new row list only when the shown percent changes)
					var changed = (int)(f * 100) != (int)(Busy.GetValueOrDefault(preset.Id) * 100);
					Busy[preset.Id] = f;
					if (changed) Fill();
				});
				Busy.Remove(preset.Id);
				if (problem != null)
				{
					foreach (var p in presets) Busy.Remove(p.Id);
					if (GodotObject.IsInstanceValid(error)) error.Text = problem;
					break;
				}
			}
			Fill();
		}

		all.Pressed += () => Get(MapPresets.All.Where(p => !MapPacks.IsDownloaded(p.Id) && !Busy.ContainsKey(p.Id)).ToList());
		Fill();
		if (_sizes == null && MapPacks.Optional)
			MapPacks.PackSizes().ContinueWith(t => Callable.From(() =>
			{
				_sizes = t.Result;
				Fill();
			}).CallDeferred());
		return card;
	}

	static string Mb(long bytes) => bytes <= 0 ? "0 MB" : bytes < 1_000_000 ? "<1 MB" : $"{bytes / 1e6:0} MB";
}
