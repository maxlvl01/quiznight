// Team mode on screen (from 0.13.1d1): the draw as a show (every screen in the lobby plays it when the host draws), the
// team badge on seats and the team standings over the scores. The teams themselves are made on the host (Room.DrawTeams);
// a team answers once per question and plays each joker once (Game.Mates), its points are its members' together.

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

public static class TeamParts
{
	public static Color ColorOf(int team) => new(TeamNames.Color(team));

	/// <summary>"● Smarty Pints" in the team's colour.</summary>
	public static Control Pill(int team, int size = 12) =>
		U.Pill(TeamNames.Name(team), ColorOf(team)).With(p => p.Raw()).With(p => p.TooltipText = Lang.T("Their team"));

	/// <summary>The teams by place, with their members and points (the player's own team outlined).</summary>
	public static Control Board(RoomState room, string? me = null)
	{
		var list = U.V(6);
		var mine = me != null ? room.Player(me).Team : null;
		foreach (var team in room.Teams ?? new List<PublicTeam>())
		{
			var names = string.Join(", ", team.Members.Select(id => room.Player(id).Name));
			var row = GameParts.Row(8, GameParts.RankBadge(team.Place, true),
				U.V(0, U.Text(team.Name, 15, ColorOf(team.Index), Fonts.Black).Raw(), U.Muted(names, 12, true).Raw()).Grow(),
				U.Mono($"{team.Score}", 20).With(l => l.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter));
			list.AddChild(U.Panel(row, "Clear").With(p => p.AddThemeStyleboxOverride("panel",
				UiTheme.Box(new Color(ColorOf(team.Index), 0.1f), 10, team.Index == mine ? C.Teal : ColorOf(team.Index), team.Index == mine ? 2 : 1, 10, 6))));
		}
		return list;
	}
}

/// <summary>
/// The draw: team columns at the bottom, a reel in the middle that spins through everyone's names, slows down and lands
/// on one, who flies into their team. Faster as it goes; then "The teams are set!" with confetti. A click or Escape skips
/// to the end; it closes by itself a few seconds after.
/// </summary>
public partial class TeamDrawShow : Control
{
	/// <summary>The last draw each room's screens have seen: a screen built again doesn't show it twice.</summary>
	static (string Code, int Draw) _seen;

	/// <summary>A lobby screen's update: plays the draw if this one is new (not when you come into a room drawn before).</summary>
	public static void MaybeShow(Control screen, RoomState room)
	{
		if (room.TeamDraw == 0 || room.Teams == null) return;
		if (_seen.Code != room.Code)
		{
			_seen = (room.Code, room.TeamDraw);
			return;
		}
		if (room.TeamDraw <= _seen.Draw) return;
		_seen = (room.Code, room.TeamDraw);
		screen.AddChild(new TeamDrawShow(room.Teams.OrderBy(t => t.Index).Select(t => t.Members.Select(id => room.Player(id).Name).ToList()).ToList(), room.TeamDraw));
	}

	readonly List<(string Name, int Team)> _picks;
	readonly List<VBoxContainer> _columns = new();
	readonly Label _reel;
	readonly Label _title;
	readonly Label _hint;
	readonly AudioStreamPlayer _tick = new() { Bus = Prefs.SfxBus };
	readonly AudioStreamPlayer _land = new() { Bus = Prefs.SfxBus, VolumeDb = -6 };
	readonly Random _rng;
	int _next;
	double _spun, _spinFor, _sinceTick, _tickEvery;
	bool _done;
	double _closeIn = -1;

	public TeamDrawShow() : this(new List<List<string>>(), 0) { }

	/// <param name="teams">Each team's members' names, in team order.</param>
	/// <param name="seed">The same on every screen (the draw's number), so everyone sees the same order.</param>
	public TeamDrawShow(List<List<string>> teams, int seed)
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		_rng = new Random(seed * 7919 + teams.Sum(t => t.Count));
		// (everyone, in an order every screen agrees on)
		_picks = teams.SelectMany((members, t) => members.Select(m => (m, t))).OrderBy(p => StableHash(p.m + "|" + seed)).ToList();

