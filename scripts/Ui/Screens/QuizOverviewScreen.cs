using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Server;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

/// <summary>One quiz: title, its questions (reorder, delete) and "add a question".</summary>
public partial class QuizOverviewScreen : Control
{
	readonly Quiz _quiz;
	readonly VBoxContainer _questions = U.V(6);
	readonly HBoxContainer _sortBar = Tutorial.Mark(U.H(8), "overview.sort");
	/// <summary>How the list is shown: "play" (the order they're played in), "type" or "created". Only the view: the play order
	/// changes only with "Play them in this order". Kept while the game runs, for every quiz.</summary>
	static string _sort = "play";

	/// <summary>Dev runs (--screen quiz --list category): how the list opens.</summary>
	public static string ListedBy { set => _sort = value; }
	readonly VBoxContainer _foot = U.V(8);
	readonly Label _error = U.Error();
	readonly LineEdit _title;
	/// <summary>Ctrl+Z / Ctrl+Y: moving, deleting and renaming, back and forth (the questions themselves aren't copied).</summary>
	readonly EditHistory<(string Title, List<Question> Questions, List<string> Categories)> _history;
	/// <summary>The questions picked with Ctrl+click or Shift+click: they move (drag, to top/bottom) and delete together.</summary>
	readonly HashSet<Question> _picked = new();
	/// <summary>Where a Shift+click's range starts: the last question clicked with Ctrl or Shift.</summary>
	Question? _anchor;
	readonly HBoxContainer _pickBar = U.H(8);
	readonly Dictionary<Question, PanelContainer> _rows = new();

	public QuizOverviewScreen() : this(new Quiz()) { }

	public QuizOverviewScreen(Quiz quiz)
	{
		_quiz = quiz;
		SetAnchorsPreset(LayoutPreset.FullRect);

		// Saving rewrites the whole file (images included), so the title is saved when you leave the field.
		var title = _title = U.Input(quiz.Title, "Quiz title", t =>
		{
			_quiz.Title = t;
			_titleDirty = true;
			RefreshFoot();
		}, QuestionLimits.TitleMaxLength);
		title.FocusExited += SaveTitle;
		title.TextSubmitted += _ => SaveTitle();
		title.AddThemeFontOverride("font", Fonts.Black);
		title.AddThemeFontSizeOverride("font_size", 22);

		var sidebar = U.V(12,
			U.Link("← My quizzes", App.I.GoQuizzes, 14).With(b => b.SizeFlagsHorizontal = SizeFlags.ShrinkBegin),
			U.Eyebrow("Quiz"),
			Tutorial.Mark(title, "overview.title"),
			_sortBar,
			_pickBar,
			Tutorial.Mark(new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }.With(s => s.AddChild(_questions.Grow())).GrowV(), "overview.list"),
			_error,
			Tutorial.Mark(_foot, "overview.foot"));
		var side = U.Panel(sidebar).MinSize(380);
		side.AddThemeStyleboxOverride("panel", UiTheme.Box(C.Panel, 0, null, 0, 18, 18));

