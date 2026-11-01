// The end of a quiz night. The final standings are taken once, from the first finished state (Shared.FinalResults),
// so nobody drops off it by leaving. It plays like a show: the podium comes in big, 3rd, 2nd, then 1st; it shrinks and
// everyone else's places and tonight's awards come in below it; then the credits roll (the quizmaster, everyone who
// played, the awards). Each podium is kept on this PC (Storage.Podiums): the match history shows it again.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

public partial class PodiumScreen : Control, IRoomScreen
{
	readonly bool _isHost;
	readonly string? _myId;
	readonly Action? _backToLobby;
	/// <summary>A kept podium from the match history (no room behind it).</summary>
	readonly FinalResults? _replay;
	readonly VBoxContainer _content = U.V(14);
	FinalResults? _results;

	ScaledBox? _podium;
	Control? _after, _buttons;
	CreditsRoll? _credits;
	Tween? _show;
	Label? _saved;

	/// <summary>When the podium is shown big, how long until it shrinks, and how long after that the credits start.</summary>
	const float BigScale = 1.3f, SmallScale = 0.8f, ShrinkAt = 4.6f, CreditsAfter = 2.5f;

	public PodiumScreen() : this(false, null, null) { }

	// (the podium song stops with the screen: back to the lobby, home, or the game closing)
	public override void _ExitTree() => Sfx.StopPodium();

	public PodiumScreen(bool isHost, string? myId, Action? backToLobby)
	{
		_isHost = isHost;
		_myId = myId;
		_backToLobby = backToLobby;
		SetAnchorsPreset(LayoutPreset.FullRect);
		_content.Alignment = BoxContainer.AlignmentMode.Center;
		AddChild(U.Page(_content, 860, 40));
	}

	/// <summary>A kept podium, played again (from the match history).</summary>
	public PodiumScreen(FinalResults saved) : this(false, saved.Me, null)
	{
		_replay = saved;
		Ready += () => Build(saved);
	}

	/// <summary>
	/// Escape: first it skips the show (the credits, or the podium coming in); then the host goes back to the lobby, a
	/// player leaves, a kept podium goes back to the match history.
	/// </summary>
	public void GoBack()
	{
		if (Skip()) return;
		if (_replay != null) App.I.GoMatchHistory();
		else if (_isHost) _backToLobby?.Invoke();
		else App.I.LeaveRoom();
	}

	public void Refresh(RoomState room)
	{
		// (only the first time: later states can be missing whoever left)
		if (_results != null) return;
		var results = FinalResults.From(room);
		results.Me = _myId;
		// (dev runs with arguments are tests: they don't fill the match history)
		if (OS.GetCmdlineUserArgs().Length == 0) Podiums.Save(results);
		Build(results);
	}

