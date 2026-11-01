// Tutorials: the guided tour, the first time the home screen opens (Settings' "Take the tour" starts it again). The screen
// goes dark except for one thing at a time, which gets a glowing frame, and a bubble next to it says what it is for.
// Screens mark what a tour points at (Mark). The tour walks through the screens on its own (Step.Where): home, the quiz
// list, a sample quiz that only lives in memory, one of its questions in the editor, and back home. Skip (or Esc) ends
// it, "Turn off tutorials" too and keeps it from coming back. Dev: --tutorial guided shows it every time home opens,
// --tutorial-step N starts at that step.

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;
using QuizNight.Ui.Screens;

namespace QuizNight.Ui;

public static class Tutorial
{
	/// <summary>
	/// One step: the screen it's on (Where: home, quizzes, overview, editor; the tour goes there first), what it points at
	/// (a Mark key; null = nothing, the bubble in the middle), a title and the text.
	/// </summary>
	public record Step(string Where, string? Target, string Title, string Text);

	const string MetaKey = "tutorial_target";

	/// <summary>The guided tour's id (its "seen" mark in the settings).</summary>
	public const string Guided = "guided";

	public static readonly Dictionary<string, Step[]> Tours = new()
	{
		[Guided] = new Step[]
		{
			new("home", null, "Welcome to Quiznight!", "A short tour through the game: joining a quiz night, making your own quiz, and telling Max what you think. Skip it any time (Esc); Settings → \"Take the tour\" shows it again."),
			new("home", "home.join", "Join a quiz night", "Got a room code from a friend? Type it here with your name and press Join!. Everyone plays on their own PC; talk on Discord while you play."),
			new("home", "bug", "Found a bug? Got an idea?", "The bug button is on every screen. \"Report a bug\" if something doesn't work or looks wrong, \"Give feedback\" for an idea: a map, a question type, anything. Max gets it straight away, and ideas that make it into the game are credited in the changelog."),
			new("home", "home.host", "Be the quizmaster", "Here you make your own quizzes and host them. The host runs the night and doesn't play along. Let's have a look."),
			new("quizzes", "quizzes.bar", "Your quizzes", "Start a new quiz, import a quiz file, or paste a share code a friend sent you. Your quizzes are listed below: click one to open it."),
			new("overview", null, "A quiz, opened", "This is a sample quiz just for the tour (it isn't saved). Here you build the quiz: its name, its questions and their order."),
			new("overview", "overview.title", "Name your quiz", "Click the title to rename it. It's saved as soon as you leave the field."),
			new("overview", "overview.types", "Add a question", "Pick a type to add a question to the quiz. The ? on a card tells what the type does and which ways it can be played."),
			new("overview", "overview.list", "Your questions", "They're listed in the order they're played, each with its type and category. Click one to edit it; drag it by its dots, or type another number, to move it."),
			new("overview", "overview.list", "Why categories?", "Give every question a category: the stats per category (who knows geography best?) need them, and the lobby can show them to players as a hint. Questions without one say so in yellow."),
			new("overview", "overview.sort", "Sort the list", "\"Show\" sorts the list by type, by category or by when you made the questions. \"Play in this order\" makes the order you see the play order."),
			new("overview", "overview.foot", "Host it or share it", "When nothing is left to fix, Open lobby starts a quiz night. Copy share code and Export file give the quiz to a friend."),
			new("editor", "editor.issues", "Fill in the blanks", "Write the question and its answer. This line says what's still missing; you can save anyway and finish it later."),
			new("editor", "editor.round", "Round settings", "Points, time limit and how the question is scored. Hover over a setting to learn more about it."),
			new("editor", "editor.category", "Pick a category", "One of the suggestions, or your own. Players only see it if \"Show categories\" is on in the lobby (it makes questions easier); \"Keep it secret\" hides it even then. It always counts for the statistics."),
			new("editor", "editor.after", "Sources, fun facts and answer pictures", "Under the question: the answer as a big picture (the game a Steam review is about), a fun fact or a short story, and the source, all shown to everyone at the reveal. A source lets players check it themselves."),
			new("editor", "editor.preview", "Try it out", "Preview shows the question exactly as players will see it, and you can play it yourself. Nothing is saved or counted."),
			new("editor", "editor.save", "Save", "Save question keeps it and goes back to the quiz; Ctrl+S saves and stays on the question. \"← Back to quiz\" (or Esc) leaves without saving, and asks first if you changed something."),
			new("editor", null, "Shortcuts", "Ctrl+Z takes back a change, Ctrl+Y does it again. Ctrl+V pastes a picture (a screenshot, say), and you can drop picture, sound and video files onto the window. Esc goes back one screen, and so does your mouse's back button."),
			new("home", null, "That's it!", "Have fun creating and quizzing! Settings → \"Take the tour\" shows this again."),
		},
	};

