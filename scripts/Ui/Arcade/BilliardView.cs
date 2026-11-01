// Billiard in the lobby's mini games: the host rolls the balls (ArcadeHost), this draws the table. On your turn, press on
// the table and pull back from the white ball like a slingshot: the line shows where it goes and how hard; let go to shoot.

using System;
using System.Linq;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Ui.Arcade;

public partial class BilliardView : Control
{
	readonly ArcadeLink _link;
	ArcadeFrame _frame;
	Vector2? _drag;

	static readonly Color Cloth = new("#1f7a4d"), CaromCloth = new("#2a5fa8"), Rail = new("#5a3a22");
	/// <summary>Pool balls 1–8 (9–15 are the same colours as stripes): yellow, blue, red, purple, orange, green, maroon, black.</summary>
	static readonly Color[] Pool =
	{
		Colors.White, new("#f5c518"), new("#2a5bd7"), new("#e0302a"), new("#6b3fa0"), new("#f07d1a"), new("#1f8a4c"), new("#8a2232"), new("#14151b"),
	};
	static readonly Color Yellow = new("#f5c518"), Red = new("#e0302a");

	/// <summary>A ball's colour, whether it's a stripe, and its number ("" for cue and carom balls).</summary>
	static (Color Colour, bool Stripe, string Number) Look(string mode, int i) => BilliardMode.Carom(mode)
		? (i == 0 ? Colors.White : i == 1 ? Yellow : Red, false, "")
		: i == 0 ? (Colors.White, false, "")
		: i <= 8 ? (Pool[i], false, i.ToString())
		: (Pool[i - 8], true, i.ToString());

	const float MaxPull = 0.5f;

	public BilliardView() : this(null!, new ArcadeFrame()) { }

	public BilliardView(ArcadeLink link, ArcadeFrame first)
	{
		_link = link;
		_frame = first;
		FocusMode = FocusModeEnum.All;
		CustomMinimumSize = new Vector2(0, 420);
	}

	/// <summary>Which billiard game this is (BilliardMode).</summary>
	public string Mode => _frame.Billiard?.Mode ?? _frame.Game;

	// The balls are played back a little behind the host (Delay), between the two frames around that moment, so they roll
	// smoothly however unevenly the frames arrive. Billiard is turn by turn, so a tenth of a second behind costs nothing.

	/// <summary>Recent frames with the host's clock; the oldest are dropped once playback is past them.</summary>
	readonly System.Collections.Generic.List<ArcadeFrame> _recent = new();
	/// <summary>Local clock minus the host's, from the fastest frame seen (the slow ones only waited longer on the way).</summary>
	double? _offset;
	const double Delay = 0.1;

	static double LocalNow => Time.GetTicksMsec() / 1000.0;

	public void Take(ArcadeFrame frame)
	{
		var offset = LocalNow - frame.Time;
		// (the smallest delay seen so far, creeping up slowly in case the clocks drift)
		_offset = _offset is { } o ? Math.Min(offset, o + 0.002) : offset;
		_frame = frame;
		_recent.Add(frame);
		if (_recent.Count > 30) _recent.RemoveAt(0);
		QueueRedraw();
	}

	/// <summary>Where ball i is shown now: between the frames around the playback moment (or the newest, at rest).</summary>
	Vector2 ShownAt(BilliardState newest, int i)
	{
		var at = new Vector2((float)newest.X[i], (float)newest.Y[i]);
		if (_offset is not { } offset || !newest.Moving && _recent.Count > 0 && LocalNow - offset - Delay > _recent[^1].Time) return at;
		var play = LocalNow - offset - Delay;
		for (var n = _recent.Count - 1; n > 0; n--)
		{
			var (a, b) = (_recent[n - 1], _recent[n]);
			if (a.Time > play || a.Billiard is not { } sa || b.Billiard is not { } sb) continue;
			// (a different table, e.g. racked after the lag: no in-between)
			if (sa.X.Count != sb.X.Count || i >= sa.X.Count || sa.In[i]) return at;
			var t = b.Time > a.Time ? (float)Math.Clamp((play - a.Time) / (b.Time - a.Time), 0, 1) : 1;
			return new Vector2((float)sa.X[i], (float)sa.Y[i]).Lerp(new Vector2((float)sb.X[i], (float)sb.Y[i]), t);
		}
		return at;
	}

