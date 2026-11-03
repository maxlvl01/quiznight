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
		DrawHalf(0, panel, h);
		DrawHalf(w - panel, w, h);
	}

	/// <summary>One half as vertical slices whose colours run dark to light and back, so it reads as folded velvet.</summary>
	void DrawHalf(float x0, float x1, float h)
	{
		var slices = Folds * SlicesPerFold;
		var step = (x1 - x0) / slices;
		var points = new Vector2[4];
		var colors = new Color[4];
		for (var i = 0; i < slices; i++)
		{
			var left = x0 + i * step;
			colors[0] = colors[3] = Shade(i / (float)SlicesPerFold);
			colors[1] = colors[2] = Shade((i + 1) / (float)SlicesPerFold);
			points[0] = new Vector2(left, 0);
			points[1] = new Vector2(left + step + 0.5f, 0);
			points[2] = new Vector2(left + step + 0.5f, h);
			points[3] = new Vector2(left, h);
			DrawPolygon(points, colors);
		}
	}

	static Color Shade(float foldPosition) => Dark.Lerp(Light, 0.5f - 0.5f * Mathf.Cos(foldPosition * Mathf.Tau));
}