	void Build(FinalResults r)
	{
		_results = r;
		Sfx.PlayRoomPodium(r.PodiumSound, _isHost);
		_content.Clear();
		var top = r.Players.Where(p => p.Place <= 3).ToList();
		var rest = r.Players.Where(p => p.Place > 3).ToList();
		var me = _myId != null ? r.Player(_myId) : null;

		// (everything has a sponsor's name, like in American sports)
		_content.AddChild(U.H(10, U.Eyebrow("The"), new TrackGlowLogo(0.55f) { SizeFlagsVertical = SizeFlags.ShrinkCenter }, U.Eyebrow("Podium")).Center());
		_content.AddChild(U.Eyebrow(Lang.F("{0} · final results", r.QuizTitle)).Center());
		_content.AddChild(U.H(0, U.Heading(top.Count(p => p.Place == 1) > 1 ? "It's a tie at the top" : "And the winner is", 44, false), U.Text("…", 44, C.Accent, Fonts.Black)).Center());

		// Classic podium order: 2nd, 1st, 3rd. Each step comes in on its own (3rd first), so they start hidden.
		var row = U.H(14);
		row.Alignment = BoxContainer.AlignmentMode.End;
		var steps = new List<(int Place, Control Step, Control Block)>();
		var tags = U.AnyClanTag(top.Select(t => t.Name));
		foreach (var place in new[] { 2, 1, 3 })
		foreach (var p in top.Where(t => t.Place == place))
		{
			var (step, block) = Step(p, r, tags);
			row.AddChild(step);
			steps.Add((place, step, block));
		}
		_podium = new ScaledBox { Zoom = Math.Min(BigScale, 820f / Math.Max(1, steps.Count * 184)) };
		_podium.AddChild(row);
		_content.AddChild(_podium);

		// Below it once it's small: you, everyone else, the awards, the buttons.
		var after = U.V(14);
		if (me != null) after.AddChild(U.H(4, U.Muted("You finished", 16, false), U.Text(Lang.Ordinal(me.Place), 16, C.Accent, Fonts.Bold).Raw(), U.Muted(Lang.F("with {0} points.", me.Score), 16, false)).Center());
		if (rest.Count > 0) after.AddChild(Standings(rest, r));
		if (r.Awards.Count > 0) after.AddChild(AwardsCard(r));
		after.AddChild(U.Gap(4));
		after.AddChild(_buttons = Buttons());
		_after = after;
		_content.AddChild(after);

		// The show: hidden steps and standings, then 3rd, 2nd, 1st, the shrink, the rest, and the credits.
		foreach (var s in steps) s.Step.Modulate = Colors.Transparent;
		after.Modulate = Colors.Transparent;
		after.MouseFilter = MouseFilterEnum.Ignore;
		_show = CreateTween();
		var at = 0.5f;
		foreach (var place in new[] { 3, 2, 1 })
		{
			var these = steps.Where(s => s.Place == place).ToList();
			if (these.Count == 0) continue;
			_show.TweenInterval(at);
			for (var i = 0; i < these.Count; i++) Rise(_show, these[i].Step, these[i].Block, place == 1, i == 0);
			at = place == 1 ? 0 : 1.0f;
		}
		_show.TweenInterval(Math.Max(0.5f, ShrinkAt - 0.5f - 2.0f));
		_show.TweenMethod(Callable.From<float>(z => _podium.Zoom = z), _podium.Zoom, SmallScale, 0.7f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
		_show.TweenProperty(after, "modulate", Colors.White, 0.5f);
		_show.TweenCallback(Callable.From(() => after.MouseFilter = MouseFilterEnum.Pass));
		_show.TweenInterval(CreditsAfter);
		_show.TweenCallback(Callable.From(RollCredits));
	}

	/// <summary>
	/// A step comes in: the player fades in and the block grows up out of the floor. first: the first of its place
	/// (after the wait before it; players sharing the place come in with it).
	/// </summary>
	void Rise(Tween t, Control step, Control block, bool winner, bool first)
	{
		block.PivotOffset = new Vector2(0, block.CustomMinimumSize.Y);
		block.Scale = new Vector2(1, 0.05f);
		(first ? t : t.Parallel()).TweenProperty(step, "modulate", Colors.White, 0.35f);
		t.Parallel().TweenProperty(block, "scale", Vector2.One, 0.55f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		if (winner) t.Parallel().TweenCallback(Callable.From(() => AddChild(new Confetti())));
	}

	static (Control Step, Control Block) Step(FinalPlayer p, FinalResults r, bool tags)
	{
		var height = p.Place == 1 ? 190 : p.Place == 2 ? 140 : 100;
		var block = U.Panel(U.Text(Lang.Ordinal(p.Place), 44, C.OnBright, Fonts.Black, false, HorizontalAlignment.Center).Raw(), "Clear")
			.With(x =>
			{
				var box = UiTheme.Box(C.Medal(p.Place) ?? C.Accent, 0, null, 0, 0, 10);
				box.CornerRadiusTopLeft = box.CornerRadiusTopRight = 14;
				x.AddThemeStyleboxOverride("panel", box);
			}).MinSize(170, height);
		// (a wide name is cut off with "…" instead of widening its step)
		var name = U.PlayerName(p.Name, 18, HorizontalAlignment.Center, tags);
		var step = U.V(6, new Avatar(p.ToPublic(), 64).Center(), name, U.Mono(Lang.F("{0} pts", p.Score), 15, C.Muted).Center());
		if (SeatNote(p, r) is { } note) step.AddChild(note.Center());
		step.AddChild(block);
		step.Alignment = BoxContainer.AlignmentMode.End;
		return (step, block);
	}

	/// <summary>"left at Q 7/20" / "joined at Q 3/20", or null.</summary>
	static Label? SeatNote(FinalPlayer p, FinalResults r) =>
		p.LeftAt is { } left ? U.Text(Lang.F("left at Q {0}/{1}", left, r.Questions), 12, C.Coral)
		: p.JoinedAt is { } joined ? U.Muted(Lang.F("joined at Q {0}/{1}", joined, r.Questions), 12, false)
		: null;

	/// <summary>4th place on: "4th  Ann · 12" chips.</summary>
	Control Standings(List<FinalPlayer> rest, FinalResults r)
	{
		var chips = U.Flow(8);
		chips.Alignment = FlowContainer.AlignmentMode.Center;
		foreach (var p in rest)
		{
			var chip = Chips.Player(p.ToPublic(), $"· {p.Score}", p.Id == _myId);
			var inside = (HBoxContainer)chip.GetChild(0);
			var place = U.Mono(Lang.Ordinal(p.Place), 13, C.Muted).Raw();
			inside.AddChild(place);
			inside.MoveChild(place, 0);
			if (SeatNote(p, r) is { } note) inside.AddChild(note);
			chips.AddChild(chip);
		}
		return chips;
	}

	/// <summary>Tonight's awards as tiles: what it's for, who (with their picture), their number.</summary>
	Control AwardsCard(FinalResults r)
	{
		var tiles = U.Flow(10);
		tiles.Alignment = FlowContainer.AlignmentMode.Center;
		foreach (var a in r.Awards)
		{
			if (NightAwards.Of(a.Kind) is not { } info) continue;
			var who = U.V(4);
			foreach (var id in a.Players)
				if (r.Player(id) is { } p)
					who.AddChild(GameParts.Row(6, new Avatar(p.ToPublic(), 22), U.Text(p.Name, 15, p.Id == _myId ? C.Teal : C.Text, Fonts.Bold).Raw().Clipped(60)));
			var tile = U.Card(6, U.Text(Lang.T(info.Title).ToUpper(), 12, C.Accent, Fonts.Spaced), who, U.Muted(Lang.F(info.Line, info.Format(a.Value)), 13, false));
			tile.CustomMinimumSize = new Vector2(250, 0);
			tiles.AddChild(tile);
		}
		return U.V(10, U.Eyebrow("Tonight's awards").Center(), tiles);
	}

	Control Buttons()
	{
		var credits = U.Btn("▶ Credits", "", RollCredits).Tip("The credits again: who hosted, who played, tonight's awards");
		var picture = U.Btn("📷 Save picture", "", SavePicture).Tip("Saves the podium as a picture in Documents/Quiznight/Pictures");
		_saved = U.Muted("", 13, false);
		_saved.Visible = false;
		Control main = _replay != null ? U.Btn("← Match history", "AccentButton", App.I.GoMatchHistory)
			: _isHost ? U.Btn("Back to lobby — play again", "AccentButton", _backToLobby)
			: U.H(16, U.Muted("Thanks for playing! The host can start another round.", 14, false).With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter), U.Link("Leave", App.I.LeaveRoom));
		return U.V(10, main.Center(), U.H(10, credits, picture).Center(), _saved.Center());
	}

	/// <summary>Jumps to the end of what's playing: the credits close, or the podium is shown small with everything. True if it did.</summary>
	bool Skip()
	{
		if (_credits is { } c && IsInstanceValid(c) && !c.IsQueuedForDeletion())
		{
			c.Close();
			_credits = null;
			return true;
		}
		if (_show is not { } show || !show.IsValid() || !show.IsRunning()) return false;
		show.Kill();
		_show = null;
		foreach (var step in _podium!.GetChild<Control>(0).GetChildren().OfType<Control>())
		{
			step.Modulate = Colors.White;
			foreach (var block in step.GetChildren().OfType<Control>()) block.Scale = Vector2.One;
		}
		_podium.Zoom = SmallScale;
		_after!.Modulate = Colors.White;
		_after.MouseFilter = MouseFilterEnum.Pass;
		return true;
	}

	public override void _Input(InputEvent e)
	{
		// (a click while the podium comes in shows it all at once; the still hidden buttons don't get it)
		if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || _show is not { } s || !s.IsValid() || !s.IsRunning()) return;
		if (_credits != null && IsInstanceValid(_credits)) return;
		Skip();
		GetViewport().SetInputAsHandled();
	}