	/// <summary>The tour's sample quiz (in memory only, never saved): the sample with categories, some left out on purpose.</summary>
	internal static Quiz TourQuiz()
	{
		var quiz = Dev.SampleQuiz.Build();
		quiz.Title = Lang.T("Sample quiz (tour)");
		var presets = new[] { "Space & astronomy", "Science", "Music", "Geography", "Games", "Geography", "Music", "Science", "Games", "Sport" };
		for (var i = 0; i < quiz.Questions.Count; i++)
			quiz.Questions[i].Category = i < presets.Length && i != 2 ? presets[i] : "";
		return quiz;
	}

	/// <summary>Starts the guided tour now, from the home screen (Settings' "Take the tour").</summary>
	public static void Start()
	{
		if (TutorialOverlay.Current != null) return;
		App.I.GoHome();
		var home = App.I.Screen!;
		home.GetTree().CreateTimer(0.3).Timeout += () => App.I.AddChild(new TutorialOverlay(Guided, home));
	}

	/// <summary>Marks what a tour step points at; returns the control.</summary>
	public static T Mark<T>(T control, string key) where T : Control
	{
		control.SetMeta(MetaKey, key);
		return control;
	}

	/// <summary>The visible control marked with the key (searched again each frame: forms rebuild themselves).</summary>
	public static Control? Find(Node root, string key)
	{
		foreach (var child in root.GetChildren())
		{
			if (child is Control c && !c.IsVisibleInTree()) continue;
			if (child is Control m && m.HasMeta(MetaKey) && m.GetMeta(MetaKey).AsString() == key) return m;
			if (Find(child, key) is { } found) return found;
		}
		return null;
	}

	static string[] DevArgs => OS.GetCmdlineUserArgs();

	/// <summary>The tour forced with --tutorial ID (dev).</summary>
	static string? Forced => Array.IndexOf(DevArgs, "--tutorial") is var i && i >= 0 && i + 1 < DevArgs.Length ? DevArgs[i + 1] : null;

	/// <summary>Whether the tour would show now: tutorials on, not seen yet, a window, and no dev arguments (bots, screenshots).</summary>
	public static bool Due(string id)
	{
		if (Forced == id) return true;
		if (DisplayServer.GetName() == "headless" || DevArgs.Length > 0) return false;
		return Settings.TutorialsOn && !Settings.TutorialsSeen.Contains(id);
	}

	/// <summary>A screen opened: its tour starts once the screen has its layout (only if still on that screen). when = the
	/// screen's own condition (the overview: a quiz without questions); --tutorial ID skips it.</summary>
	public static void Offer(string id, Control screen, bool when = true)
	{
		if (!(when || Forced == id) || !Due(id) || !Tours.ContainsKey(id)) return;
		Wait(id, screen);
	}

	/// <summary>Starts the tour once the screen has its layout, and once the intro (title screen, welcome) is over.</summary>
	static void Wait(string id, Control screen) => screen.GetTree().CreateTimer(0.5).Timeout += () =>
	{
		if (!GodotObject.IsInstanceValid(screen) || App.I.Screen != screen || TutorialOverlay.Current != null) return;
		if (App.I.GetChildren().OfType<IntroSequence>().Any())
		{
			Wait(id, screen);
			return;
		}
		App.I.AddChild(new TutorialOverlay(id, screen));
	};

	public static void MarkSeen(string id)
	{
		if (!Settings.TutorialsSeen.Contains(id)) Settings.TutorialsSeen = Settings.TutorialsSeen.Append(id).ToArray();
	}

