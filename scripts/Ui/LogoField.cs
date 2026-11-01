// The home screen's logo as a quiz answer field: "Quiznight?" in a text box with a blinking cursor. Every so often it
// deletes itself letter by letter and types an easter egg instead (Klaus? Mond? Autobahn? …), waits, and types
// "Quiznight?" back. Two spotlights from the top corners shine on it (Spotlights, or LitSpotlights in Settings).

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace QuizNight.Ui;

public partial class LogoField : PanelContainer
{
	const string Logo = "Quiznight?";

	/// <summary>What the field types now and then (in a new random order each round).</summary>
	static readonly string[] Eggs =
	{
		"Klaus?", "Mond?", "Autobahn?", "Mars?", "Grownz?", "Tazor?", "Max?", "Phil?", "Lars?", "Rematch?", "Big Brain?", "Patty?", "Olf?", "Fußspuren?",
	};

	readonly Label _text;
	readonly Label _question;
	readonly ColorRect _caret;
	readonly int _size;
	string _shown = Logo;
	readonly Queue<string> _todo = new();
	double _wait;
	double _blink;
	readonly List<string> _deck = new();
	readonly StyleBoxFlat _box;
	double _pulse;
	bool _update;

	/// <summary>An update is out: the outline pulses between the accent and green (HomeScreen; the arrow installs it).</summary>
	public bool UpdateAvailable
	{
		get => _update;
		set
		{
			_update = value;
			// (the tooltip needs the mouse; the field otherwise lets it through)
			MouseFilter = value ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
			if (!value) _box.BorderColor = C.Accent;
		}
	}

	public LogoField() : this(64) { }

	public LogoField(int size)
	{
		_size = size;
		MouseFilter = MouseFilterEnum.Ignore;
		// (a dark field, or a light one in a light skin)
		var box = UiTheme.Box(C.Light ? C.Panel : C.Bg.Darkened(0.35f), 22, C.Accent, 4, size * 0.4f, size * 0.12f);
		box.BorderWidthBottom = 10;
		box.BorderColor = C.Accent;
		AddThemeStyleboxOverride("panel", box);
		_box = box;
		_text = U.Text("", size, C.Text, Fonts.Black).Raw();
		_question = U.Text("?", size, C.Accent, Fonts.Black).Raw();
		_caret = new ColorRect { Color = C.Accent, CustomMinimumSize = new Vector2(Mathf.Max(3, size / 16f), size * 0.95f), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		var row = U.H(2, _text, _question, U.Gap(4), _caret);
		row.Alignment = BoxContainer.AlignmentMode.Begin;
		AddChild(row);
		// (seasonal skins dress the logo up: snow and a Santa hat, pumpkins and a cobweb)
		if (Seasonal.On) AddChild(new LogoDecor(C.Skin));
		// (as wide as "Quiznight?", so the field doesn't jump while it types something shorter)
		var width = Fonts.Black.GetStringSize(Logo, HorizontalAlignment.Left, -1, size).X + size * 0.9f;
		CustomMinimumSize = new Vector2(width, 0);
		Show(Logo);
		_wait = 4 + Random.Shared.NextDouble() * 3;
		AccessibilityName = "Quiznight?";
		_text.AccessibilityName = _question.AccessibilityName = " ";
	}

	void Show(string text)
	{
		_shown = text;
		var q = text.EndsWith('?');
		_text.Text = q ? text[..^1] : text;
		_question.Visible = q;
	}

	/// <summary>The next easter egg (each once before any comes again).</summary>
	string NextEgg()
	{
		if (_deck.Count == 0) _deck.AddRange(Eggs.OrderBy(_ => Random.Shared.Next()));
		var egg = _deck[0];
		_deck.RemoveAt(0);
		return egg;
	}

	/// <summary>Plans "delete it all, type this": one step per letter.</summary>
	void Plan(string target)
	{
		for (var i = _shown.Length - 1; i >= 0; i--) _todo.Enqueue(_shown[..i]);
		for (var i = 1; i <= target.Length; i++) _todo.Enqueue(target[..i]);
	}

	static readonly Color UpdateGreen = new("#2ed573");

	public override void _Process(double delta)
	{
		if (_update)
		{
			// (with less motion asked for it stays green instead of pulsing)
			_pulse += delta;
			var f = Announcer.ReduceMotion ? 1f : 0.5f + 0.5f * Mathf.Sin((float)_pulse * 3.2f);
			_box.BorderColor = C.Accent.Lerp(UpdateGreen, f);
		}
		if (Announcer.ReduceMotion) return;
		// (the cursor blinks while it waits, stays on while it types)
		_blink += delta;
		_caret.Modulate = new Color(1, 1, 1, _todo.Count > 0 || (_blink % 1.0) < 0.55 ? 1 : 0);
		_wait -= delta;
		if (_wait > 0) return;
		if (_todo.Count > 0)
		{
			var next = _todo.Dequeue();
			var deleting = next.Length < _shown.Length;
			Show(next);
			// (deleting is quick, typing a little uneven, like a person)
			_wait = deleting ? 0.045 : 0.08 + Random.Shared.NextDouble() * 0.09;
			if (_todo.Count == 0) _wait = _shown == Logo ? 7 + Random.Shared.NextDouble() * 6 : 1.8;
			return;
		}
		Plan(_shown == Logo ? NextEgg() : Logo);
		_wait = 0;
	}
}

/// <summary>
/// Two spotlights from the top corners aimed at the logo (home screen and title screen): soft cones that sway a little
/// around it, ending in a round pool of light on the logo. Drawn behind the logo; the mouse goes through.
/// </summary>
public partial class Spotlights : Control
{
	double _t;
	/// <summary>What they light: the logo (its middle is where the light lands).</summary>
	public Control? Logo;