	/// <summary>Whether ball i is shown as potted yet: once playback has reached the frame it went in with.</summary>
	bool ShownIn(BilliardState newest, int i)
	{
		if (!newest.In[i] || _offset is not { } offset) return newest.In[i];
		var play = LocalNow - offset - Delay;
		var before = _recent.LastOrDefault(f => f.Time <= play)?.Billiard;
		return before == null || before.X.Count != newest.X.Count || before.In[i];
	}

	/// <summary>True while playback hasn't caught up with the newest frame yet (keep drawing).</summary>
	bool PlayingBack => _offset is { } o && _recent.Count > 0 && LocalNow - o - Delay <= _recent[^1].Time;

	/// <summary>My side ("A" left, "B" right).</summary>
	string Me => _frame.A == _link.MyId ? "A" : "B";

	/// <summary>
	/// Whether I can shoot now: my turn with the table at rest, or in the lag (for the break) as long as I haven't shot
	/// (both shoot at the same time).
	/// </summary>
	bool MyTurn => _frame.Billiard is { } b && (b.Lag
		? !b.LagShot[Me == "A" ? 0 : 1]
		: !b.Moving && (b.Turn == "A" ? _frame.A : _frame.B) == _link.MyId);

	Rect2 Table()
	{
		var scale = Mathf.Min((Size.X - 40) / (float)BilliardState.W, (Size.Y - 90) / (float)BilliardState.H);
		var size = new Vector2((float)BilliardState.W, (float)BilliardState.H) * scale;
		return new Rect2(new Vector2((Size.X - size.X) / 2, 60), size);
	}

	/// <summary>Where the shooter's ball is (pool: the white; carom: their own cue ball).</summary>
	Vector2 White()
	{
		var t = Table();
		var b = _frame.Billiard!;
		var cue = b.Lag ? (Me == "A" ? 0 : 1) : BilliardRules.CueFor(b);
		return t.Position + new Vector2((float)b.X[cue], (float)b.Y[cue]) * (t.Size.X / (float)BilliardState.W);
	}

