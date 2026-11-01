using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using QuizNight.Net;
using QuizNight.Server;
using QuizNight.Shared;

namespace QuizNight.Ui.Screens;

/// <summary>A screen that shows a room and redraws when its state changes.</summary>
public interface IRoomScreen
{
	void Refresh(RoomState state);
}

public partial class MessageScreen : Control
{
	public MessageScreen() : this("") { }

	/// <summary>What Escape does here: the button's job (e.g. Cancel), or nothing.</summary>
	public Action? Escape;

	public MessageScreen(string text, string? button = null, Action? onButton = null)
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		var box = U.V(16, U.Text(text, 18, C.Muted, null, false, HorizontalAlignment.Center));
		if (button != null) box.AddChild(U.Btn(button, "", onButton).Center());
		Escape = onButton;
		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		center.AddChild(box);
		AddChild(center);
	}
}

public partial class HostLobbyScreen : Control, IRoomScreen
{
	readonly HostSession _host;
	readonly VBoxContainer _content = U.V(12);
	string? _error;
	// (the host was told some players' games are too old and starts anyway)
	bool _versionsOk;

	public HostLobbyScreen() : this(null!) { }

	public HostLobbyScreen(HostSession host)
	{
		_host = host;
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(U.Page(U.Panel(_content).With(Seasonal.SnowOn), 1000, 16));
		host.Arcade.HostName = Storage.Settings.PlayerName is { Length: > 0 } name ? name : Lang.T("Host");
		_arcadeLink = Arcade.ArcadeLink.ForHost(host);
		_arcadeLink.Frame += OpenOnMatch;
	}

	// ---------- the mini games (Ui/Arcade) ----------

	readonly Arcade.ArcadeLink _arcadeLink;
	Arcade.ArcadeWindow? _arcade;

	public void OpenArcade()
	{
		if (_arcade == null || !IsInstanceValid(_arcade)) AddChild(_arcade = new Arcade.ArcadeWindow(_arcadeLink));
	}

	/// <summary>Someone accepted my challenge while the window was closed: it opens on the match.</summary>
	void OpenOnMatch(ArcadeFrame frame)
	{
		if (frame.Winner != null || _arcade != null && IsInstanceValid(_arcade)) return;
		OpenArcade();
		_arcade!.OnFrame(frame);
	}

	public override void _ExitTree()
	{
		_arcadeLink.Frame -= OpenOnMatch;
		_arcadeLink.Unhook();
	}