		var types = Tutorial.Mark(new GridContainer { Columns = 3 }, "overview.types");
		types.AddThemeConstantOverride("h_separation", 12);
		types.AddThemeConstantOverride("v_separation", 12);
		// (alphabetical by the name shown, so also in German)
		foreach (var type in QuestionType.All.OrderBy(t => Lang.T(QuestionType.Infos[t].Label), System.StringComparer.CurrentCultureIgnoreCase))
		{
			var info = QuestionType.Infos[type];
			// Layers: the clickable card, the name on top (it sets the height: long names wrap, e.g. in German), and the "?"
			// on the right, level with the name, which tells what the type does (hover) and its variants (click).
			var card = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore }.Grow();
			var button = new Button { ThemeTypeVariation = "", MouseDefaultCursorShape = CursorShape.PointingHand, CustomMinimumSize = new Vector2(0, 76) };
			// (a key like every button, KeyBox: lighter under the mouse, pushed in when clicked)
			button.AddThemeStyleboxOverride("normal", new KeyBox(C.Panel2, 16, 16, 14, 3, 6, false));
			button.AddThemeStyleboxOverride("hover", new KeyBox(C.Panel2.Lightened(0.1f), 16, 16, 14, 3, 6, false));
			button.AddThemeStyleboxOverride("pressed", new KeyBox(C.Panel2.Darkened(0.05f), 16, 16, 14, 3, 6, true));
			button.AddThemeStyleboxOverride("hover_pressed", new KeyBox(C.Panel2, 16, 16, 14, 3, 6, true));
			button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			var t = type;
			button.Pressed += () => App.I.GoQuestion(_quiz.Id, null, t);
			card.AddChild(button);
			var inner = GameParts.Row(12, new TypeIcon(type, 28), U.Bold(info.Label, 17).Grow().With(l => l.AutowrapMode = TextServer.AutowrapMode.Word));
			// (the row and everything in it let clicks through to the card underneath)
			inner.MouseFilter = MouseFilterEnum.Ignore;
			foreach (var c in inner.GetChildren().OfType<Control>()) c.MouseFilter = MouseFilterEnum.Ignore;
			card.AddChild(U.Pad(inner, 18, 12, 44, 18).With(m => m.MouseFilter = MouseFilterEnum.Ignore));
			var corner = new Control { MouseFilter = MouseFilterEnum.Ignore };
			var help = U.Link("?", () => Dialogs.Info(this, TypeHelp(info), info.Label), 20);
			help.AnchorLeft = help.AnchorRight = 1;
			help.AnchorTop = help.AnchorBottom = 0.5f;
			(help.OffsetLeft, help.OffsetTop, help.OffsetRight, help.OffsetBottom) = (-40, -24, -12, 8);
			corner.AddChild(help);
			card.AddChild(corner);
			types.AddChild(card);
		}
		var main = U.V(10, GameParts.Row(12, U.Heading("Add a question", 30).Grow(), new UpdateBadge()), U.Muted("Pick a type, then fill in the blanks. No code needed. The ? on a type tells what it does.", 15), U.Gap(6), types);

		var layout = U.H(0, side, U.Pad(new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }.With(s => s.AddChild(main.Grow())), 32, 28, 32, 28).Grow().GrowV());
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(layout);
		// (the jukebox and 🐞 buttons go in the free bottom right: in the bottom left they'd cover "Copy share code")
		var spot = ButtonDock.Spot(ButtonDock.Note);
		AddChild(spot);
		spot.Size = spot.CustomMinimumSize;
		spot.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.KeepSize, (int)ButtonDock.Margin);
		RefreshList();
		_history = new(() => (_quiz.Title, _quiz.Questions.ToList(), _quiz.CategoryOrder.ToList()), state =>
		{
			_quiz.Title = state.Title;
			_quiz.Questions = state.Questions.ToList();
			_quiz.CategoryOrder = state.Categories.ToList();
			_title.Text = state.Title;
			_titleDirty = false;
			Save();
			RefreshList();
		});
	}

	public bool Undo() => _history.Undo();

	public bool Redo() => _history.Redo();

	bool _titleDirty;

	void SaveTitle()
	{
		if (!_titleDirty) return;
		_titleDirty = false;
		Save();
		_history.Record();
	}

	public override void _ExitTree() => SaveTitle();

	void Save()
	{
		try
		{
			QuizStore.Save(_quiz);
			_error.Visible = false;
		}
		catch (UserError e)
		{
			_error.Text = e.Message;
			_error.Visible = true;
		}
	}

	void RefreshList()
	{
		_questions.Clear();
		_rows.Clear();
		_picked.IntersectWith(_quiz.Questions);
		if (_quiz.Questions.Count == 0) _questions.AddChild(U.Muted("No questions yet — pick a type on the right.", 13));
		string? group = null;
		foreach (var i in Shown())
		{
			// (by category: a heading for each, with arrows that change the categories' order)
			if (_sort == "category" && _quiz.Questions[i].Category is var cat && cat != group)
			{
				group = cat;
				_questions.AddChild(CategoryHead(cat));
			}
			_questions.AddChild(Row(i));
		}
		RefreshSortBar();
		RefreshPicked();
		RefreshFoot();
	}

	/// <summary>The categories in the quiz's order (CategoryOrder first, the others A–Z), without "no category".</summary>
	List<string> CategoryList() =>
		_quiz.Questions.Select(q => q.Category).Where(c => c != "").Distinct()
			.OrderBy(CategoryRank).ThenBy(Categories.Label, System.StringComparer.CurrentCultureIgnoreCase).ToList();

	/// <summary>Where a category goes: its place in the quiz's order, then the rest (A–Z), questions without one last.</summary>
	int CategoryRank(string category) => category == "" ? int.MaxValue : _quiz.CategoryOrder.IndexOf(category) is var k and >= 0 ? k : 100_000;

	/// <summary>A category's heading in "By category": its name, how many, and ▲ ▼ to move it before or after the next one.</summary>
	Control CategoryHead(string category)
	{
		var count = _quiz.Questions.Count(q => q.Category == category);
		var name = category == "" ? U.Text(Lang.T("No category yet"), 15, new Color(C.Gold, 0.85f), Fonts.Bold) : U.Bold(Categories.Label(category), 15).Raw();
		var row = GameParts.Row(6, name, U.Muted(Lang.F("· {0}", count), 13, false), U.Fill());
		if (category != "")
		{
			var list = CategoryList();
			var at = list.IndexOf(category);
			var up = U.Small("▲", () => MoveCategory(category, -1)).Tip("Play this category earlier");
			var down = U.Small("▼", () => MoveCategory(category, 1)).Tip("Play this category later");
			up.Disabled = at <= 0;
			down.Disabled = at >= list.Count - 1;
			row.AddChild(up);
			row.AddChild(down);
		}
		return U.Pad(row, 2, 8, 2, 0);
	}

	void MoveCategory(string category, int by)
	{
		var list = CategoryList();
		var at = list.IndexOf(category);
		if (at < 0 || at + by < 0 || at + by >= list.Count) return;
		(list[at], list[at + by]) = (list[at + by], list[at]);
		_quiz.CategoryOrder = list;
		Save();
		_history.Record();
		Callable.From(RefreshList).CallDeferred();
	}

	/// <summary>The questions' play indexes in the order the list shows them (ties keep the play order).</summary>
	List<int> Shown()
	{
		var all = Enumerable.Range(0, _quiz.Questions.Count);
		return (_sort switch
		{
			"type" => all.OrderBy(i => System.Array.IndexOf(QuestionType.All, KindOf(_quiz.Questions[i]))),
			// (in the quiz's category order, then A–Z by the category as it's shown; questions without one last, where they stand out)
			"category" => all.OrderBy(i => CategoryRank(_quiz.Questions[i].Category))
				.ThenBy(i => Categories.Label(_quiz.Questions[i].Category), System.StringComparer.CurrentCultureIgnoreCase),
			// (questions made before 0.12.0d5 don't know when: they come first, in play order)
			"created" => all.OrderBy(i => _quiz.Questions[i].CreatedAt),
			_ => all.OrderBy(i => i),
		}).ThenBy(i => i).ToList();
	}

	/// <summary>A picture reveal sorts with the pictures (it's one of their modes).</summary>
	static string KindOf(Question q) => q.Type == QuestionType.Image ? QuestionType.Picture : q.Type;

	void RefreshSortBar()
	{
		_sortBar.Clear();
		_sortBar.Visible = _quiz.Questions.Count > 1;
		_sortBar.AddChild(U.Muted("Show", 13, false).With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		// (one drop-down: three buttons took the whole row, and a fourth way didn't fit)
		_sortBar.AddChild(U.Select(new[] { ("play", "Play order"), ("type", "By type"), ("category", "By category"), ("created", "Order made") }, _sort, id =>
		{
			_sort = id;
			Callable.From(RefreshList).CallDeferred();
		}).With(s =>
		{
			s.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			s.CustomMinimumSize = new Vector2(200, 0);
		}));
		_sortBar.AddChild(U.Gap(0).Grow());
		var shown = Shown();
		if (_sort != "play" && !shown.SequenceEqual(Enumerable.Range(0, shown.Count)))
			_sortBar.AddChild(U.Small("Play in this order", () =>
			{
				_quiz.Questions = shown.Select(i => _quiz.Questions[i]).ToList();
				_sort = "play";
				Save();
				_history.Record();
				RefreshList();
			}).Tip("Changes the order the questions are played in to the one shown (Ctrl+Z takes it back)"));
	}

	Control Row(int i)
	{
		var q = _quiz.Questions[i];
		var broken = QuizRules.QuestionIssues(q, _quiz.Maps).Count > 0;
		// (the number is a field: type another one to move the question there)
		var num = new LineEdit
		{
			Text = $"{i + 1}", Alignment = HorizontalAlignment.Center, MaxLength = 3, CustomMinimumSize = new Vector2(34, 0),
			SizeFlagsVertical = SizeFlags.ShrinkCenter, SelectAllOnFocus = true, TooltipText = Lang.T("Type a number to move the question there"),
		};
		foreach (var style in new[] { "normal", "read_only" }) num.AddThemeStyleboxOverride(style, UiTheme.Box(C.ForType(q.Type), 6, null, 0, 4, 2));
		num.AddThemeStyleboxOverride("focus", UiTheme.Box(C.ForType(q.Type), 6, C.Text, 2, 4, 2));
		num.AddThemeColorOverride("font_color", C.AccentInk);
		num.AddThemeColorOverride("caret_color", C.AccentInk);
		num.AddThemeFontOverride("font", Fonts.Bold);
		num.AddThemeFontSizeOverride("font_size", 13);
		num.AddThemeConstantOverride("minimum_character_width", 2);
		void Renumber()
		{
			if (!int.TryParse(num.Text.Trim(), out var to)) to = i + 1;
			to = Mathf.Clamp(to, 1, _quiz.Questions.Count) - 1;
			if (to != i) Callable.From(() => Move(i, to)).CallDeferred();
			else num.Text = $"{i + 1}";
		}
		num.TextSubmitted += _ => Renumber();
		num.FocusExited += Renumber;
		var prompt = U.Text(q.Prompt == "" ? "No question text" : q.Prompt, 14, q.Prompt == "" ? C.Muted : C.Text, Fonts.Bold);
		prompt.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		prompt.ClipText = true;
		prompt.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var sub = U.H(0, U.Muted(QuizRules.Summary(q), 12, false));
		if (QuizRules.MapNeedsCheck(q, _quiz.Maps)) sub.AddChild(U.Text(" · ⚠ map changed: check the spot", 12, C.Orange));
		else if (broken) sub.AddChild(U.Text(" · incomplete", 12, C.Accent));
		// (the category under the name; a quiet yellow note when it has none yet: the stats per category need one)
		sub.AddChild(q.Category != ""
			? U.Muted(" · " + Categories.Label(q.Category), 12, false).Raw()
			: U.Text(" · " + Lang.T("no category yet"), 12, new Color(C.Gold, 0.85f)).Tip("Pick one in the question's round settings: the stats per category need it"));
		var text = U.V(0, prompt, sub).Grow();
		var grip = new GripIcon { CustomMinimumSize = new Vector2(12, 20), SizeFlagsVertical = SizeFlags.ShrinkCenter };
		// (click opens the question; drag it onto another row to move it there)
		var open = new QuestionRowButton(i, MoveMany) { Draggable = _sort == "play", Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand,
			TooltipText = Lang.T("Click to edit, drag to move. Ctrl+click or Shift+click picks several.") }.Grow();
		// (Ctrl+click picks or drops one, Shift+click everything from the last one picked to here; a plain click opens it)
		open.Pressed += () =>
		{
			if (Input.IsKeyPressed(Key.Ctrl) || Input.IsKeyPressed(Key.Meta)) Pick(q, false);
			else if (Input.IsKeyPressed(Key.Shift)) Pick(q, true);
			else App.I.GoQuestion(_quiz.Id, q.Id, null);
		};
		if (_sort != "play") open.TooltipText = Lang.T("Click to edit (drag to move in play order). Ctrl+click or Shift+click picks several.");
		// (dragging a picked row takes all the picked ones with it, in their play order)
		open.Dragged = () => _picked.Contains(q) ? _picked.Select(p => _quiz.Questions.IndexOf(p)).Where(x => x >= 0).OrderBy(x => x).ToList() : new List<int> { i };
		open.Preview = () =>
		{
			var many = _picked.Contains(q) && _picked.Count > 1;
			return U.Panel(U.Text(many ? Lang.F("{0} questions", _picked.Count) : q.Prompt == "" ? Lang.T("No question text") : q.Prompt, 14, C.Text, Fonts.Bold).Raw(), "Card2")
				.With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(C.Panel2, 10, C.Accent, 2, 12, 8)));
		};
		var content = U.H(10, text, grip);
		content.MouseFilter = MouseFilterEnum.Ignore;
		foreach (var c in content.GetChildren().OfType<Control>()) c.MouseFilter = MouseFilterEnum.Ignore;
		foreach (var c in text.GetChildren().OfType<Control>()) c.MouseFilter = MouseFilterEnum.Ignore;
		open.AddChild(content.With(c => c.SetAnchorsPreset(LayoutPreset.FullRect)));
		open.CustomMinimumSize = new Vector2(0, 46);
		var del = U.Small("×", () =>
		{
			// (its map goes with it: from the quiz if no other question uses it, and its downloaded built-in map from this
			// PC if asked and no other question or quiz uses that either)
			var map = q.Type == QuestionType.MapPin ? _quiz.MapById(q.MapId) : null;
			var preset = MapPresets.Get(map?.PresetId);
			var onlyHere = preset != null && MapPacks.SizeOnDisk(preset) > 0
				&& !_quiz.Questions.Any(o => o != q && o.Type == QuestionType.MapPin && _quiz.MapById(o.MapId)?.PresetId == preset.Id)
				&& !QuizStore.List().Where(o => o.Id != _quiz.Id).SelectMany(MapPacks.Used).Contains(preset);
			var dropFile = false;
			Dialogs.Confirm(this, "Delete this question?", "Delete", () =>
			{
				_quiz.Questions.RemoveAt(i);
				_quiz.DropUnusedMaps();
				Save();
				_history.Record();
				if (dropFile && preset != null) MapPacks.Delete(new[] { preset.Id });
				RefreshList();
			}, option: onlyHere ? Lang.F("Also delete its map from this PC ({0}, {1} MB)", preset!.Name, System.Math.Max(1, MapPacks.SizeOnDisk(preset) / 1_000_000)) : null,
				optionChosen: on => dropFile = on);
		}).Tip("Delete");
		var panel = U.Panel(U.H(10, num, open, del), "Card2");
		_rows[q] = panel;
		Style(q);
		return panel;
	}

	/// <summary>A row's frame: the accent colour and thicker while it's picked.</summary>
	void Style(Question q)
	{
		if (!_rows.TryGetValue(q, out var panel)) return;
		var on = _picked.Contains(q);
		panel.AddThemeStyleboxOverride("panel", UiTheme.Box(on ? C.Panel2.Lightened(0.06f) : C.Panel2, 10, on ? C.Accent : C.Border, on ? 2 : 1, 8, 4));
	}

	/// <summary>Ctrl+click (range: false) picks or drops one; Shift+click picks everything shown between the last one and this.</summary>
	void Pick(Question q, bool range)
	{
		if (range && _anchor != null && _quiz.Questions.Contains(_anchor))
		{
			var shown = Shown().Select(i => _quiz.Questions[i]).ToList();
			var (a, b) = (shown.IndexOf(_anchor), shown.IndexOf(q));
			foreach (var x in shown.Skip(System.Math.Min(a, b)).Take(System.Math.Abs(a - b) + 1)) _picked.Add(x);
		}
		else if (!_picked.Remove(q)) _picked.Add(q);
		_anchor = q;
		foreach (var row in _rows.Keys) Style(row);
		RefreshPicked();
	}

	/// <summary>"3 picked" with what you can do with them (move to the top or bottom, delete), when any are picked.</summary>
	void RefreshPicked()
	{
		_pickBar.Clear();
		_pickBar.Visible = _picked.Count > 0;
		if (_picked.Count == 0) return;
		var indexes = _picked.Select(p => _quiz.Questions.IndexOf(p)).OrderBy(x => x).ToList();
		_pickBar.AddChild(U.Text(Lang.F("{0} picked", _picked.Count), 13, C.Accent, Fonts.Bold).With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		_pickBar.AddChild(U.Gap(0).Grow());
		_pickBar.AddChild(U.Small("To the top", () => MoveMany(indexes, 0)).Tip("Plays them first, in their order"));
		_pickBar.AddChild(U.Small("To the end", () => MoveMany(indexes, _quiz.Questions.Count)).Tip("Plays them last, in their order"));
		_pickBar.AddChild(U.Small("Delete", () => Dialogs.Confirm(this, Lang.F("Delete these {0} questions?", _picked.Count), "Delete", () =>
		{
			_quiz.Questions.RemoveAll(_picked.Contains);
			_picked.Clear();
			_quiz.DropUnusedMaps();
			Save();
			_history.Record();
			RefreshList();
		})));
		_pickBar.AddChild(U.Link("Clear", () =>
		{
			_picked.Clear();
			foreach (var row in _rows.Keys) Style(row);
			RefreshPicked();
		}, 13).With(b => b.SizeFlagsVertical = SizeFlags.ShrinkCenter));
	}

	/// <summary>One question to a new place (its number field).</summary>
	void Move(int from, int to)
	{
		if (from == to || from < 0 || to < 0 || from >= _quiz.Questions.Count || to >= _quiz.Questions.Count) return;
		var item = _quiz.Questions[from];
		_quiz.Questions.RemoveAt(from);
		_quiz.Questions.Insert(to, item);
		Save();
		_history.Record();
		RefreshList();
	}

	/// <summary>
	/// Questions (play indexes) to the slot before question <paramref name="slot"/> (Count = the end), together and in
	/// their order: a drag, or the picked ones to the top or end.
	/// </summary>
	void MoveMany(List<int> from, int slot)
	{
		var moving = from.Where(x => x >= 0 && x < _quiz.Questions.Count).Distinct().OrderBy(x => x).ToList();
		if (moving.Count == 0) return;
		var items = moving.Select(x => _quiz.Questions[x]).ToList();
		// (taking them out first shifts the slot up by those before it)
		var at = System.Math.Clamp(slot - moving.Count(x => x < slot), 0, _quiz.Questions.Count - items.Count);
		var rest = _quiz.Questions.Where(q => !items.Contains(q)).ToList();
		rest.InsertRange(at, items);
		if (rest.SequenceEqual(_quiz.Questions)) return;
		_quiz.Questions = rest;
		Save();
		_history.Record();
		RefreshList();
	}

	void RefreshFoot()
	{
		_foot.Clear();
		var issues = QuizRules.QuizIssues(_quiz);
		if (issues.Count > 0 && _quiz.Questions.Count > 0)
			_foot.AddChild(U.Muted(Lang.F("{0} to fix before hosting.", U.Plural(issues.Count, "thing", "things")), 13));
		if (NightSaves.Load(_quiz.Id) is { } save) _foot.AddChild(ContinueBox(this, _quiz, save, issues.FirstOrDefault(), RefreshFoot));
		// (built-in maps it uses that aren't on this PC: MapPacks)
		if (MapDownload.MissingText(_quiz) is { Length: > 0 } maps)
			_foot.AddChild(U.H(10, U.Muted(maps, 13).Raw().Grow(), U.Small("Download now", async () =>
			{
				if (await MapDownload.Ensure(this, MapPacks.Missing(_quiz)) && IsInstanceValid(this)) RefreshFoot();
			})));
		var host = U.Btn("Open lobby", "AccentButton", () => App.I.HostQuiz(_quiz)).Grow().Tip(issues.FirstOrDefault());
		host.Disabled = issues.Count > 0;
		_foot.AddChild(U.H(8, U.Btn("Copy share code", "", () => ShareInfo.Copy(this, _quiz)), U.Btn("Export file", "",() => QuizListScreen.ExportQuiz(this, _quiz, _error)), host));
	}

	/// <summary>
	/// A quiz night that didn't finish (the game crashed, or was closed to update mid-game): when it was saved, how far it
	/// got, who leads, and Continue, which opens a lobby with everyone's points up to the last finished question.
	/// </summary>
	public static Control ContinueBox(Node owner, Quiz quiz, SavedNight save, string? issue, System.Action discarded)
	{
		var leaders = save.Players.OrderByDescending(p => p.Score).Take(3).Select(p => $"{p.Name} {p.Score}");
		var box = U.V(4,
			U.Bold("Unfinished quiz night", 16),
			U.Muted(Lang.F("Saved {0} after question {1} of {2} · {3}", save.SavedAt.ToString("dd.MM. HH:mm", System.Globalization.CultureInfo.InvariantCulture),
				save.Next, save.Questions, U.Plural(save.Players.Count, "player", "players")), 13),
			U.Muted(string.Join("  ·  ", leaders), 13).Raw());
		if (save.Questions != quiz.Questions.Count)
			box.AddChild(U.Text(Lang.F("⚠ The quiz has changed since: it had {0} questions, now {1}.", save.Questions, quiz.Questions.Count), 13, C.Orange, wrap: true));
		var go = U.Btn("Continue", "AccentButton", () => App.I.HostQuiz(quiz, null, save))
			.Tip(issue ?? Lang.T("Opens a lobby with everyone's points. Players join with the new code and get their seat back."));
		go.Disabled = issue != null;
		box.AddChild(U.H(8, U.Link("Discard", () => Dialogs.Confirm(owner, "Throw this quiz night away? Its points are gone for good.", "Discard", () =>
		{
			NightSaves.Delete(quiz.Id);
			discarded();
		}), 13).With(b => b.SizeFlagsVertical = SizeFlags.ShrinkCenter), U.Fill(), go));
		return U.Panel(box, "Inset");
	}

	/// <summary>A type's help, translated: what it does, then its variants as a list.</summary>
	static string TypeHelp(QuestionType.Info info) =>
		Lang.T(info.Intro) + "\n\n" + string.Join("\n\n", (info.Details ?? System.Array.Empty<string>()).Select(d => "• " + Lang.T(d)));
}