	public override void _GuiInput(InputEvent e)
	{
		if (!MyTurn) return;
		switch (e)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
				_drag = press.Position;
				break;
			case InputEventMouseMotion motion when _drag != null:
				_drag = motion.Position;
				QueueRedraw();
				break;
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _drag != null:
				var (angle, power) = Aim(release.Position);
				_drag = null;
				if (power > 0.03f) _link.Send(new ArcadeRequest { Kind = ArcadeRequest.Shot, Angle = angle, Power = power });
				QueueRedraw();
				break;
		}
	}

	/// <summary>Pulled back from the white ball: it goes the other way, the further the harder.</summary>
	(float Angle, float Power) Aim(Vector2 mouse)
	{
		var pull = White() - mouse;
		var tableUnits = pull.Length() / (Table().Size.X / (float)BilliardState.W);
		return (Mathf.Atan2(pull.Y, pull.X), Mathf.Clamp(tableUnits / MaxPull, 0, 1));
	}

	public override void _Process(double delta)
	{
		if (_frame.Billiard is { Moving: true } || PlayingBack) QueueRedraw();
	}

	public override void _Draw()
	{
		if (_frame.Billiard is not { } s) return;
		var t = Table();
		var k = t.Size.X / (float)BilliardState.W;
		DrawRect(t.Grow(14), Rail);
		DrawRect(t, s.Pockets ? Cloth : CaromCloth);
		if (s.Pockets)
			foreach (var (px, py) in BilliardState.PocketSpots) DrawCircle(t.Position + new Vector2((float)px, (float)py) * k, (float)BilliardState.Pocket * k * 0.85f, new Color("#0b0b0f"));
		var r = (float)BilliardState.R * k;
		for (var i = 0; i < s.X.Count; i++)
		{
			if (ShownIn(s, i)) continue;
			var at = t.Position + ShownAt(s, i) * k;
			// (the lag's two balls are both plain white)
			var (colour, stripe, number) = s.Lag ? (Colors.White, false, "") : Look(s.Mode, i);
			DrawCircle(at + new Vector2(1.5f, 2), r, new Color(0, 0, 0, 0.35f));
			DrawCircle(at, r, stripe ? Colors.White : colour);
			if (stripe) DrawRect(new Rect2(at.X - r * 0.95f, at.Y - r * 0.5f, r * 1.9f, r), colour);
			if (number != "")
			{
				DrawCircle(at, r * 0.5f, Colors.White);
				DrawString(Fonts.Bold, at + new Vector2(-r * 0.5f, r * 0.3f), number, HorizontalAlignment.Center, r, Mathf.Max(7, (int)(r * 0.75f)), new Color("#14151b"));
			}
			DrawCircle(at - Vector2.One * r * 0.4f, r * 0.25f, new Color(1, 1, 1, 0.4f));
		}
		if (_drag is { } mouse && MyTurn)
		{
			var (angle, power) = Aim(mouse);
			var white = White();
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			DrawLine(white, white + dir * (60 + 360 * power), new Color(1, 1, 1, 0.7f), 2);
			DrawLine(white, mouse, new Color(C.Gold, 0.8f), 4);
		}
		var font = Fonts.Black;
		string Side(string id, int score, bool turn) => $"{(turn && !s.Lag ? "▶ " : "")}{_link.Name(id)}  {score}";
		DrawString(font, new Vector2(t.Position.X, 34), Side(_frame.A, _frame.ScoreA, s.Turn == "A"), HorizontalAlignment.Left, t.Size.X / 2 - 10, 20, s.Turn == "A" ? C.Accent : C.Text);
		DrawString(font, new Vector2(t.Position.X + t.Size.X / 2 + 10, 34), Side(_frame.B, _frame.ScoreB, s.Turn == "B"), HorizontalAlignment.Right, t.Size.X / 2 - 10, 20, s.Turn == "B" ? C.Accent : C.Text);
		if (s.Lag)
		{
			// (the lag: whose ball is whose, and what it's for)
			for (var i = 0; i < 2; i++)
				DrawString(Fonts.Bold, t.Position + new Vector2((float)s.X[i], (float)s.Y[i]) * k + new Vector2(-60, -r - 6), _link.Name(i == 0 ? _frame.A : _frame.B), HorizontalAlignment.Center, 120, 12, C.Text);
			var lag = MyTurn ? Lang.T("Lag for the break: shoot your ball off the right cushion and back. Whoever stops closest to the left cushion breaks.")
				: s.LagShot.All(x => x) ? Lang.T("Who's closer?") : Lang.T("Waiting for the other shot …");
			DrawString(Fonts.Bold, new Vector2(t.Position.X, t.End.Y + 38), lag, HorizontalAlignment.Center, t.Size.X, 15, C.Muted);
			return;
		}
		var line = s.Moving ? "" : MyTurn ? Lang.T("Your shot") : Lang.F("{0}'s shot", _link.Name(s.Turn == "A" ? _frame.A : _frame.B));
		var extra = s.Mode switch
		{
			BilliardMode.Eight => BilliardRules.GroupOf(s, "A") is { Length: > 0 } a
				? Lang.F("{0}: {1} · {2}: {3}", _link.Name(_frame.A), Lang.T(a), _link.Name(_frame.B), Lang.T(BilliardRules.GroupOf(s, "B")))
				: Lang.T("Table open: the first ball you pot is your group"),
			BilliardMode.Nine => BilliardRules.Targets(s).FirstOrDefault() is var low and > 0 ? Lang.F("Hit the {0} first", low) : "",
			BilliardMode.Four => Lang.T("Your ball: white for the left player, yellow for the right. First to 20."),
			BilliardMode.Three => Lang.T("Your ball: white for the left player, yellow for the right. First to 3."),
			_ => Lang.F("{0} balls left", s.In.Skip(1).Count(i => !i)),
		};
		DrawString(Fonts.Bold, new Vector2(t.Position.X, t.End.Y + 38), line + (line != "" && extra != "" ? " · " : "") + extra, HorizontalAlignment.Center, t.Size.X, 15, C.Muted);
	}
}