	void RollCredits()
	{
		if (_results == null || (_credits != null && IsInstanceValid(_credits))) return;
		_credits = new CreditsRoll(_results, _myId, () => _credits = null);
		AddChild(_credits);
	}

	/// <summary>The screen as a picture (without its buttons), in Documents/Quiznight/Pictures.</summary>
	async void SavePicture()
	{
		if (_results == null || _buttons == null) return;
		_buttons.Modulate = Colors.Transparent;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		var image = GetViewport().GetTexture().GetImage();
		_buttons.Modulate = Colors.White;
		var safe = new string(_results.QuizTitle.Select(c => char.IsLetterOrDigit(c) ? c : '-').Take(40).ToArray()).Trim('-');
		var path = System.IO.Path.Combine(SeasonPoster.Folder, $"Podium {(safe == "" ? "quiz" : safe)} {_results.Date:yyyy-MM-dd HH-mm}.png");
		try
		{
			Directory.CreateDirectory(SeasonPoster.Folder);
			var ok = image.SavePng(path) == Error.Ok;
			_saved!.Text = ok ? Lang.F("Saved in {0}.", "Documents/Quiznight/Pictures") : Lang.T("The picture couldn't be saved.");
			if (ok) OS.ShellOpen(SeasonPoster.Folder);
		}
		catch (Exception)
		{
			_saved!.Text = Lang.T("The picture couldn't be saved.");
		}
		_saved.Visible = true;
	}
}