		AddChild(new ColorRect { Color = new Color(C.Bg, 0.985f), MouseFilter = MouseFilterEnum.Ignore }.With(r => r.SetAnchorsPreset(LayoutPreset.FullRect)));
		_title = U.Text(Lang.T("The team draw"), 40, C.Text, Fonts.Black, false, HorizontalAlignment.Center);
		_hint = U.Muted("Click to skip", 13, false, HorizontalAlignment.Center);
		_reel = U.Text("…", 56, C.Accent, Fonts.Black, false, HorizontalAlignment.Center).Raw();
		_reel.CustomMinimumSize = new Vector2(0, 90);
		var reelPanel = U.Panel(_reel, "Clear").With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(C.Panel2, 18, C.Accent, 3, 30, 12)));
		reelPanel.CustomMinimumSize = new Vector2(560, 0);

		var row = U.H(14);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		for (var t = 0; t < teams.Count; t++)
		{
			var members = U.V(6);
			_columns.Add(members);
			var column = U.V(10, U.Text(TeamNames.Name(t), 18, TeamParts.ColorOf(t), Fonts.Black, true, HorizontalAlignment.Center).Raw(), members);
			var panel = U.Panel(column, "Clear").With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(new Color(TeamParts.ColorOf(t), 0.08f), 14, TeamParts.ColorOf(t), 2, 14, 12)));
			panel.CustomMinimumSize = new Vector2(Math.Clamp(1100 / Math.Max(1, teams.Count) - 14, 150, 260), 220);
			row.AddChild(panel);
		}

		var page = U.V(26, Glyph.Before(Glyph.Kind.Dice, _title, 38, grow: false).With(r => r.Alignment = BoxContainer.AlignmentMode.Center), reelPanel.Center(), row, _hint);
		page.Alignment = BoxContainer.AlignmentMode.Center;
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		center.AddChild(page);
		AddChild(center);
		AddChild(_tick);
		AddChild(_land);
		if (ResourceLoader.Exists("res://assets/soundeffects/ui/click.ogg")) _tick.Stream = GD.Load<AudioStream>("res://assets/soundeffects/ui/click.ogg");
		if (ResourceLoader.Exists("res://assets/soundeffects/Correct Answer.ogg")) _land.Stream = GD.Load<AudioStream>("res://assets/soundeffects/Correct Answer.ogg");
		StartSpin();
	}

	static int StableHash(string s)
	{
		unchecked
		{
			var h = 23;
			foreach (var c in s) h = h * 31 + c;
			return h;
		}
	}

	/// <summary>The next name's spin: long at first, quicker as the draw goes on.</summary>
	void StartSpin()
	{
		if (_next >= _picks.Count)
		{
			Finish();
			return;
		}
		var left = _picks.Count - _next;
		_spinFor = Announcer.ReduceMotion ? 0.25 : Math.Clamp(0.55 + 0.12 * left, 0.6, 1.8);
		_spun = 0;
		_sinceTick = 0;
		_tickEvery = 0.05;
	}

	public override void _Process(double delta)
	{
		if (_done)
		{
			if (_closeIn > 0 && (_closeIn -= delta) <= 0) Close();
			return;
		}
		_spun += delta;
		_sinceTick += delta;
		// (the reel slows down towards the end of its spin, like a wheel running out)
		var t = Math.Clamp(_spun / _spinFor, 0, 1);
		_tickEvery = 0.04 + 0.22 * t * t;
		if (_sinceTick >= _tickEvery && t < 1)
		{
			_sinceTick = 0;
			var name = _picks[_rng.Next(_picks.Count)].Name;
			_reel.Text = name;
			_reel.AddThemeColorOverride("font_color", C.Muted);
			_tick.PitchScale = 0.9f + (float)t * 0.5f;
			_tick.Play();
		}
		if (t >= 1) Land();
	}

	/// <summary>The reel stops on the next name: it shows in the team's colour and pops into the team's column.</summary>
	void Land()
	{
		var (name, team) = _picks[_next];
		_reel.Text = name;
		_reel.AddThemeColorOverride("font_color", TeamParts.ColorOf(team));
		_land.Play();
		AddToColumn(name, team, true);
		_next++;
		// (a short beat on the name before the next spin)
		_spun = 0;
		_spinFor = 1e9;
		GetTree().CreateTimer(Announcer.ReduceMotion ? 0.2 : 0.55).Timeout += () =>
		{
			if (IsInstanceValid(this) && !_done) StartSpin();
		};
	}

	void AddToColumn(string name, int team, bool pop)
	{
		var card = U.Panel(U.Text(name, 16, C.Text, Fonts.Bold, true, HorizontalAlignment.Center).Raw(), "Clear")
			.With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(C.Panel2, 10, TeamParts.ColorOf(team), 1, 10, 6)));
		_columns[team].AddChild(card);
		if (!pop || Announcer.ReduceMotion) return;
		card.Modulate = new Color(1, 1, 1, 0);
		Callable.From(() =>
		{
			if (!IsInstanceValid(card)) return;
			card.PivotOffset = card.Size / 2;
			card.Scale = new Vector2(1.5f, 1.5f);
			var tween = card.CreateTween().SetParallel();
			tween.TweenProperty(card, "scale", Vector2.One, 0.35).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(card, "modulate:a", 1f, 0.15);
		}).CallDeferred();
	}

	/// <summary>Everyone is in their team: the title says so, confetti, and it closes by itself.</summary>
	void Finish()
	{
		if (_done) return;
		_done = true;
		for (; _next < _picks.Count; _next++) AddToColumn(_picks[_next].Name, _picks[_next].Team, false);
		_title.Text = Lang.T("The teams are set!");
		_reel.Text = Lang.T("Good luck!");
		_reel.AddThemeColorOverride("font_color", C.Teal);
		_hint.Text = Lang.T("Click to close");
		if (!Announcer.ReduceMotion) AddChild(new Confetti());
		_closeIn = 5;
	}

	void Close()
	{
		_done = true;
		QueueFree();
	}

	public override void _GuiInput(InputEvent e)
	{
		if (e is not InputEventMouseButton { Pressed: true }) return;
		AcceptEvent();
		if (_done) Close();
		else Finish();
	}

	public override void _UnhandledKeyInput(InputEvent e)
	{
		if (!e.IsActionPressed("ui_cancel")) return;
		GetViewport().SetInputAsHandled();
		if (_done) Close();
		else Finish();
	}
}
