// The title screen's theatre curtain (TitleScreen): drawn in code, like the rest of the UI, and parted by IntroSequence.Start.

using Godot;

namespace QuizNight.Ui;

/// <summary>
/// A red velvet stage curtain over the whole screen: two folded halves (no valance across the top: Max preferred it without).
/// <see cref="Open"/> 0 is closed, 1 is drawn aside (bunched up at the two edges).
/// </summary>
public partial class TheaterCurtain : Control
{
	static readonly Color Dark = new("4a0716"), Light = new("a81d3c");
	const int Folds = 9;
	const int SlicesPerFold = 6;

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

	public override void _Draw()
	{
		var w = Size.X;
		var h = Size.Y;
		// (each half shrinks from half the screen to a narrow bunch at its edge, its folds packed tighter)
		var panel = w / 2 * Mathf.Lerp(1f, 0.07f, _open);
		// (closed, the right half hangs a little over the left one, as real curtains overlap in the middle)
		var overlap = Overlap * (1 - _open);
		DrawHalf(0, panel + overlap, h, inner: panel + overlap);
		DrawHalf(w - panel, w, h, inner: w - panel);
		// the right half's shadow on the left one, and a dark crease where they meet
		var edge = w - panel;
		var shadow = new[] { new Vector2(edge - ShadowW, 0), new Vector2(edge, 0), new Vector2(edge, h), new Vector2(edge - ShadowW, h) };
		var clear = new Color(0, 0, 0, 0);
		var dark = new Color(0, 0, 0, 0.55f * (1 - _open * 0.6f));
		DrawPolygon(shadow, new[] { clear, dark, dark, clear });
		DrawRect(new Rect2(edge - 1, 0, 2, h), new Color(Dark.Darkened(0.5f), 0.9f));
	}

	const float Overlap = 10;
	const float ShadowW = 22;

	/// <summary>
	/// One half as vertical slices whose colours run dark to light and back, so it reads as folded velvet. The folds are
	/// counted from the inner edge (the one in the middle), which ends on a lit fold, so each half's edge catches the light.
	/// </summary>
	void DrawHalf(float x0, float x1, float h, float inner)
	{
		var slices = Folds * SlicesPerFold;
		var step = (x1 - x0) / slices;
		var points = new Vector2[4];
		var colors = new Color[4];
		for (var i = 0; i < slices; i++)
		{
			var left = x0 + i * step;
			colors[0] = colors[3] = Shade(Mathf.Abs(left - inner) / step / SlicesPerFold + 0.5f);
			colors[1] = colors[2] = Shade(Mathf.Abs(left + step - inner) / step / SlicesPerFold + 0.5f);
			points[0] = new Vector2(left, 0);
			points[1] = new Vector2(left + step + 0.5f, 0);
			points[2] = new Vector2(left + step + 0.5f, h);
			points[3] = new Vector2(left, h);
			DrawPolygon(points, colors);
		}
	}

	static Color Shade(float foldPosition) => Dark.Lerp(Light, 0.5f - 0.5f * Mathf.Cos(foldPosition * Mathf.Tau));
}