/// <summary>
/// A question row in the quiz overview: clicking opens it, dragging it onto another row moves it there (a line shows
/// where it lands: above the row's middle before it, below after it). A picked row takes the other picked ones along.
/// </summary>
public partial class QuestionRowButton : Button
{
	readonly int _index;
	/// <summary>Moves these questions (play indexes) to the slot before that question (Count: the end).</summary>
	readonly System.Action<List<int>, int> _move;
	public System.Func<Control>? Preview;
	/// <summary>The questions a drag of this row moves (just this one, or every picked one).</summary>
	public System.Func<List<int>>? Dragged;
	/// <summary>Off while the list is sorted another way than the play order (where a row lands would be unclear).</summary>
	public bool Draggable = true;
	/// <summary>Where a dragged row would land while one is over this row: -1 above, 1 below, 0 none.</summary>
	int _dropSide;

	public QuestionRowButton() : this(0, (_, _) => { }) { }

	public QuestionRowButton(int index, System.Action<List<int>, int> move)
	{
		_index = index;
		_move = move;
		MouseExited += () => SetDropSide(0);
	}

	const string DragKind = "quiznight_question_row";

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (!Draggable) return default;
		if (Preview?.Invoke() is { } preview)
		{
			preview.Modulate = new Color(1, 1, 1, 0.9f);
			preview.CustomMinimumSize = new Vector2(Mathf.Min(Size.X, 420), 0);
			SetDragPreview(preview);
		}
		var moving = Dragged?.Invoke() ?? new List<int> { _index };
		return new Godot.Collections.Dictionary { ["kind"] = DragKind, ["indexes"] = new Godot.Collections.Array<int>(moving) };
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!Draggable || data.VariantType != Variant.Type.Dictionary || data.AsGodotDictionary() is not { } d || !d.ContainsKey("kind") || d["kind"].AsString() != DragKind) return false;
		SetDropSide(atPosition.Y < Size.Y / 2 ? -1 : 1);
		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var from = data.AsGodotDictionary()["indexes"].AsGodotArray().Select(v => v.AsInt32()).ToList();
		var below = atPosition.Y >= Size.Y / 2;
		SetDropSide(0);
		// (the slot between rows: dropping below row 3 lands after it)
		_move(from, _index + (below ? 1 : 0));
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd) SetDropSide(0);
	}

	void SetDropSide(int side)
	{
		if (_dropSide == side) return;
		_dropSide = side;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_dropSide == 0) return;
		var y = _dropSide < 0 ? -5f : Size.Y + 5f;
		DrawLine(new Vector2(-40, y), new Vector2(Size.X + 40, y), C.Accent, 4);
	}
}

/// <summary>Six dots: the handle that says "you can drag this row".</summary>
public partial class GripIcon : Control
{
	public GripIcon() => MouseFilter = MouseFilterEnum.Ignore;

	public override void _Draw()
	{
		for (var row = 0; row < 3; row++)
			for (var col = 0; col < 2; col++)
				DrawCircle(new Vector2(Size.X * (0.25f + col * 0.5f), Size.Y * (0.2f + row * 0.3f)), 1.8f, C.Muted);
	}
}
