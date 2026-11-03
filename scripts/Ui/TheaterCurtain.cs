// The title screen's theatre curtain (TitleScreen): drawn in code, like the rest of the UI, and parted by IntroSequence.Start.

using Godot;

namespace QuizNight.Ui;

/// <summary>
/// A red velvet stage curtain over the whole screen, in two halves that overlap a little in the middle (no valance across
/// the top: Max preferred it without). Each half is a mesh of folds of uneven width and depth: lit ridges with a velvet
/// sheen, deep hollows, darker under the rod and at the hem, whose ridges hang a little lower than its hollows.
/// <see cref="Open"/> 0 is closed, 1 is drawn aside (bunched up at the two edges); while it opens, the bottom trails behind
/// the top, as heavy cloth does.
/// </summary>
public partial class TheaterCurtain : Control
{
	static readonly Color Deep = new("2a0410"), Mid = new("8e1530"), Sheen = new("dc4a63");
	const int Folds = 11;
	const int PerFold = 6;
	const int Rows = 14;
	const float Overlap = 10;
	const float ShadowW = 22;
	/// <summary>How far the bottom trails behind the top while opening (in Open units, at its most).</summary>
	const float Lag = 0.12f;
	/// <summary>How much higher a hollow's hem hangs than a ridge's.</summary>
	const float HemLift = 8;

	// (the folds' widths, depths and wobble, the same every time: a fixed seed)
	static readonly float[] Widths = new float[Folds], Depths = new float[Folds], Phases = new float[Folds];
	static readonly float TotalWidth;

	static TheaterCurtain()
	{
		var rng = new System.Random(7);
		for (var k = 0; k < Folds; k++)
		{
			Widths[k] = 0.7f + 0.6f * (float)rng.NextDouble();
			Depths[k] = 0.65f + 0.35f * (float)rng.NextDouble();
			Phases[k] = (float)rng.NextDouble() * Mathf.Tau;
			TotalWidth += Widths[k];
		}
	}

	float _open;

	/// <summary>0 closed … 1 open.</summary>
	public float Open
	{
		get => _open;
		set
		{
			_open = value;
			QueueRedraw();
		}
	}

	public TheaterCurtain()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	/// <summary>How open the curtain is at this height: the bottom lags behind the top while it moves.</summary>
	float OpenAt(float y, float h) => Mathf.Clamp(_open - Lag * Mathf.Sin(_open * Mathf.Pi) * (y / h) * (y / h), 0, 1);

	/// <summary>One half's width at this openness: half the screen closed, a narrow bunch at the edge open.</summary>
	static float HalfWidth(float w, float open) => w / 2 * Mathf.Lerp(1f, 0.07f, open);

	public override void _Draw()
	{
		var w = Size.X;
		var h = Size.Y;
		// (closed, the right half hangs a little over the left one, as real curtains overlap in the middle)
		DrawHalf(w, h, right: false, 0);
		DrawHalf(w, h, right: true, 5);
		// the right half's shadow on the left one, and a dark crease where they meet, following its edge down
		var clear = new Color(0, 0, 0, 0);
		var dark = new Color(0, 0, 0, 0.55f * (1 - _open * 0.6f));
		var crease = new Color(Deep.Darkened(0.4f), 0.9f);
		for (var r = 0; r < Rows; r++)
		{
			float y0 = h * r / Rows, y1 = h * (r + 1) / Rows;
			float e0 = w - HalfWidth(w, OpenAt(y0, h)), e1 = w - HalfWidth(w, OpenAt(y1, h));
			DrawPolygon(new[] { new Vector2(e0 - ShadowW, y0), new Vector2(e0, y0), new Vector2(e1, y1), new Vector2(e1 - ShadowW, y1) },
				new[] { clear, dark, dark, clear });
			DrawPolygon(new[] { new Vector2(e0 - 1, y0), new Vector2(e0 + 1, y0), new Vector2(e1 + 1, y1), new Vector2(e1 - 1, y1) },
				new[] { crease, crease, crease, crease });
		}
	}

	/// <summary>
	/// One half as a grid of quads: columns across the folds (counted from the inner edge, which ends on a lit ridge),
	/// rows down the screen. <paramref name="shift"/> starts the right half elsewhere in the fold pattern, so the two
	/// halves aren't mirror images.
	/// </summary>
	void DrawHalf(float w, float h, bool right, int shift)
	{
		const int cols = Folds * PerFold;
		var points = new Vector2[cols + 1, Rows + 1];
		var colors = new Color[cols + 1, Rows + 1];
		for (var c = 0; c <= cols; c++)
		{
			var f = c / (float)PerFold;
			var k = Mathf.Min((int)f, Folds - 1);
			var frac = f - k;
			var fold = (k + shift) % Folds;
			var across = 0f;
			for (var j = 0; j < k; j++) across += Widths[(j + shift) % Folds];
			across = (across + frac * Widths[fold]) / TotalWidth;
			// (1 on a ridge, 0 in a hollow; ridges are where one fold meets the next)
			var ridge = 0.5f + 0.5f * Mathf.Cos(frac * Mathf.Tau);
			var bottom = h - HemLift * (1 - ridge) * Depths[fold];
			for (var r = 0; r <= Rows; r++)
			{
				var y = bottom * r / Rows;
				var open = OpenAt(y, h);
				var inner = right ? w - HalfWidth(w, open) : HalfWidth(w, open) + Overlap * (1 - open);
				var outer = right ? w : 0;
				// (the folds sway a little down their length; the two edges stay straight)
				var wobble = c == 0 || c == cols ? 0 : Mathf.Sin(y / h * 3.4f + Phases[fold]) * 3f;
				points[c, r] = new Vector2(Mathf.Lerp(inner, outer, across) + wobble, y);
				colors[c, r] = Tone(ridge, Depths[fold]) * Light(y, h);
			}
		}
		var quad = new Vector2[4];
		var tint = new Color[4];
		for (var c = 0; c < cols; c++)
			for (var r = 0; r < Rows; r++)
			{
				(quad[0], quad[1], quad[2], quad[3]) = (points[c, r], points[c + 1, r], points[c + 1, r + 1], points[c, r + 1]);
				(tint[0], tint[1], tint[2], tint[3]) = (colors[c, r], colors[c + 1, r], colors[c + 1, r + 1], colors[c, r + 1]);
				DrawPolygon(quad, tint);
			}
	}

	/// <summary>Velvet: deep, narrow hollows, broad mid-red faces and a pinkish sheen on the ridges, softer on shallow folds.</summary>
	static Color Tone(float ridge, float depth)
	{
		var s = Mathf.Pow(ridge, 0.75f);
		var c = s < 0.6f ? Deep.Lerp(Mid, s / 0.6f) : Mid.Lerp(Sheen, (s - 0.6f) / 0.4f * depth);
		return Deep.Lerp(c, 0.55f + 0.45f * depth);
	}

	/// <summary>Darker under the rod at the top and along the heavy hem at the bottom.</summary>
	static Color Light(float y, float h)
	{
		var v = 1 - 0.4f * (1 - Mathf.SmoothStep(0, h * 0.18f, y)) - 0.3f * Mathf.SmoothStep(h - 40, h, y);
		return new Color(v, v, v);
	}
}