	public Spotlights()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		// (drawn over the logo and added to it, so the pool of light shows on the logo instead of behind its box)
		Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
	}

	/// <summary>The spotlights a screen gets: the real ones if switched on in Settings (or --real-spotlights), else drawn.</summary>
	public static Control For(Control logo) =>
		Storage.Settings.RealSpotlights || Array.IndexOf(OS.GetCmdlineUserArgs(), "--real-spotlights") >= 0
			? new LitSpotlights { Logo = logo }
			: new Spotlights { Logo = logo };

	public override void _Process(double delta)
	{
		// (with less motion asked for, the lights stand still)
		if (!Announcer.ReduceMotion) _t += delta;
		QueueRedraw();
	}

	/// <summary>Where each lamp hangs and where its light lands now, and how big the pool is (shared with LitSpotlights).</summary>
	internal static (Vector2 Lamp, Vector2 Aim)[] Beams(Control self, Control? logo, double t, out float pool)
	{
		var w = self.Size.X;
		var rect = logo != null && IsInstanceValid(logo) && logo.IsVisibleInTree()
			? new Rect2(logo.GlobalPosition - self.GlobalPosition, logo.Size * logo.GetGlobalTransform().Scale)
			: new Rect2(w / 2 - 200, 80, 400, 120);
		var c = rect.GetCenter();
		pool = Math.Max(rect.Size.X, rect.Size.Y) * 0.42f;
		// (they sway around the logo at different speeds, so their pools cross on it)
		var sway = rect.Size.X * 0.22f;
		return new[]
		{
			(new Vector2(w * 0.12f, -20), c + new Vector2((float)Math.Sin(t * 0.45) * sway, (float)Math.Sin(t * 0.7) * rect.Size.Y * 0.08f)),
			(new Vector2(w * 0.88f, -20), c + new Vector2((float)Math.Sin(t * 0.37 + 1.7) * sway, (float)Math.Sin(t * 0.6 + 0.8) * rect.Size.Y * 0.08f)),
		};
	}

