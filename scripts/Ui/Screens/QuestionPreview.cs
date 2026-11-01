// The editor's "Preview": the question as a player sees it in a match, over the editor. A real game runs for it in the
// background (a room with just this question, its host here, one player connected in memory: Net/LoopbackTransport), so
// the preview is the player's screen itself, not a copy that could drift from it. Nothing is saved, sent or counted.

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using QuizNight.Net;
using QuizNight.Server;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Ui.Screens;

public partial class QuestionPreview : Control
{
	/// <summary>The preview open now, if any (Escape and the mouse's back button close it, App).</summary>
	public static QuestionPreview? Current { get; private set; }

	readonly Quiz _quiz;
	readonly Control _stage = new() { SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
	readonly HBoxContainer _actions = U.H(10);
	HostSession? _host;
	ClientSession? _client;
	PlayerGameScreen? _screen;

	public QuestionPreview() : this(new Quiz()) { }

	QuestionPreview(Quiz quiz)
	{
		_quiz = quiz;
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		var back = new ColorRect { Color = C.Bg, MouseFilter = MouseFilterEnum.Ignore };
		back.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(back);
		var bar = U.Panel(U.H(12,
			U.V(0, U.Bold("👁 Preview: what players see", 17), U.Muted("You're the only player. Nothing is saved or counted.", 12, false)).Grow(),
			_actions,
			U.Btn("Close", "", Close).With(b => b.TooltipText = "Esc")), "Bar");
		var layout = U.V(0, bar, _stage);
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(layout);
		Begin();
	}

	/// <summary>Opens the preview of this question as it is in the editor now (draft and maps, saved or not).</summary>
	public static void Open(Quiz quiz, Question draft, System.Collections.Generic.IEnumerable<MapAsset> maps)
	{
		Close();
		// (the quiz with only this question: its id stays, so it is still that quiz's files and maps)
		var copy = new Quiz { Id = quiz.Id, Title = quiz.Title, Maps = maps.Select(m => m.Clone()).ToList(), Questions = { draft.Clone() } };
		Current = new QuestionPreview(copy);
		App.I.AddChild(Current);
	}

	public static void Close()
	{
		if (Current is not { } preview) return;
		Current = null;
		preview.End();
		preview.QueueFree();
	}

	/// <summary>A new game for the preview: the question shown straight away, the buzzers open (no host clicks needed).</summary>
	void Begin()
	{
		End();
		_stage.Clear();
		_stage.AddChild(U.Muted("Starting the preview…", 16, false, HorizontalAlignment.Center).With(l => l.SetAnchorsPreset(LayoutPreset.Center)));
		var link = new Loopback();
		var room = new Room(Room.NewCode(), _quiz) { Preview = true };
		_host = new HostSession(room, link.Host, NetMode.Lan) { BuzzerSound = Settings.BuzzerSound };
		_host.UpdateSettings(hostShows: false, hostUnlocks: false);
		_host.Changed += Refresh;
		var client = new ClientSession(NetMode.Lan, "preview", Lang.T("You"), "preview",
			() => Task.FromResult<(IClientTransport?, string?)>((link.Client, null)));
		_client = client;
		client.Changed += Refresh;
		client.Ended += _ =>
		{
			// (the player screen's own "Leave": the preview closes)
			if (_client == client) Callable.From(Close).CallDeferred();
		};
		client.Start();
	}

	void End()
	{
		var client = _client;
		_client = null;
		client?.Dispose();
		_host?.Dispose();
		_host = null;
		_screen = null;
	}

	public override void _Process(double delta)
	{
		_host?.Poll();
		_client?.Poll();
	}

	void Refresh()
	{
		if (_host is not { } host || _client is not { } client) return;
		// (the player is in: the match starts at once)
		if (client.MyId != null && host.Room.Phase == Phase.Lobby) host.Start();
		if (client.State is not { Game: { } } state) return;
		if (_screen == null)
		{
			_stage.Clear();
			_screen = new PlayerGameScreen(client);
			_stage.AddChild(_screen);
		}
		_screen.Refresh(state);
		Actions(host);
	}

	/// <summary>What the host would do next: reveal, play the sound or video, show the next hint or slide, start again.</summary>
	void Actions(HostSession host)
	{
		_actions.Clear();
		if (host.Room.Game is not { } game) return;
		var q = game.Question;
		if (game.Stage == Stage.Open)
		{
			// (you buzzed in: as host you judge your own answer, like in a match)
			if (host.State.Game?.Buzzes.Any(b => b.Status == BuzzStatus.Answering) == true)
			{
				_actions.AddChild(U.Small("✓ Correct", () => host.Judge(true)));
				_actions.AddChild(U.Small("✗ Wrong", () => host.Judge(false)));
			}
			if (q.Media != null) _actions.AddChild(U.Small(game.MediaPlaying ? "⏸ Pause" : "▶ Play", () => host.SetMedia(!game.MediaPlaying)));
			if (q.Type == QuestionType.Steps && game.Hint < q.Hints.Count - 1) _actions.AddChild(U.Small("Next hint →", host.NextHint));
			if (q.Type == QuestionType.Picture && game.Slide < q.Slides.Count - 1) _actions.AddChild(U.Small("Next picture →", () => host.SetSlide(game.Slide + 1)));
			_actions.AddChild(U.Small("Reveal the answer", host.Reveal));
		}
		_actions.AddChild(U.Small("↺ Start again", () => Callable.From(Begin).CallDeferred()));
	}
}
