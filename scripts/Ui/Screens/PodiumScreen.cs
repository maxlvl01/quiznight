// The end of a quiz night. The final standings are taken once, from the first finished state (Shared.FinalResults),
// so nobody drops off it by leaving. It plays like a show: the podium comes in big in the middle, 3rd, 2nd, then 1st,
// with everyone's score list under it; then podium and list move into the right third of the screen, and the credits
// roll on the left two thirds (the quizmaster, everyone who played, tonight's awards), which end on the awards. Each
// podium is kept on this PC (Storage.Podiums), and the night's award pack is drawn in the background (NightPack).

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
	/// <summary>A kept podium played again (match history, Test lab): where its back button goes.</summary>
	readonly (string Label, Action Go)? _replayBack;
	FinalResults? _results;

	/// <summary>Header, podium and score list: one picture that zooms as it moves into the right third.</summary>
	readonly ScaledBox _side = new() { CenterV = true };
	/// <summary>The left two thirds: the credits, then the awards, with the buttons under them.</summary>
	readonly Control _left = new() { MouseFilter = MouseFilterEnum.Ignore };
	readonly Control _stage = new() { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ExpandFill };
	Control? _awards, _bar, _list;
	CreditsRoll? _credits;
	/// <summary>True once the credits have rolled to their end (or were skipped); the dev runs wait for it.</summary>
	public bool CreditsDone { get; private set; }
	Button? _packButton;
	Label? _note;
	Tween? _show;
	/// <summary>0: everything in the middle; 1: podium and list in the right third.</summary>
	float _t;

	public PodiumScreen() : this(false, null, null) { }

	// (the podium song stops with the screen: back to the lobby, home, or the game closing)
	public override void _ExitTree() => Sfx.StopPodium();

	public PodiumScreen(bool isHost, string? myId, Action? backToLobby)
	{
		_isHost = isHost;
		_myId = myId;
		_backToLobby = backToLobby;
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_side);
		AddChild(_left);
		Resized += Layout;
	}

	/// <summary>A kept podium, played again.</summary>
	public PodiumScreen(FinalResults saved, string backLabel, Action back) : this(false, saved.Me, null)
	{
		_replayBack = (backLabel, back);
		Ready += () => Build(saved);
	}

	/// <summary>
	/// Escape: first it skips the show (the podium coming in, then the credits); then the host goes back to the lobby, a
	/// player leaves, a kept podium goes back where it came from.
	/// </summary>
	public void GoBack()
	{
		if (Skip()) return;
		if (_replayBack is { } back) back.Go();
		else if (_isHost) _backToLobby?.Invoke();
		else App.I.LeaveRoom();
	}

	public void Refresh(RoomState room)
	{
		// (only the first time: later states can be missing whoever left)
		if (_results != null) return;
		var results = FinalResults.From(room);
		results.Me = _myId;
		// (dev runs with arguments are tests: they don't fill the match history or Pictures)
		if (OS.GetCmdlineUserArgs().Length == 0)
		{
			Podiums.Save(results);
			NightPack.Make(results);
		}
		Build(results);
	}

	void Build(FinalResults r)
	{
		_results = r;
		Sfx.PlayRoomPodium(r.PodiumSound, _isHost);
		var top = r.Ranked.Where(p => p.Place <= 3).ToList();
		// (team night: your team's place)
		var me = _myId != null ? r.Entry(_myId) : null;

		// ---- the right side: header, podium, score list ----
		var side = U.V(12);
		// (everything has a sponsor's name, like in American sports)
		side.AddChild(U.H(12, Head("The", 20), new TrackGlowLogo(0.7f) { SizeFlagsVertical = SizeFlags.ShrinkCenter }, Head("Podium", 20)).Center());
		side.AddChild(Head(Lang.F("{0} · final results", r.QuizTitle), 16).Raw().Center());
		side.AddChild(U.H(0, U.Heading(top.Count(p => p.Place == 1) > 1 ? "It's a tie at the top" : "And the winner is", 40, false), U.Text("…", 40, C.Accent, Fonts.Black)).Center());
		side.AddChild(U.Gap(6));

		// Classic podium order: 2nd, 1st, 3rd. Each step comes in on its own (3rd first), so they start hidden.
		var row = U.H(14);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		var steps = new List<(int Place, Control Step, Control Block)>();
		var tags = U.AnyClanTag(top.Select(t => t.Name));
		foreach (var place in new[] { 2, 1, 3 })
		foreach (var p in top.Where(t => t.Place == place))
		{
			var (step, block) = Step(p, r, tags);
			row.AddChild(step);
			steps.Add((place, step, block));
		}
		side.AddChild(row);

		var list = U.V(10);
		if (me != null) list.AddChild(U.H(4, U.Muted("You finished", 16, false), U.Text(Lang.Ordinal(me.Place), 16, C.Accent, Fonts.Bold).Raw(), U.Muted(Lang.F("with {0} points.", me.Score), 16, false)).Center());
		list.AddChild(Standings(r));
		side.AddChild(_list = list);
		foreach (var child in _side.GetChildren()) child.QueueFree();
		_side.AddChild(side);

		// ---- the left side: the credits' and awards' stage, the buttons under it ----
		foreach (var child in _left.GetChildren()) child.QueueFree();
		_awards = AwardsPanel(r);
		_awards.Visible = false;
		_stage.AddChild(_awards);
		_bar = Buttons();
		var column = U.V(14, _stage, _bar);
		// (clear of the bug and music buttons in the bottom left corner)
		var margin = U.Pad(column, 84, 28, 24, 24);
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.MouseFilter = MouseFilterEnum.Ignore;
		_left.AddChild(margin);

		// The show: 3rd, 2nd, 1st, the list, a moment to take it in, the move to the right, the credits.
		foreach (var s in steps) s.Step.Modulate = Colors.Transparent;
		list.Modulate = Colors.Transparent;
		_t = 0;
		Layout();
		_show = CreateTween();
		var wait = 0.5f;
		foreach (var place in new[] { 3, 2, 1 })
		{
			var these = steps.Where(s => s.Place == place).ToList();
			if (these.Count == 0) continue;
			_show.TweenInterval(wait);
			for (var i = 0; i < these.Count; i++) Rise(_show, these[i].Step, these[i].Block, place == 1, i == 0);
			wait = 1.0f;
		}
		_show.TweenInterval(0.7f);
		_show.TweenProperty(list, "modulate", Colors.White, 0.5f);
		_show.TweenInterval(3.2f);
		_show.TweenMethod(Callable.From<float>(t =>
		{
			_t = t;
			Layout();
		}), 0f, 1f, 0.9f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
		_show.TweenCallback(Callable.From(RollCredits));
	}

	/// <summary>Where everything goes for the move (_t): the side from the whole width to the right third, zoomed to fit.</summary>
	void Layout()
	{
		if (_side.GetChildCount() == 0 || _side.GetChild(0) is not Control content) return;
		var size = Size;
		var third = size.X / 3;
		var nat = content.GetCombinedMinimumSize();
		if (nat.X <= 0 || nat.Y <= 0) return;
		var big = Mathf.Min(1.1f, Mathf.Min((size.X - 80) / nat.X, (size.Y - 40) / nat.Y));
		var small = Mathf.Min(0.9f, Mathf.Min((third - 40) / nat.X, (size.Y - 40) / nat.Y));
		_side.Zoom = Mathf.Lerp(big, small, _t);
		_side.Position = new Vector2(Mathf.Lerp(0, size.X - third - 8, _t), 0);
		_side.Size = new Vector2(Mathf.Lerp(size.X, third, _t), size.Y);
		_left.Position = Vector2.Zero;
		_left.Size = new Vector2(size.X - third, size.Y);
		_left.Modulate = new Color(1, 1, 1, _t);
		_left.Visible = _t > 0.01f;
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
		var height = p.Place == 1 ? 170 : p.Place == 2 ? 125 : 90;
		var block = U.Panel(U.Text(Lang.Ordinal(p.Place), 40, C.OnBright, Fonts.Black, false, HorizontalAlignment.Center).Raw(), "Clear")
			.With(x =>
			{
				var box = UiTheme.Box(C.Medal(p.Place) ?? C.Accent, 0, null, 0, 0, 10);
				box.CornerRadiusTopLeft = box.CornerRadiusTopRight = 14;
				x.AddThemeStyleboxOverride("panel", box);
			}).MinSize(170, height);
		// (a wide name is cut off with "…" instead of widening its step)
		var name = U.PlayerName(p.Name, 18, HorizontalAlignment.Center, tags);
		var step = U.V(6, new Avatar(p.ToPublic(), 60).Center(), name, U.Mono(Lang.F("{0} pts", p.Score), 15, C.Muted).Center());
		if (p.Members is { } members) step.AddChild(U.Muted(string.Join(", ", members), 13, true, HorizontalAlignment.Center).Raw().MinSize(170, 0));
		if (SeatNote(p, r) is { } note) step.AddChild(note.Center());
		step.AddChild(block);
		step.Alignment = BoxContainer.AlignmentMode.End;
		return (step, block);
	}

	/// <summary>
	/// The heading lines over the podium: spaced capitals like an eyebrow, but heavier and at their real size (the eyebrow's
	/// thin 12 px letters looked scuffed once the podium was zoomed up: fonts aren't redrawn sharper when scaled).
	/// </summary>
	static Label Head(string text, int size) =>
		U.Text(Lang.T(text).ToUpper(), size, new Color(C.Text, 0.75f), _head ??= new FontVariation { BaseFont = Fonts.Black, SpacingGlyph = 3 });

	static FontVariation? _head;

	/// <summary>"left at Q 7/20" / "joined at Q 3/20", or null.</summary>
	static Label? SeatNote(FinalPlayer p, FinalResults r) =>
		p.LeftAt is { } left ? U.Text(Lang.F("left at Q {0}/{1}", left, r.Questions), 12, C.Coral)
		: p.JoinedAt is { } joined ? U.Muted(Lang.F("joined at Q {0}/{1}", joined, r.Questions), 12, false)
		: null;

	/// <summary>Everyone's place, picture, name and points, in a card.</summary>
	Control Standings(FinalResults r)
	{
		var rows = U.V(4);
		foreach (var p in r.Ranked)
		{
			var mine = p.Id == _myId || p.MemberIds?.Contains(_myId ?? "") == true;
			var place = U.Mono(Lang.Ordinal(p.Place), 15, C.Medal(p.Place) ?? C.Muted).Raw().MinSize(44);
			var name = U.Text(p.Name, 17, mine ? C.Teal : C.Text, p.Place <= 3 || mine ? Fonts.Bold : null).Raw().Clipped(80);
			var row = GameParts.Row(10, place, new Avatar(p.ToPublic(), 28), name);
			if (SeatNote(p, r) is { } note) row.AddChild(note);
			row.AddChild(U.Mono($"{p.Score}", 18, p.Place == 1 ? C.Gold : C.Accent).MinSize(48).With(l => { l.HorizontalAlignment = HorizontalAlignment.Right; l.SizeFlagsVertical = SizeFlags.ShrinkCenter; }));
			rows.AddChild(row);
		}
		return U.Card(8, rows).MinSize(560, 0);
	}

	/// <summary>Tonight's awards as tiles (what it's for, who, their number), or thanks when nobody won one.</summary>
	Control AwardsPanel(FinalResults r)
	{
		var body = U.V(14, U.Heading(r.Awards.Count > 0 ? "Tonight's awards" : "Thanks for playing!", 30),
			U.Muted(Lang.F("{0} · {1}", r.QuizTitle, r.Date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)), 14, false).Raw());
		var tiles = U.Flow(12);
		foreach (var a in r.Awards)
		{
			if (NightAwards.Of(a.Kind) is not { } info) continue;
			var who = U.V(4);
			foreach (var id in a.Players)
				if (r.Player(id) is { } p)
					who.AddChild(GameParts.Row(6, new Avatar(p.ToPublic(), 24), U.Text(p.Name, 16, p.Id == _myId ? C.Teal : C.Text, Fonts.Bold).Raw().Clipped(60)));
			var tile = U.Card(6, U.Text(Lang.T(info.Title).ToUpper(), 12, C.Accent, Fonts.Spaced), who, U.Muted(Lang.F(info.Line, info.Format(a.Value)), 13, false));
			tile.CustomMinimumSize = new Vector2(228, 0);
			tiles.AddChild(tile);
		}
		body.AddChild(tiles);
		var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		scroll.SetAnchorsPreset(LayoutPreset.FullRect);
		body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(body);
		return scroll;
	}

	Control Buttons()
	{
		var credits = U.Btn("▶ Credits", "", RollCredits).Tip("The credits again: who hosted, who played, tonight's awards");
		var picture = U.Btn("Save picture", "", SavePicture).On(Glyph.Kind.Camera, 18).Tip("Saves the podium as a picture in Documents/Quiznight/Pictures");
		_packButton = U.Btn("Award pack", "", PackPressed).On(Glyph.Kind.Folder, 18).Tip("Pictures of tonight to share: the standings and a card for every award, in one ZIP");
		_note = U.Muted("", 13, false);
		Control main = _replayBack is { } back ? U.Btn(back.Label, "AccentButton", back.Go)
			: _isHost ? U.Btn("Back to lobby — play again", "AccentButton", _backToLobby)
			: U.Btn("Leave", "AccentButton", App.I.LeaveRoom).Tip("Thanks for playing! The host can start another round.");
		var row = U.Flow(10, main, credits, picture, _packButton);
		UpdatePack();
		return U.V(6, row, _note);
	}

	/// <summary>The award pack's button: open it, wait while it's drawn, or (a kept podium without one) make it.</summary>
	void UpdatePack()
	{
		if (_packButton == null || _results == null) return;
		var making = NightPack.Making(_results);
		_packButton.Disabled = making;
		_packButton.Text = Lang.T(making ? "Drawing the award pack…" : NightPack.Exists(_results) ? "Award pack" : "Make award pack");
	}

	double _packCheck;

	public override void _Process(double delta)
	{
		if ((_packCheck += delta) < 0.5) return;
		_packCheck = 0;
		UpdatePack();
	}

	void PackPressed()
	{
		if (_results == null) return;
		if (NightPack.Exists(_results)) NightPack.Open(_results);
		else NightPack.Make(_results);
		UpdatePack();
	}

	/// <summary>Jumps ahead: the podium's show to its end (and the credits start), or the credits to the awards. True if it did.</summary>
	bool Skip()
	{
		if (_credits is { } c && IsInstanceValid(c) && !c.IsQueuedForDeletion())
		{
			c.Close();
			return true;
		}
		if (_show is not { } show || !show.IsValid() || !show.IsRunning()) return false;
		show.Kill();
		_show = null;
		if (_side.GetChild(0) is Control content)
			foreach (var step in content.FindChildren("*", "VBoxContainer", true, false).OfType<Control>())
			{
				step.Modulate = Colors.White;
				foreach (var block in step.GetChildren().OfType<PanelContainer>()) block.Scale = Vector2.One;
			}
		_list!.Modulate = Colors.White;
		_t = 1;
		Layout();
		RollCredits();
		return true;
	}

	public override void _Input(InputEvent e)
	{
		// (a click while the podium comes in jumps to the credits)
		if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || _show is not { } s || !s.IsValid() || !s.IsRunning()) return;
		Skip();
		GetViewport().SetInputAsHandled();
	}

	/// <summary>The credits on the left (the awards make way for them, and come back when they end).</summary>
	void RollCredits()
	{
		if (_results == null || (_credits != null && IsInstanceValid(_credits) && !_credits.IsQueuedForDeletion())) return;
		_awards!.Visible = false;
		_credits = new CreditsRoll(_results, _myId, () =>
		{
			_credits = null;
			CreditsDone = true;
			_awards.Visible = true;
			_awards.Modulate = Colors.Transparent;
			CreateTween().TweenProperty(_awards, "modulate", Colors.White, 0.6f);
		});
		_stage.AddChild(_credits);
	}

	/// <summary>The screen as a picture (without its buttons), in Documents/Quiznight/Pictures.</summary>
	async void SavePicture()
	{
		if (_results == null || _bar == null) return;
		_bar.Modulate = Colors.Transparent;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		var image = GetViewport().GetTexture().GetImage();
		_bar.Modulate = Colors.White;
		var safe = new string(_results.QuizTitle.Select(c => char.IsLetterOrDigit(c) ? c : '-').Take(40).ToArray()).Trim('-');
		var path = System.IO.Path.Combine(SeasonPoster.Folder, $"Podium {(safe == "" ? "quiz" : safe)} {_results.Date:yyyy-MM-dd HH-mm}.png");
		try
		{
			Directory.CreateDirectory(SeasonPoster.Folder);
			var ok = image.SavePng(path) == Error.Ok;
			_note!.Text = ok ? Lang.F("Saved in {0}.", "Documents/Quiznight/Pictures") : Lang.T("The picture couldn't be saved.");
			if (ok) OS.ShellShowInFileManager(path);
		}
		catch (Exception)
		{
			_note!.Text = Lang.T("The picture couldn't be saved.");
		}
	}
}