	/// <summary>Settings' "Show tutorials again": every tour shows again, and tutorials are on.</summary>
	public static void Reset()
	{
		Settings.TutorialsSeen = Array.Empty<string>();
		Settings.TutorialsOn = true;
	}
}

/// <summary>A tour running: the dark screen with a hole around the step's thing, and the bubble.</summary>
public partial class TutorialOverlay : Control
{
	public static TutorialOverlay? Current { get; private set; }

	readonly string _id;
	/// <summary>The screen the tour is on (it goes to each step's screen itself; any other change of screen ends it).</summary>
	Control _screen;
	/// <summary>Which of the tour's screens that is (Step.Where).</summary>
	string _where = "home";
	/// <summary>The sample quiz the tour shows (in memory, never saved).</summary>
	Quiz? _quiz;
	readonly List<Tutorial.Step> _steps;
	int _at;
	readonly PanelContainer _bubble;
	readonly VBoxContainer _content = U.V(10);
	/// <summary>The hole (where the step's thing is, a little bigger), or null for a step without one.</summary>
	Rect2? _hole;
	float _time;
	const float Gap = 14, BubbleWidth = 420;

	public TutorialOverlay() : this(Tutorial.Guided, new Control()) { }

	public TutorialOverlay(string id, Control screen)
	{
		_id = id;
		_screen = screen;
		_steps = Tutorial.Tours[id].ToList();
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		_bubble = U.Panel(U.Pad(_content, 22, 18, 22, 18));
		_bubble.AddThemeStyleboxOverride("panel", UiTheme.Box(C.Panel, 14, C.Accent, 2, 0, 0));
		_bubble.CustomMinimumSize = new Vector2(BubbleWidth, 0);
		AddChild(_bubble);
	}

	public override void _Ready()
	{
		Current = this;
		Trace.Log("tutorial", _id);
		// (keys go to the tour, not to a field that had the focus)
		GetViewport().GuiReleaseFocus();
		// (dev: --tutorial-step N starts at that step, for screenshots)
		var args = OS.GetCmdlineUserArgs();
		var arg = Array.IndexOf(args, "--tutorial-step");
		Show(arg >= 0 && arg + 1 < args.Length && int.TryParse(args[arg + 1], out var n) ? Math.Clamp(n - 1, 0, _steps.Count - 1) : 0);
	}

	public override void _ExitTree()
	{
		if (Current == this) Current = null;
	}

	/// <summary>Opens the step's screen if the tour isn't on it yet (the sample quiz's overview or one of its questions).</summary>
	void Go(string where)
	{
		if (where == _where) return;
		_where = where;
		_quiz ??= Tutorial.TourQuiz();
		switch (where)
		{
			case "home": App.I.GoHome(); break;
			case "quizzes": App.I.GoQuizzes(); break;
			case "overview": App.I.Show(new QuizOverviewScreen(_quiz)); break;
			// (the question with everything for the reveal: answer picture and all)
			case "editor": App.I.Show(new QuestionEditorScreen(_quiz, _quiz.Questions.FirstOrDefault(q => q.AnswerImage != null) ?? _quiz.Questions[0], false)); break;
		}
		_screen = App.I.Screen!;
	}

	void Show(int at)
	{
		_at = at;
		var step = _steps[at];
		Go(step.Where);
		_content.Clear();
		_content.AddChild(U.Eyebrow(Lang.F("Tutorial · {0} of {1}", at + 1, _steps.Count)));
		_content.AddChild(U.Bold(step.Title, 19, C.Text, true));
		_content.AddChild(U.Muted(step.Text, 14));
		var last = at == _steps.Count - 1;
		var next = U.Btn(last ? "Done" : "Next ›", "AccentButton", () => Next(1), 15);
		var back = U.Small("‹ Back", () => Next(-1));
		back.Disabled = at == 0;
		var buttons = U.H(8, U.Link("Turn off tutorials", TurnOff, 13).Tip("Settings can turn them on again"), new Control().Grow(), back);
		if (!last) buttons.AddChild(U.Small("Skip", Close));
		buttons.AddChild(next);
		foreach (var b in buttons.GetChildren().OfType<Button>()) b.FocusMode = FocusModeEnum.None;
		_content.AddChild(U.Gap(2));
		_content.AddChild(buttons);
		// (the thing can be scrolled out of sight: bring it in)
		if (step.Target != null && Tutorial.Find(App.I, step.Target) is { } target)
			for (var p = target.GetParent(); p != null; p = p.GetParent())
				if (p is ScrollContainer scroll)
				{
					scroll.EnsureControlVisible(target);
					break;
				}
		_bubble.Modulate = new Color(1, 1, 1, 0);
		CreateTween().TweenProperty(_bubble, "modulate:a", 1f, 0.18);
		_bubble.Size = Vector2.Zero;
	}