	/// <summary>
	/// Laid out like the players' waiting room (the code, a seat for everyone plus an open one), with the host's options
	/// in two columns underneath, so it fits without scrolling.
	/// </summary>
	public void Refresh(RoomState room)
	{
		_content.Clear();
		var connected = room.Players.Count(p => p.Connected);
		// (more than one row of big seat cards pushed Start below the window: from 7 seats on they're small cards, one line
		// each, and how to join moves into the player count's tooltip; by then everyone knows)
		var seats = Math.Min(room.Players.Count + 1, Limits.MaxPlayers);
		var compact = seats > 6;
		string howToJoin;
		if (_host.Mode == NetMode.Steam)
			howToJoin = Lang.F("Players open Quiznight, choose Steam on the home screen and enter this code. {0} here so far (up to {1}).", room.Players.Count, Limits.MaxPlayers);
		else
		{
			var addresses = LanHostTransport.LanAddresses();
			var where = addresses.Count > 0 ? string.Join(Lang.T("  or  "), addresses) : Lang.T("this PC's network address");
			howToJoin = Lang.F("LAN mode: players choose \"LAN / same PC\" and enter {0} (same Wi-Fi), or 127.0.0.1 on this PC. Port {1}. {2} here so far (up to {3}).", where, _host.LanPort, room.Players.Count, Limits.MaxPlayers);
		}
		var quizLine = U.Muted($"{room.Quiz.Title} · {U.Plural(room.Quiz.QuestionCount, "question", "questions")}", 15, !compact).Raw();
		// (a full lobby: the quiz's name beside "Lobby" instead of on a line of its own)
		Control title = compact ? U.H(14, U.Heading("Lobby", 28, false), quizLine.With(l => { l.SizeFlagsVertical = SizeFlags.ShrinkCenter; l.ClipText = true; }).Grow()) : U.Heading("Lobby", 28, false);
		_content.AddChild(U.H(8, title.Grow(), new RoomCodeButton(room.Code, 32),
			U.Pill(U.Plural(room.Players.Count, "player", "players")).With(pill => { if (compact) pill.Tip(howToJoin); }), MediaIcon.SettingsGear()));
		if (!compact) _content.AddChild(quizLine);
		if (room.ResumeFrom is { } from)
			_content.AddChild(U.Text(Lang.F("▶ A saved quiz night goes on at question {0} of {1}. Everyone gets their seat and points back when they join again ({2}).",
				from, room.Quiz.QuestionCount, Lang.T(_host.Mode == NetMode.Steam ? "with the same Steam account" : "with the same name")), 14, C.Accent, wrap: true));
		if (!compact) _content.AddChild(U.Muted(howToJoin, 13).Raw());

		// Seats: everyone who's in (× removes them), plus one open seat while the room isn't full.
		var grid = new GridContainer { Columns = 6 };
		grid.AddThemeConstantOverride("h_separation", compact ? 8 : 10);
		grid.AddThemeConstantOverride("v_separation", compact ? 6 : 10);
		var tags = U.AnyClanTag(room.Players.Select(p => p.Name));
		foreach (var p in room.Players)
		{
			var id = p.Id;
			if (compact)
			{
				grid.AddChild(SmallSeat(p, room));
				continue;
			}
			var seat = U.V(4, new Avatar(p, 44).Center(), U.PlayerName(p.Name, 15, HorizontalAlignment.Center, tags));
			if (p.Team is { } team) seat.AddChild(TeamParts.Pill(team).Center());
			if (room.ResumeFrom != null) seat.AddChild(U.Muted(room.Score(id) == 1 ? Lang.T("1 point") : Lang.F("{0} points", room.Score(id)), 12, false, HorizontalAlignment.Center));
			seat.AddChild(
				U.H(4, GameParts.SeatStatus(p, room, false).With(s => s.SizeFlagsVertical = SizeFlags.ShrinkCenter),
					U.Link("×", () => _host.Kick(id), 15).Tip(Lang.F("Remove {0}", p.Name)).With(b => b.SizeFlagsVertical = SizeFlags.ShrinkCenter)).Center());
			// (an older game than the host's: which version, so the host can ask them to update)
			if (VersionCheck.Older(p.Version, App.Version))
				seat.AddChild(Glyph.Before(Glyph.Kind.Warning, U.Text(p.Version != "" ? p.Version : Lang.T("older version"), 12, C.Orange).Raw(), 13, grow: false)
					.With(r => { r.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; r.MouseFilter = MouseFilterEnum.Stop; })
					.Tip(Lang.F("An older Quiznight than yours ({0}). Ask them to update.", App.Version)));
			grid.AddChild(U.Panel(seat, "Card2").Grow());
		}
		for (var i = room.Players.Count; i < seats; i++)
		{
			if (compact)
			{
				var open = U.H(8, U.Panel(U.Gap(22), "Clear").With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(Colors.Transparent, 999, C.Border, 2, 4, 4))),
					U.Muted("Open seat", 12, false).With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter));
				grid.AddChild(U.Panel(U.Pad(open, 0, 2, 0, 2), "Card2").With(p => p.Modulate = new Color(1, 1, 1, 0.5f)).Grow());
				continue;
			}
			var empty = U.V(4, U.Panel(U.Gap(34), "Clear").With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(Colors.Transparent, 999, C.Border, 2, 4, 4))).Center(),
				U.Muted(room.Players.Count == 0 ? "Send the code to your friends" : "Open seat", 12, true, HorizontalAlignment.Center));
			grid.AddChild(U.Panel(empty, "Card2").With(p => p.Modulate = new Color(1, 1, 1, 0.5f)).Grow());
		}
		_content.AddChild(grid);
		_content.AddChild(Arcade.ArcadeWindow.LobbyStrip(_arcadeLink, OpenArcade));
		if (_arcade != null && IsInstanceValid(_arcade)) _arcade.Refresh();
		// (the options sit at the bottom of the screen; the seats grow into the space above them)
		_content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

		// The host's options: rules on the left, time and sounds on the right (details on hover).
		var left = U.V(2,
			U.Muted("How you run it", 13),
			Rule("I show each question myself", "It stays hidden until you press Show (Space).", room.Settings.HostShowsQuestions,
				on => _host.UpdateSettings(hostShows: on)),
			Rule("I unlock the buzzers myself", "After reading the question out (Space).", room.Settings.HostUnlocksBuzzers,
				on => _host.UpdateSettings(hostUnlocks: on)),
			Rule("Only my Steam friends can join", "Over Steam: players who aren't on your Steam friends list can't come in, even with the code.",
				room.Settings.FriendsOnly, on => _host.UpdateSettings(friendsOnly: on)),
			Rule("Show question categories", "Players see each question's category (e.g. Geography) on the Get ready screen. It makes questions easier; secret ones stay hidden.",
				room.Settings.ShowCategories, on => _host.UpdateSettings(categories: on)),
			U.Muted("Jokers (once per player and match)", 13).With(l => l.TooltipText = Lang.T("Played before a question starts.")),
			Rule("×2 Double points", "Whatever the next question gives counts twice. Anyone can play it.", room.Settings.Jokers.Contains(Joker.Double),
				on => _host.UpdateSettings(joker: (Joker.Double, on))),
			Rule("Fast Pass", "On the next buzzer question, you answer first, even if someone is already answering. One player per question.", room.Settings.Jokers.Contains(Joker.FastPass),
				on => _host.UpdateSettings(joker: (Joker.FastPass, on))).With(b => b.Icon = Bolt.Icon(15)));
		// (Team mode under the sounds, not under the jokers: there the rules card grew taller than the window and the lobby
		// scrolled, while this column had room to spare)
		var right = U.V(8,
			Stepper.Make("Answer time per buzz", room.Settings.AnswerTimeSec, Limits.AnswerTimeMin, Limits.AnswerTimeMax,
				v => _host.UpdateSettings(answerTimeSec: v), " s"),
			BuzzerPicker.Make(_host.BuzzerSound, id => _host.BuzzerSound = id, false),
			PodiumPicker.Make(_host.PodiumSound, id => _host.PodiumSound = id, true),
			TeamRow(room));
		_content.AddChild(U.Panel(U.V(8, U.Eyebrow("Game rules for tonight"), U.H(24, left.Grow(), right.Grow())), "Inset"));

		// What players on older games can't play in this quiz.
		var cant = room.Players.Where(p => p.Connected)
			.Select(p => (p.Name, Missing: VersionCheck.Missing(_host.Room.Quiz, p.Version))).Where(c => c.Missing.Count > 0).ToList();
		foreach (var (name, missing) in cant)
			_content.AddChild(Glyph.Before(Glyph.Kind.Warning, U.Text(Lang.F("{0}'s game is too old for {1}. Ask them to update.", name, string.Join(", ", missing)), 13, C.Orange, wrap: true)));

		if (_error != null) _content.AddChild(U.Error(_error));
		void StartQuiz()
		{
			if (cant.Count > 0 && !_versionsOk)
			{
				Dialogs.Confirm(this, Lang.F("{0} can't play every question on their version (see the lobby). Start anyway?", string.Join(", ", cant.Select(c => c.Name))),
					"Start anyway", () => { _versionsOk = true; StartQuiz(); });
				return;
			}
			var result = _host.Start();
			_error = result.Ok ? null : result.Error;
			if (!result.Ok) Refresh(_host.State);
		}
		var undrawn = room.Settings.TeamMode && room.Players.Any(p => p.Connected && p.Team == null);
		var start = U.Btn(room.ResumeFrom != null ? "Continue quiz" : "Start quiz", "AccentButton", StartQuiz);
		// Everyone gets the quiz's pictures, sounds and videos first, so nothing has to load mid-question.
		var loading = room.Players.Where(p => p.Connected && room.Downloads.GetValueOrDefault(p.Id, 100) < 100).ToList();
		start.Disabled = connected < Limits.MinPlayersToStart || loading.Count > 0 || undrawn;
		Control needs = connected < Limits.MinPlayersToStart ? U.Muted(Lang.F("Needs at least {0} players.", Limits.MinPlayersToStart), 13, false)
			: undrawn ? U.Muted("Draw the teams first.", 13, false)
			: loading.Count > 0 ? U.H(10,
				U.Muted(loading.Count == 1 ? Lang.F("{0} is still downloading the quiz…", loading[0].Name) : Lang.F("{0} players are still downloading the quiz…", loading.Count), 13, false),
				U.Link("Start anyway", StartQuiz, 13).Tip("Whoever isn't done gets each question's pictures when it comes up"))
			: U.Fill();
		_content.AddChild(U.H(12, U.Link("Close room", () => Dialogs.Confirm(this, "Close the room? Everyone in it is sent back to the home screen.", "Close room", App.I.CloseRoom)),
			U.Fill(), needs, start));
		TeamDrawShow.MaybeShow(this, room);
	}

	/// <summary>
	/// "Play in teams" (off to start with). Ticked, the room picks how many teams from the players here and keeps
	/// following them (Room.FollowPlayers); before the draw the host can pick another split instead ("2 teams of 4" or
	/// "4 teams of 2"). Then the draw, again as often as you like until the start.
	/// </summary>
	Control TeamRow(RoomState room)
	{
		var on = room.Settings.TeamMode;
		var box = Rule("Play in teams", "Teams are drawn at random. A team answers together: anyone in it can change the team's answer until time is up. Jokers once per team. Team nights don't count for the Ewige Tabelle.",
			on, play => _host.UpdateSettings(teams: play ? TeamNames.Auto : 0));
		if (!on) return box;
		var players = room.Players.Count(p => p.Connected);
		var splits = TeamNames.Offered(players);
		// (the host's pick, if it's fair but not among the best three)
		if (!splits.Contains(room.Settings.Teams) && TeamNames.Splits(players).Contains(room.Settings.Teams)) splits = splits.Append(room.Settings.Teams).OrderBy(t => t).ToList();
		var drawn = room.Teams != null;
		var fair = drawn || TeamNames.Possible(players);
		var col = U.V(8, box);
		// (the split can only change before the draw; afterwards newcomers join the smallest team)
		if (!drawn && fair)
			col.AddChild(U.Segmented(splits.Select(t => (t.ToString(), Split(players, t))).ToArray(), room.Settings.Teams.ToString(),
				id => _host.UpdateSettings(teams: int.Parse(id))));
		var draw = U.Small(drawn ? "Draw again" : "Draw teams", () =>
		{
			var result = _host.DrawTeams();
			_error = result.Ok ? null : result.Error;
			if (!result.Ok) Refresh(_host.State);
		}).On(Glyph.Kind.Dice, 15).Tip("Everyone is put in a team at random, with a show on every screen.");
		// (no team may have fewer than 3/4 of another's players: 3 or 5 players can't be split fairly)
		draw.Disabled = !fair;
		col.AddChild(U.H(10, draw,
			U.Muted(drawn ? Lang.F("{0} teams drawn. Draw again to change the split.", room.Settings.Teams)
				: !fair ? Lang.F("{0} players can't be split into fair teams. 4, 6 or more work.", players)
				: "Follows the number of players until you pick a split.", 12, true).With(l => { l.SizeFlagsVertical = SizeFlags.ShrinkCenter; l.Grow(); })));
		return col;
	}

	/// <summary>
	/// A seat on one line for a full lobby: avatar, a team's colour dot, the name, ⚠ for an older game, a download ring
	/// (✓ when done, … when away) and ×. What the big card says in words is in the tooltips.
	/// </summary>
	Control SmallSeat(PublicPlayer p, RoomState room)
	{
		var id = p.Id;
		var row = U.H(6, new Avatar(p, 26).With(a => a.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		Control Centered(Control c)
		{
			c.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			c.MouseFilter = MouseFilterEnum.Stop;
			return c;
		}
		if (p.Team is { } team) row.AddChild(Centered(U.Text("●", 12, TeamParts.ColorOf(team), null, false).Raw().Tip(TeamNames.Name(team))));
		row.AddChild(U.PlayerName(p.Name, 14, HorizontalAlignment.Left).With(n => { n.SizeFlagsHorizontal = SizeFlags.ExpandFill; n.SizeFlagsVertical = SizeFlags.ShrinkCenter; n.CustomMinimumSize = new Vector2(40, 0); }));
		if (VersionCheck.Older(p.Version, App.Version))
			row.AddChild(Centered(Glyph.Rect(Glyph.Kind.Warning, 13, C.Orange).With(r => r.MouseFilter = MouseFilterEnum.Stop)
				.Tip(Lang.F("An older Quiznight than yours ({0}). Ask them to update.", App.Version) + (p.Version != "" ? $" ({p.Version})" : ""))));
		if (room.ResumeFrom != null) row.AddChild(Centered(U.Muted($"{room.Score(id)}", 12, false).Raw().Tip(Lang.F("{0} points", room.Score(id)))));
		var percent = room.Downloads.GetValueOrDefault(id, 100);
		row.AddChild(Centered(!p.Connected ? U.Muted("…", 13, false).Raw().Tip(room.ResumeFrom != null ? "Not back yet" : "Offline")
			: percent < 100 ? new DownloadRing(percent, 14).Tip(Lang.F("Downloading {0} %", percent))
			: U.Text("✓", 13, C.Teal).Raw().Tip(room.Downloads.Count > 0 ? "Ready" : "Joined")));
		row.AddChild(U.Link("×", () => _host.Kick(id), 15).Tip(Lang.F("Remove {0}", p.Name)).With(b => b.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		// (Card2 with less padding: one line doesn't need its height, and 24 seats are 4 rows)
		return U.Panel(row, "Card2").With(c => c.AddThemeStyleboxOverride("panel", UiTheme.Chunky(C.Panel2, 14, 10, 3, 3, 3))).Grow();
	}

	/// <summary>"2 teams of 4", or "3 teams (3/3/2)" when they can't be even.</summary>
	static string Split(int players, int teams) => players >= teams && players % teams == 0
		? Lang.F("{0} teams of {1}", teams, players / teams)
		: Lang.F("{0} teams ({1})", teams, players >= teams ? TeamNames.Sizes(players, teams) : "…");

	static CheckBox Rule(string text, string tip, bool on, Action<bool> changed)
	{
		var box = new CheckBox { Text = text, TooltipText = tip, ButtonPressed = on, MouseDefaultCursorShape = CursorShape.PointingHand };
		box.Toggled += v => changed(v);
		return box;
	}
}

public partial class WaitingRoomScreen : Control, IRoomScreen
{
	readonly ClientSession _client;
	readonly VBoxContainer _content = U.V(14);

	public WaitingRoomScreen() : this(null!) { }

	public WaitingRoomScreen(ClientSession client)
	{
		_client = client;
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(U.Page(U.Panel(_content).With(Seasonal.SnowOn), 1000, 16));
		_arcadeLink = Arcade.ArcadeLink.ForPlayer(client);
		_arcadeLink.Frame += OpenOnMatch;
	}

	// ---------- the mini games (Ui/Arcade) ----------

	readonly Arcade.ArcadeLink _arcadeLink;
	Arcade.ArcadeWindow? _arcade;

	void OpenArcade()
	{
		if (_arcade == null || !IsInstanceValid(_arcade)) AddChild(_arcade = new Arcade.ArcadeWindow(_arcadeLink));
	}

	/// <summary>Someone accepted my challenge while the window was closed: it opens on the match.</summary>
	void OpenOnMatch(ArcadeFrame frame)
	{
		if (frame.Winner != null || _arcade != null && IsInstanceValid(_arcade)) return;
		OpenArcade();
		_arcade!.OnFrame(frame);
	}

	public override void _ExitTree()
	{
		_arcadeLink.Frame -= OpenOnMatch;
		_arcadeLink.Unhook();
	}

	/// <summary>While you change your name: the field (kept across updates, so typing isn't lost).</summary>
	LineEdit? _rename;
	string? _renameError;

	public void Refresh(RoomState room)
	{
		_rename?.GetParent()?.RemoveChild(_rename);
		_content.Clear();
		_content.AddChild(U.H(8, U.Heading("Waiting room", 28, false).Grow(),
			_client.Mode == NetMode.Steam ? new RoomCodeButton(room.Code) : U.Pill(Lang.F("Host {0}", _client.Target)),
			U.Pill(U.Plural(room.Players.Count, "player", "players")), MediaIcon.SettingsGear()));
		_content.AddChild(U.Muted(room.Quiz.Title, 15).Raw());
		if (room.ResumeFrom is { } from)
			_content.AddChild(U.Text(Lang.F("▶ The host continues a saved quiz night at question {0} of {1}: everyone keeps their points.", from, room.Quiz.QuestionCount),
				14, C.Accent, wrap: true));

		var grid = new GridContainer { Columns = 4 };
		grid.AddThemeConstantOverride("h_separation", 12);
		grid.AddThemeConstantOverride("v_separation", 12);
		var tags = U.AnyClanTag(room.Players.Select(p => p.Name));
		foreach (var p in room.Players)
		{
			var me = p.Id == _client.MyId;
			var seat = U.V(6, new Avatar(p, 56).Center(), me ? MyName(p, room, tags) : U.PlayerName(p.Name, 16, HorizontalAlignment.Center, tags),
				me && room.Downloads.GetValueOrDefault(p.Id, 100) >= 100 ? U.Muted("You · double-click your name to change it", 13, true, HorizontalAlignment.Center)
					: GameParts.SeatStatus(p, room, true).Center());
			if (me && _renameError != null) seat.AddChild(U.Text(_renameError, 12, C.Coral, null, true, HorizontalAlignment.Center));
			if (p.Team is { } team) seat.AddChild(TeamParts.Pill(team).Center());
			grid.AddChild(U.Panel(seat, me ? "Scored" : "Card2").Grow());
		}
		// Everyone who's in, plus one open seat (while the room isn't full): the grid grows as people join.
		for (var i = room.Players.Count; i < Math.Min(room.Players.Count + 1, Limits.MaxPlayers); i++)
		{
			var empty = U.V(6, U.Panel(U.Gap(44), "Clear").With(p => p.AddThemeStyleboxOverride("panel", UiTheme.Box(Colors.Transparent, 999, C.Border, 2, 4, 4))).Center(),
				U.Muted("Open seat", 13, false, HorizontalAlignment.Center));
			grid.AddChild(U.Panel(empty, "Card2").With(p => p.Modulate = new Color(1, 1, 1, 0.5f)).Grow());
		}
		_content.AddChild(grid);
		// (the mini games need a version from 0.12 on on both sides: the host sends their part of the state)
		if (room.Arcade != null) _content.AddChild(Arcade.ArcadeWindow.LobbyStrip(_arcadeLink, OpenArcade));
		if (_arcade != null && IsInstanceValid(_arcade)) _arcade.Refresh();
		_content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
		_content.AddChild(Rules(room.Settings));
		_content.AddChild(U.H(16,
			U.Muted("Waiting for the host to start the quiz.", 14, false).Grow(),
			U.Link("Leave", App.I.LeaveRoom)));
		TeamDrawShow.MaybeShow(this, room);
	}

	/// <summary>Tonight's rules as the host set them (read only): how the host runs it, and the jokers you can play.</summary>
	static Control Rules(RoomSettings settings)
	{
		Control Line(string text) => U.Text(text, 15);
		var general = U.V(6,
			Line(settings.HostShowsQuestions ? "• The host shows each question when ready" : "• Questions show right away"),
			Line(settings.HostUnlocksBuzzers ? "• The host unlocks the buzzers after reading out" : "• Buzzers open as soon as a question shows"),
			Line(settings.ShowCategories ? "• You see each question's category" : "• Categories stay hidden"),
			Line(settings.TeamMode ? Lang.F("• Team mode: {0} teams, drawn at random. Your team answers together.", settings.Teams) : Lang.T("• Everyone plays for themselves")),
			Line(Lang.F("• Answer time per buzz: {0} s", settings.AnswerTimeSec)));
		var jokers = U.V(6, U.Muted("Jokers (once each per match)", 13));
		// (every joker stays listed, greyed out when the host turns it off, so the panel doesn't jump in height)
		foreach (var kind in Joker.All)
		{
			var on = settings.Jokers.Contains(kind);
			var row = U.H(8, Bolt.JokerPill(kind, on ? C.Accent : C.Muted), U.Muted(!on ? Lang.T("Off tonight.") : kind == Joker.Double
				? Lang.T("The next question counts twice. Anyone can play it.")
				: Lang.T("You answer first, even if someone is already answering. One player per question."), 13).Grow());
			if (!on) row.Modulate = new Color(1, 1, 1, 0.45f);
			jokers.AddChild(row);
		}
		return U.Panel(U.V(8, U.Eyebrow("Tonight's rules"), U.H(24, general.Grow(), jokers.Grow())), "Inset");
	}
}

public partial class WaitingRoomScreen
{
	/// <summary>Your name; double-click it to change it (Enter keeps it, Esc goes back).</summary>
	Control MyName(PublicPlayer me, RoomState room, bool reserveTag)
	{
		if (_rename != null) return _rename;
		var name = U.PlayerName(me.Name, 16, HorizontalAlignment.Center, reserveTag);
		name.MouseFilter = MouseFilterEnum.Stop;
		name.MouseDefaultCursorShape = CursorShape.Ibeam;
		name.TooltipText = Lang.T("Double-click to change your name");
		name.GuiInput += e =>
		{
			if (e is not InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left }) return;
			_rename = U.Input(me.Name, "", null, Limits.NameInputMax);
			_rename.Alignment = HorizontalAlignment.Center;
			var field = _rename;
			field.TextSubmitted += _ => Done(true);
			field.FocusExited += () => Done(true);
			field.GuiInput += k =>
			{
				if (k is InputEventKey { Pressed: true, Keycode: Key.Escape }) Done(false);
			};
			Refresh(room);
			field.CallDeferred(Control.MethodName.GrabFocus);
			field.CallDeferred(LineEdit.MethodName.SelectAll);

			void Done(bool keep)
			{
				if (_rename != field) return;
				_rename = null;
				var text = Server.Room.CleanName(field.Text);
				_renameError = null;
				if (keep && text != "" && text != me.Name)
				{
					if (room.Players.Any(p => p.Id != me.Id && string.Equals(p.Name, text, StringComparison.OrdinalIgnoreCase)))
						_renameError = Lang.T("Someone in this room already uses that name.");
					else
					{
						_client.Rename(text);
						Storage.Settings.PlayerName = text;
					}
				}
				// (after the field's own signal is done: rebuilding frees it)
				Callable.From(() => Refresh(_client.State ?? room)).CallDeferred();
			}
		};
		return name;
	}
}
