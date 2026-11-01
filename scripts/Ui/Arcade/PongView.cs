// Pong in the lobby's mini games: the host runs the ball (ArcadeHost), this draws it. Your own paddle follows your mouse
// (or ↑ ↓) straight away and goes to the host ~60 times a second.
//
// Over the internet the host's frames arrive late and unevenly, so the screen doesn't just show them: it plays the ball on
// itself with the same rules (PongRules.Step, predict: no serves or points, those come from the host), walls and paddles
// included, so the ball glides and bounces off your paddle the moment it meets it. Each frame from the host then becomes
// the new truth, but the difference to what's on screen fades out over a few hundredths of a second instead of making
// the ball jump. The other's paddle glides to where the host says, instead of hopping 60 times a second.

using System;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Ui.Arcade;

public partial class PongView : Control
{
	readonly ArcadeLink _link;
	ArcadeFrame _frame;
	/// <summary>The ball as this screen plays it on (from the host's last frame).</summary>
	PongState _ball;
	/// <summary>How far the shown ball is off the played one after a correction; fades to nothing.</summary>
	Vector2 _error;
	double _mine = PongState.H / 2;
	/// <summary>The other's paddle as shown: glides towards the host's.</summary>
	double _theirs = PongState.H / 2;
	double _sentMine = -1, _sendIn;
	readonly bool _left;

	/// <summary>A correction bigger than this (field units) is a new serve or a point: shown at once, not faded.</summary>
	const double Snap = 0.5;
	/// <summary>How quickly a correction fades (per second; 1/this ≈ the time it takes).</summary>
	const float Settle = 14;
	/// <summary>How quickly the other's paddle follows (per second).</summary>
	const float Follow = 20;
	const double StepSec = 1 / 240.0;

	public PongView() : this(null!, new ArcadeFrame()) { }

	public PongView(ArcadeLink link, ArcadeFrame first)
	{
		_link = link;
		_frame = first;
		_ball = Copy(first.Pong ?? new PongState());
		_left = first.A == link.MyId;
		_theirs = _left ? _ball.PaddleB : _ball.PaddleA;
		FocusMode = FocusModeEnum.All;
		CustomMinimumSize = new Vector2(0, 420);
	}

	static PongState Copy(PongState s) => new()
	{
		BallX = s.BallX, BallY = s.BallY, VelX = s.VelX, VelY = s.VelY, PaddleA = s.PaddleA, PaddleB = s.PaddleB, Wait = s.Wait, ServeTo = s.ServeTo,
	};

	public void Take(ArcadeFrame frame)
	{
		if (frame.Pong is not { } host) return;
		var scored = frame.ScoreA != _frame.ScoreA || frame.ScoreB != _frame.ScoreB;
		_frame = frame;
		var shown = new Vector2((float)_ball.BallX, (float)_ball.BallY) + _error;
		_ball = Copy(host);
		var gap = shown - new Vector2((float)host.BallX, (float)host.BallY);
		// (a serve or a point: straight there; otherwise the difference fades out)
		_error = scored || host.Wait > 0 || gap.Length() > Snap ? Vector2.Zero : gap;
		QueueRedraw();
	}

	Rect2 Field()
	{
		var scale = Mathf.Min(Size.X / (float)PongState.W, (Size.Y - 40) / (float)PongState.H);
		var size = new Vector2((float)PongState.W, (float)PongState.H) * scale;
		return new Rect2(new Vector2((Size.X - size.X) / 2, 40), size);
	}

	public override void _GuiInput(InputEvent e)
	{
		if (e is InputEventMouseMotion motion)
		{
			var f = Field();
			_mine = Math.Clamp((motion.Position.Y - f.Position.Y) / f.Size.Y * PongState.H, 0, PongState.H);
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Up)) _mine = Math.Max(0, _mine - 3.5 * delta);
		if (Input.IsKeyPressed(Key.Down)) _mine = Math.Min(PongState.H, _mine + 3.5 * delta);
		_sendIn -= delta;
		if (_sendIn <= 0 && Math.Abs(_mine - _sentMine) > 0.001)
		{
			_sendIn = 1 / 60.0;
			_sentMine = _mine;
			_link.Send(new ArcadeRequest { Kind = ArcadeRequest.Paddle, Value = _mine / PongState.H });
		}
		if (_frame.Pong is { } host)
		{
			var target = _left ? host.PaddleB : host.PaddleA;
			_theirs += (target - _theirs) * (1 - Math.Exp(-Follow * delta));
		}
		// (the ball on with the paddles as they're shown: mine where my mouse is, theirs where it glides)
		if (_left) (_ball.PaddleA, _ball.PaddleB) = (_mine, _theirs);
		else (_ball.PaddleA, _ball.PaddleB) = (_theirs, _mine);
		var dt = Math.Min(delta, 0.1);
		for (var t = 0.0; t < dt; t += StepSec) PongRules.Step(_ball, Math.Min(StepSec, dt - t), predict: true);
		_error *= Mathf.Exp(-Settle * (float)delta);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_frame.Pong == null) return;
		var f = Field();
		var k = f.Size.X / (float)PongState.W;
		Vector2 At(double x, double y) => f.Position + new Vector2((float)x, (float)y) * k;
		DrawRect(f, C.Well);
		for (var y = 0f; y < f.Size.Y; y += 18) DrawRect(new Rect2(f.Position.X + f.Size.X / 2 - 2, f.Position.Y + y, 4, 9), new Color(C.Muted, 0.4f));
		var paddle = new Vector2(0.08f * k, (float)PongState.PaddleH * k);
		DrawRect(new Rect2(At(PongState.PaddleX, _ball.PaddleA) - new Vector2(paddle.X, paddle.Y / 2), paddle), _left ? C.Accent : C.Text);
		DrawRect(new Rect2(At(PongState.W - PongState.PaddleX, _ball.PaddleB) - new Vector2(0, paddle.Y / 2), paddle), _left ? C.Text : C.Accent);
		if (_ball.Wait <= 0)
		{
			var x = _ball.BallX + _error.X;
			var y = Math.Clamp(_ball.BallY + _error.Y, PongState.Ball, PongState.H - PongState.Ball);
			// (past a paddle and out: the host's frame with the point is on its way)
			if (x > -0.2 && x < PongState.W + 0.2) DrawCircle(At(x, y), (float)PongState.Ball * k, C.Gold);
		}
		var font = Fonts.Black;
		DrawString(font, new Vector2(f.Position.X, 30), $"{_link.Name(_frame.A)}  {_frame.ScoreA}", HorizontalAlignment.Left, f.Size.X / 2 - 20, 22, C.Text);
		DrawString(font, new Vector2(f.Position.X + f.Size.X / 2 + 20, 30), $"{_frame.ScoreB}  {_link.Name(_frame.B)}", HorizontalAlignment.Right, f.Size.X / 2 - 20, 22, C.Text);
	}
}