/// <summary>A container for one child, drawn at a zoom: its room in the layout grows and shrinks with it (centred).</summary>
public partial class ScaledBox : Container
{
	float _zoom = 1;

	public float Zoom
	{
		get => _zoom;
		set
		{
			_zoom = value;
			UpdateMinimumSize();
			QueueSort();
		}
	}

	Control? Child => GetChildCount() > 0 ? GetChild<Control>(0) : null;

	public override Vector2 _GetMinimumSize() => Child is { } c ? c.GetCombinedMinimumSize() * _zoom : Vector2.Zero;

	public override void _Notification(int what)
	{
		if (what != NotificationSortChildren || Child is not { } c) return;
		var size = c.GetCombinedMinimumSize();
		c.Size = size;
		c.Scale = Vector2.One * _zoom;
		c.Position = new Vector2((Size.X - size.X * _zoom) / 2, 0);
	}
}

/// <summary>
/// The credits over the podium, rolling up like at the end of a party game: the quiz, the quizmaster, everyone who
/// played, tonight's awards, and thanks. Skip (or Escape) closes them; they close by themselves at the end.
/// </summary>
public partial class CreditsRoll : Control
{
	const float Speed = 70;
	readonly VBoxContainer _roll = U.V(10);
	readonly Action _closed;
	bool _closing;

	public CreditsRoll() : this(new FinalResults(), null, () => { }) { }