	void Next(int by)
	{
		var to = _at + by;
		if (to >= _steps.Count) Close();
		else if (to >= 0) Show(to);
	}

	void Close()
	{
		Tutorial.MarkSeen(_id);
		QueueFree();
		// (never left on the sample quiz, which isn't saved: back home)
		if (_where is "overview" or "editor" && App.I.Screen == _screen) App.I.GoHome();
	}

	void TurnOff()
	{
		Settings.TutorialsOn = false;
		Close();
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		// (the screen changed under the tour, e.g. by a mouse button: the tour ends, seen)
		if (!IsInstanceValid(_screen) || App.I.Screen != _screen)
		{
			Close();
			return;
		}
		var target = _steps[_at].Target is { } key ? Tutorial.Find(App.I, key) : null;
		_hole = target?.GetGlobalRect().Grow(8);
		PlaceBubble();
		QueueRedraw();
	}

	/// <summary>The bubble below the hole if it fits, else above, else beside it; in the middle without one; always on screen.</summary>
	void PlaceBubble()
	{
		var size = _bubble.GetCombinedMinimumSize();
		_bubble.Size = size;
		var screen = Size;
		Vector2 at;
		if (_hole is not { } h) at = (screen - size) / 2;
		else if (h.End.Y + Gap + size.Y <= screen.Y - 8) at = new Vector2(h.GetCenter().X - size.X / 2, h.End.Y + Gap);
		else if (h.Position.Y - Gap - size.Y >= 8) at = new Vector2(h.GetCenter().X - size.X / 2, h.Position.Y - Gap - size.Y);
		else if (h.End.X + Gap + size.X <= screen.X - 8) at = new Vector2(h.End.X + Gap, h.GetCenter().Y - size.Y / 2);
		else if (h.Position.X - Gap - size.X >= 8) at = new Vector2(h.Position.X - Gap - size.X, h.GetCenter().Y - size.Y / 2);
		// (the thing fills the screen: the bubble sits in its bottom right)
		else at = screen - size - new Vector2(24, 24);
		_bubble.Position = new Vector2(Mathf.Clamp(at.X, 8, Mathf.Max(8, screen.X - size.X - 8)), Mathf.Clamp(at.Y, 8, Mathf.Max(8, screen.Y - size.Y - 8)));
	}

	public override void _Draw()
	{
		var dim = new Color(0, 0, 0, 0.62f);
		var all = new Rect2(Vector2.Zero, Size);
		if (_hole is not { } h)
		{
			DrawRect(all, dim);
			return;
		}
		h = h.Intersection(all);
		// (four dark bands around the hole)
		DrawRect(new Rect2(0, 0, Size.X, h.Position.Y), dim);
		DrawRect(new Rect2(0, h.End.Y, Size.X, Size.Y - h.End.Y), dim);
		DrawRect(new Rect2(0, h.Position.Y, h.Position.X, h.Size.Y), dim);
		DrawRect(new Rect2(h.End.X, h.Position.Y, Size.X - h.End.X, h.Size.Y), dim);
		// (a glowing frame that breathes)
		var glow = C.Accent with { A = 0.55f + 0.45f * Mathf.Sin(_time * 4) };
		var frame = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = glow, DrawCenter = false };
		frame.SetBorderWidthAll(3);
		frame.SetCornerRadiusAll(10);
		DrawStyleBox(frame, h);
	}

	public override void _Input(InputEvent e)
	{
		if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
		switch (key.Keycode)
		{
			case Key.Escape: Close(); break;
			case Key.Right or Key.Enter or Key.KpEnter or Key.Space: Next(1); break;
			case Key.Left: Next(-1); break;
			default: return;
		}
		GetViewport().SetInputAsHandled();
	}
}