/// <summary>A container for one child, drawn at a zoom and centred: its room in the layout grows and shrinks with it.</summary>
public partial class ScaledBox : Container
{
	float _zoom = 1;

	/// <summary>Also centred top to bottom (else at the top).</summary>
	public bool CenterV;

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
		c.Position = new Vector2((Size.X - size.X * _zoom) / 2, CenterV ? Mathf.Max(0, (Size.Y - size.Y * _zoom) / 2) : 0);
	}
}

/// <summary>
/// The credits, rolling up like at the end of a party game: the quiz, the quizmaster, everyone who played, tonight's
/// awards, and thanks. Fills its parent; Skip (or Escape) closes them, and they close by themselves at the end.
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
		MouseFilter = MouseFilterEnum.Ignore;

		_roll.Alignment = BoxContainer.AlignmentMode.Center;
		_roll.AddChild(Centered(U.Heading(r.QuizTitle, 36).Raw()));
		_roll.AddChild(Centered(U.Muted(r.Date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture), 15, false)));

		Section("Quizmaster");
		_roll.AddChild(Centered(U.Text(r.Host != "" ? r.Host : Lang.T("The host"), 30, C.Text, Fonts.Black).With(l => { if (r.Host != "") l.Raw(); })));

		Section("Starring");
		foreach (var p in r.Ranked)
		{
			var mine = p.Id == me || p.MemberIds?.Contains(me ?? "") == true;
			_roll.AddChild(GameParts.Row(10, new Avatar(p.ToPublic(), 30), U.Text(p.Name, 24, mine ? C.Teal : C.Text, Fonts.Bold).Raw(),
				U.Mono(Lang.Ordinal(p.Place), 15, C.Medal(p.Place) ?? C.Muted).Raw()).Center());
			// (a team: its members under it)
			if (p.Members is { } members) _roll.AddChild(Centered(U.Muted(string.Join(", ", members), 16, true).Raw()));
		}

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
		// (the sponsors at the end, like after a film)
		Section("Sponsored by");
		_roll.AddChild(new TrackGlowLogo(0.9f).Center());
		_roll.AddChild(U.Gap(80));
		_roll.AddChild(Centered(U.Heading("Thanks for playing!", 40, false)));
		_roll.AddChild(Centered(U.Muted("See you at the next quiz night.", 16, false)));
		AddChild(_roll);

		var skip = U.Btn("Skip ▸", "", Close, 14);
		skip.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		skip.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		var corner = U.Pad(skip, 0, 0, 8, 8);
		corner.SetAnchorsPreset(LayoutPreset.FullRect);
		corner.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(corner);

		Modulate = Colors.Transparent;
		Ready += () => CreateTween().TweenProperty(this, "modulate", Colors.White, 0.5f);
	}

	/// <summary>Placed below the stage on the first frame (when it's ready, its own size isn't known yet).</summary>
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