	public CreditsRoll(FinalResults r, string? me, Action closed)
	{
		_closed = closed;
		SetAnchorsPreset(LayoutPreset.FullRect);
		ClipContents = true;
		MouseFilter = MouseFilterEnum.Stop;
		var dim = new ColorRect { Color = new Color(C.Bg, 0.96f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		_roll.Alignment = BoxContainer.AlignmentMode.Center;
		_roll.AddChild(new TrackGlowLogo(0.9f).Center());
		_roll.AddChild(U.Gap(10));
		_roll.AddChild(Centered(U.Heading(r.QuizTitle, 36).Raw()));
		_roll.AddChild(Centered(U.Muted(r.Date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture), 15, false)));

		Section("Quizmaster");
		_roll.AddChild(Centered(U.Text(r.Host != "" ? r.Host : Lang.T("The host"), 30, C.Text, Fonts.Black).With(l => { if (r.Host != "") l.Raw(); })));

		Section("Starring");
		foreach (var p in r.Players)
			_roll.AddChild(GameParts.Row(10, new Avatar(p.ToPublic(), 30), U.Text(p.Name, 24, p.Id == me ? C.Teal : C.Text, Fonts.Bold).Raw(),
				U.Mono(Lang.Ordinal(p.Place), 15, C.Medal(p.Place) ?? C.Muted).Raw()).Center());

		if (r.Awards.Count > 0)
		{
			Section("Tonight's awards");
			foreach (var a in r.Awards)
			{
				if (NightAwards.Of(a.Kind) is not { } info) continue;
				_roll.AddChild(Centered(U.Text(Lang.T(info.Title).ToUpper(), 14, C.Accent, Fonts.Spaced)));
				_roll.AddChild(Centered(U.Text(string.Join(" & ", a.Players.Select(id => r.Player(id)?.Name ?? "?")), 24, C.Text, Fonts.Bold).Raw()));
				_roll.AddChild(Centered(U.Muted(Lang.F(info.Line, info.Format(a.Value)), 14, false)));
				_roll.AddChild(U.Gap(14));
			}
		}

		Section("Played with");
		_roll.AddChild(Centered(U.Text("Quiznight?", 24, C.Text, Fonts.Black).Raw()));
		_roll.AddChild(U.Gap(80));
		_roll.AddChild(Centered(U.Heading("Thanks for playing!", 40, false)));
		_roll.AddChild(Centered(U.Muted("See you at the next quiz night.", 16, false)));
		AddChild(_roll);

		var skip = U.Btn("Skip ▸", "", Close, 14);
		skip.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		skip.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		var corner = U.Pad(skip, 0, 0, 24, 24);
		corner.SetAnchorsPreset(LayoutPreset.FullRect);
		corner.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(corner);

		Modulate = Colors.Transparent;
		Ready += () => CreateTween().TweenProperty(this, "modulate", Colors.White, 0.5f);
	}

	/// <summary>Placed below the screen on the first frame (when it's ready, its own size isn't known yet).</summary>
	bool _placed;

	static Label Centered(Label l)
	{
		l.HorizontalAlignment = HorizontalAlignment.Center;
		return l;
	}

	void Section(string title)
	{
		_roll.AddChild(U.Gap(46));
		_roll.AddChild(Centered(U.Text(Lang.T(title).ToUpper(), 13, C.Muted, Fonts.Spaced)));
		_roll.AddChild(U.Gap(2));
	}

	/// <summary>How long "Thanks for playing!" stays in the middle before the credits close.</summary>
	const float HoldSec = 3;
	float _held;

	public override void _Process(double delta)
	{
		_roll.Size = new Vector2(Size.X, _roll.GetCombinedMinimumSize().Y);
		if (!_placed)
		{
			_placed = true;
			_roll.Position = new Vector2(0, Size.Y);
			return;
		}
		// (the end stops a little below the middle, then they close)
		var stop = Size.Y * 0.62f - _roll.Size.Y;
		if (_roll.Position.Y > stop) _roll.Position = new Vector2(0, Math.Max(stop, _roll.Position.Y - Speed * (float)delta));
		else if ((_held += (float)delta) >= HoldSec) Close();
	}

	public void Close()
	{
		if (_closing) return;
		_closing = true;
		_closed();
		var t = CreateTween();
		t.TweenProperty(this, "modulate", Colors.Transparent, 0.6f);
		t.TweenCallback(Callable.From(QueueFree));
	}
}