	/// <summary>The round pool of light where a beam lands (also LitSpotlights': real lights barely show on the logo's dark box).</summary>
	internal static void Pool(CanvasItem on, Vector2 aim, float pool, Color color)
	{
		// (many thin steps, so it fades smoothly instead of in rings)
		for (var i = 0; i < 24; i++)
			on.DrawCircle(aim, pool * (1 - i / 25f), new Color(color, color.A * 0.055f));
	}

	void Cone(Vector2 lamp, Vector2 aim, float pool, Color color)
	{
		var dir = (aim - lamp).Normalized();
		var side = new Vector2(-dir.Y, dir.X);
		var faint = new Color(color, 0);
		// (the beam widens to the pool's size where it lands, and fades a little past it)
		DrawPolygon(new[] { lamp + side * 6, lamp - side * 6, aim - side * pool, aim + side * pool },
			new[] { color, color, new Color(color, color.A * 0.35f), new Color(color, color.A * 0.35f) });
		DrawPolygon(new[] { aim + side * pool, aim - side * pool, aim + dir * pool * 0.9f - side * pool * 1.05f, aim + dir * pool * 0.9f + side * pool * 1.05f },
			new[] { new Color(color, color.A * 0.35f), new Color(color, color.A * 0.35f), faint, faint });
		// (the round pool of light on the logo)
		Pool(this, aim, pool, color);
	}

	public override void _Draw()
	{
		var beams = Beams(this, Logo, _t, out var pool);
		Cone(beams[0].Lamp, beams[0].Aim, pool, new Color(C.Gold, 0.16f));
		Cone(beams[1].Lamp, beams[1].Aim, pool, new Color(C.Pink, 0.13f));
	}
}

/// <summary>
/// The experimental version (Settings → "Real spotlights"): the same two lamps aimed at the logo as real 2D lights. The
/// light ends in a round pool on the logo, and the logo throws its shadow onto what's behind it (only the logo: the
/// buttons and cards don't). Godot has no ray tracing in 2D; its lights trace shadows from occluders' edges, which is
/// what this uses.
/// </summary>
public partial class LitSpotlights : Control
{
	double _t;
	/// <summary>What they light and what throws the shadow.</summary>
	public Control? Logo;
	readonly PointLight2D[] _lamps = new PointLight2D[2];
	readonly LightOccluder2D _shadow = new() { Occluder = new OccluderPolygon2D() };
	static Texture2D? _cone;
	/// <summary>The cone texture's size, and where in it the pool's middle is (from the top).</summary>
	const int N = 256;
	const float PoolY = N * 0.78f;

