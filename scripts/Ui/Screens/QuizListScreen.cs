using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

public partial class QuizListScreen : Control
{
	readonly VBoxContainer _list = U.V(12);
	readonly Label _error = U.Error();
	/// <summary>"Deleted … Undo" after a delete (0.14).</summary>
	readonly HBoxContainer _undo = U.H(10);
	/// <summary>The list of recently deleted quizzes is open.</summary>
	bool _showDeleted;
	readonly VBoxContainer _deleted = U.V(8);

	public QuizListScreen()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		var head = U.H(10,
			U.Heading("My quizzes", 34).Grow(),
			new UpdateBadge(),
			Tutorial.Mark(U.H(10,
				U.Btn("Refresh", "", () => { QuizStore.Rescan(); Refresh(); }).On(Glyph.Kind.Refresh, 16)
					.Tip("Reads the quizzes folder again, for quiz files you put there yourself"),
				U.Btn("Open folder", "", () => OS.ShellOpen(QuizStore.Folder)).Tip("Where your quiz files are saved"),
				U.Btn("Import file", "", Import),
				U.Btn("Paste share code", "", PasteCode).Tip("Imports the share code you copied"),
				U.Btn("New quiz", "AccentButton", Create)), "quizzes.bar"));
		var page = U.V(14, U.Link("← Home", () => App.I.GoHome()).With(b => b.SizeFlagsHorizontal = SizeFlags.ShrinkBegin), head, _error, _undo, _deleted, _list);
		AddChild(U.Page(page, 980, 20));
		// (the folder again each time the list opens, so quiz files copied in by hand show without a restart)
		QuizStore.Rescan();
		Refresh();
	}

	void Refresh()
	{
		_list.Clear();
		AddDeleted();
		var quizzes = QuizStore.List();
		if (quizzes.Count == 0)
		{
			_list.AddChild(U.Card(8,
				U.Heading("No quizzes yet", 22).With(l => l.HorizontalAlignment = HorizontalAlignment.Center),
				U.Muted("Create your first quiz night, or import a .quiznight.json file — exported from the old Quiznight website or sent by a friend.", 15, true, HorizontalAlignment.Center)));
			return;
		}
		var grid = new GridContainer { Columns = 2 };
		grid.AddThemeConstantOverride("h_separation", 14);
		grid.AddThemeConstantOverride("v_separation", 14);
		foreach (var quiz in quizzes) grid.AddChild(Card(quiz));
		_list.AddChild(grid);
	}

	/// <summary>
	/// Over the list: the quizzes deleted in the last 30 days, to bring back (QuizStore.Restore). Before 0.14 a deleted
	/// quiz was gone for good (Max lost his with "Don't ask again" ticked).
	/// </summary>
	void AddDeleted()
	{
		_deleted.Clear();
		var deleted = QuizStore.Deleted();
		if (deleted.Count == 0) return;
		_deleted.AddChild(U.Link(Lang.F(_showDeleted ? "▾ Recently deleted ({0})" : "▸ Recently deleted ({0})", deleted.Count), () =>
		{
			_showDeleted = !_showDeleted;
			Refresh();
		}, 14).With(b => b.SizeFlagsHorizontal = SizeFlags.ShrinkBegin));
		if (!_showDeleted) return;
		var rows = U.V(8, U.Muted(Lang.F("Deleted quizzes are kept for {0} days, then they're gone for good.", QuizStore.KeepDeletedDays), 13));
		foreach (var (id, title, at) in deleted)
			rows.AddChild(GameParts.Row(10,
				U.Bold(title == "" ? Lang.T("Untitled quiz") : title, 15).Raw().With(l => { l.ClipText = true; l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; }).Grow(),
				U.Muted(Lang.F("deleted {0}", at.ToString("dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)), 13, false),
				U.Small("Restore", () => Restore(id))));
		_deleted.AddChild(U.Card(6, rows));
	}

	void Restore(string id)
	{
		_undo.Clear();
		if (QuizStore.Restore(id) == null) ShowError(Lang.T("Couldn't restore that quiz."));
		Refresh();
	}

	void ShowUndo(Quiz quiz)
	{
		_undo.Clear();
		_undo.AddChild(U.Muted(Lang.F("Deleted \"{0}\".", quiz.Title), 14, false).Raw().With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		_undo.AddChild(U.Small("Undo", () => Restore(quiz.Id)));
	}

	Control Card(Quiz quiz)
	{
		var ready = QuizRules.QuizIssues(quiz).Count == 0;
		var edited = DateTimeOffset.FromUnixTimeMilliseconds(quiz.UpdatedAt).LocalDateTime.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
		var title = U.Btn(quiz.Title == "" ? "Untitled quiz" : quiz.Title, "LinkButton2", () => App.I.GoQuiz(quiz.Id), 22);
		title.AddThemeColorOverride("font_color", C.Text);
		title.Alignment = HorizontalAlignment.Left;
		title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		title.ClipText = true;
		var info = U.H(8, U.Muted(Lang.F("{0} · edited {1}", U.Plural(quiz.Questions.Count, "question", "questions"), edited), 13, false));
		if (quiz.Questions.Any(q => QuizRules.MapNeedsCheck(q, quiz.Maps))) info.AddChild(U.Pill("A map changed: check", C.Orange));
		else if (!ready) info.AddChild(U.Pill("Not ready yet", C.Accent));
		var host = U.Btn("Open lobby", "AccentButton", () => App.I.HostQuiz(quiz), 14);
		host.Disabled = !ready;
		// (a night that didn't finish: Continue goes on where it stopped; the quiz's page tells more)
		Control resume = NightSaves.Load(quiz.Id) is { } save
			? U.Small(Lang.F("▶ Continue at question {0}", save.Next + 1), () => App.I.HostQuiz(quiz, null, save))
				.With(b => b.Disabled = !ready).Tip(Lang.F("An unfinished quiz night, saved {0}: everyone gets their points back", save.SavedAt.ToString("dd.MM. HH:mm", System.Globalization.CultureInfo.InvariantCulture)))
			: U.Fill();
		var actions = U.H(6,
			resume,
			U.Fill(),
			U.Small("Copy share code", () => ShareInfo.Copy(this, quiz)),
			U.Small("Export", () => Export(quiz)),
			host);
		// (the trash can in the top-right corner, away from the buttons you use all the time)
		var top = GameParts.Row(10, title.Grow(), TrashButton(quiz));
		return U.Card(6, top, info, U.Gap(4), actions).Grow();
	}

	/// <summary>The trash can: deletes the quiz, after asking (unless "Don't ask me again" was ticked once).</summary>
	Button TrashButton(Quiz quiz)
	{
		var dropMaps = false;
		// (its downloaded built-in maps no other quiz uses: offered to go with it, MapPacks)
		List<MapPreset> OnlyHere() => MapPacks.Used(quiz)
			.Where(p => MapPacks.SizeOnDisk(p) > 0 && !QuizStore.List().Where(o => o.Id != quiz.Id).SelectMany(MapPacks.Used).Contains(p)).ToList();
		void Delete()
		{
			var maps = dropMaps ? OnlyHere() : new();
			try
			{
				QuizStore.Delete(quiz.Id);
			}
			catch (UserError e)
			{
				ShowError(e.Message);
				return;
			}
			if (maps.Count > 0) MapPacks.Delete(maps.Select(m => m.Id));
			Refresh();
			ShowUndo(quiz);
		}
		var button = U.Small("", () =>
		{
			var maps = OnlyHere();
			var option = maps.Count == 0 ? null
				: Lang.F("Also delete its maps from this PC ({0}, {1} MB)", string.Join(", ", maps.Select(m => m.Name)), Math.Max(1, maps.Sum(MapPacks.SizeOnDisk) / 1_000_000));
			dropMaps = false;
			if (!Settings.AskBeforeDeletingQuiz && option == null) Delete();
			else Dialogs.Confirm(this, Lang.F("Delete \"{0}\"? You can bring it back from \"Recently deleted\" for 30 days.", quiz.Title), "Delete", Delete,
				dontAskAgain: Settings.AskBeforeDeletingQuiz ? () => Settings.AskBeforeDeletingQuiz = false : null,
				option: option, optionChosen: on => dropMaps = on);
		});
		// A small red key like the others on the card (Max asked for one that fits in, red, without the "Delete?" text that
		// showed under the mouse), the can in it in the key's dark ink; the tooltip says what it does.
		button.ThemeTypeVariation = "SmallCoralButton";
		button.TooltipText = Lang.T("Delete quiz");
		var icon = new TrashIcon(new Color("#2a0a0e"), 18) { MouseFilter = MouseFilterEnum.Ignore };
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		center.AddChild(icon);
		button.AddChild(center);
		var side = Mathf.Max(32, button.GetCombinedMinimumSize().Y);
		button.CustomMinimumSize = new Vector2(side, side);
		button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		return button;
	}

	void Create()
	{
		var quiz = QuizStore.Create();
		App.I.GoQuiz(quiz.Id);
	}

	async void PasteCode()
	{
		var text = DisplayServer.ClipboardGet();
		if (!ShareCode.Looks(text))
		{
			ShowError(Lang.T("Copy a share code first (it looks like QN-7K4P-9XZ2), then press Paste share code."));
			return;
		}
		try
		{
			Quiz quiz;
			if (ShareCode.ShortKey(text) is { } key)
			{
				var wait = Dialogs.Wait(this, "Looking up the share code…", "Share code");
				try
				{
					quiz = QuizStore.SaveImported(await QuizShare.Download(key));
				}
				finally
				{
					Dialogs.Close(wait);
				}
				if (!IsInstanceValid(this)) return;
			}
			else quiz = QuizStore.ImportCode(text);
			_error.Visible = false;
			Refresh();
			ShareInfo.Imported(this, quiz);
		}
		catch (UserError e)
		{
			ShowError(e.Message);
		}
	}

	void ShowError(string text)
	{
		_error.Text = text;
		_error.Visible = true;
	}

	void Import() => Dialogs.OpenFile(this, "Import a quiz file", QuizStore.ImportFilters, path =>
	{
		try
		{
			QuizStore.Import(path);
			_error.Visible = false;
			Refresh();
		}
		catch (UserError e)
		{
			_error.Text = e.Message;
			_error.Visible = true;
		}
	});

	public void Export(Quiz quiz) => ExportQuiz(this, quiz, _error);

	public static void ExportQuiz(Node owner, Quiz quiz, Label? error) =>
		Dialogs.SaveFile(owner, "Export quiz", QuizStore.SuggestedFileName(quiz), QuizStore.ZipFilters, path =>
		{
			try
			{
				QuizStore.Export(quiz, path.EndsWith(".zip") ? path : path + QuizStore.ZipExtension);
			}
			catch (UserError e)
			{
				if (error == null) return;
				error.Text = e.Message;
				error.Visible = true;
			}
		});

	public void OnFilesDropped(string[] files)
	{
		foreach (var f in files)
		{
			if (!f.EndsWith(".json") && !f.EndsWith(".txt") && !f.EndsWith(".zip")) continue;
			try
			{
				QuizStore.Import(f);
			}
			catch (UserError e)
			{
				_error.Text = e.Message;
				_error.Visible = true;
			}
		}
		Refresh();
	}
}