	public LitSpotlights()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		// (its own drawing, the pools, is added over the logo like the drawn spotlights')
		Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		AddChild(_shadow);
		var colours = new[] { new Color(C.Gold, 1), new Color(C.Pink, 1) };
		for (var i = 0; i < 2; i++)
		{
			_lamps[i] = new PointLight2D
			{
				Texture = _cone ??= ConeTexture(), Color = colours[i], Energy = 3.2f,
				BlendMode = Light2D.BlendModeEnum.Add, ShadowEnabled = true, ShadowFilter = Light2D.ShadowFilterEnum.Pcf5,
				ShadowFilterSmooth = 3, ShadowColor = new Color(0, 0, 0, 0),
				// (they light only the logo and what's behind it, LitLayer: not the buttons and cards; only what's behind
				// the logo takes its shadow, ShadowLayer, so the logo itself is lit)
				RangeItemCullMask = LitLayer,
				ShadowItemCullMask = ShadowLayer,
				// (the texture's top middle is the lamp)
				Offset = new Vector2(0, N / 2f),
			};
			AddChild(_lamps[i]);
		}
	}

	const int ShadowLayer = 2;
	const int LitLayer = 4;

	/// <summary>Everything drawn before this (the app's background, a title screen's glow) takes the logo's shadow.</summary>
	public override void _Ready() => MarkBehind(true);

	public override void _ExitTree() => MarkBehind(false);

	void MarkBehind(bool on)
	{
		static void Set(Node n, bool on)
		{
			if (n is CanvasItem item) item.LightMask = on ? item.LightMask | ShadowLayer | LitLayer : item.LightMask & ~(ShadowLayer | LitLayer);
			foreach (var child in n.GetChildren()) Set(child, on);
		}
		// (each ancestor's children before the branch this is in: the background, the page)
		for (Node node = this; node.GetParent() is { } parent; node = parent)
		{
			foreach (var sibling in parent.GetChildren())
			{
				if (sibling == node) break;
				Set(sibling, on);
			}
			if (parent == App.I) break;
		}
		// (but the page itself isn't lit, only the logo: lit, and not in its own shadow)
		if (GetParent() is { } screen)
			foreach (var sibling in screen.GetChildren())
			{
				if (sibling == this) break;
				Set(sibling, false);
			}
		if (Logo != null && IsInstanceValid(Logo)) SetLit(Logo, on);
	}

	static void SetLit(Node n, bool on)
	{
		if (n is CanvasItem item) item.LightMask = on ? (item.LightMask | LitLayer) & ~ShadowLayer : item.LightMask & ~LitLayer;
		foreach (var child in n.GetChildren()) SetLit(child, on);
	}

	/// <summary>A cone of light pointing down from the top middle, widening into a round pool at PoolY, soft at its edges.</summary>
	static ImageTexture ConeTexture()
	{
		var img = Image.CreateEmpty(N, N, false, Image.Format.Rgba8);
		var poolR = N * 0.17f;
		for (var y = 0; y < N; y++)
			for (var x = 0; x < N; x++)
			{
				var dx = x - N / 2f;
				// (the beam: as wide as the pool where it lands, fainter than the pool)
				var halfWidth = 3 + (poolR - 3) * Math.Min(1f, (y + 1f) / PoolY);
				var edge = Math.Clamp((halfWidth - Math.Abs(dx)) / (halfWidth * 0.45f), 0, 1);
				var beam = edge * edge * (y <= PoolY ? 0.45 : 0.45 * Math.Clamp(1 - (y - PoolY) / (N - PoolY), 0, 1));
				// (the round pool)
				var d = new Vector2(dx, y - PoolY).Length() / poolR;
				var pool = Math.Clamp(1 - d, 0, 1);
				var v = (float)Math.Min(1, beam + pool * pool * 0.9);
				img.SetPixel(x, y, new Color(v, v, v, 1));
			}
		return ImageTexture.CreateFromImage(img);
	}

	public override void _Process(double delta)
	{
		if (!Announcer.ReduceMotion) _t += delta;
		var beams = Spotlights.Beams(this, Logo, _t, out var pool);
		for (var i = 0; i < 2; i++)
		{
			var (from, to) = beams[i];
			var lamp = _lamps[i];
			lamp.Position = from;
			lamp.Rotation = (to - from).Angle() - Mathf.Pi / 2;
			// (stretched so the pool lands on the logo, whatever the distance)
			lamp.TextureScale = (to - from).Length() / PoolY;
		}
		// (the logo's shadow, where the logo is now: it hardly moves)
		if (Logo != null && IsInstanceValid(Logo) && Logo.IsVisibleInTree())
		{
			var r = new Rect2(Logo.GlobalPosition - GlobalPosition, Logo.Size * Logo.GetGlobalTransform().Scale);
			_shadow.Visible = true;
			_shadow.Occluder.Polygon = new[] { r.Position, new Vector2(r.End.X, r.Position.Y), r.End, new Vector2(r.Position.X, r.End.Y) };
		}
		else _shadow.Visible = false;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var beams = Spotlights.Beams(this, Logo, _t, out var pool);
		Spotlights.Pool(this, beams[0].Aim, pool, new Color(C.Gold, 0.13f));
		Spotlights.Pool(this, beams[1].Aim, pool, new Color(C.Pink, 0.1f));
	}
}
