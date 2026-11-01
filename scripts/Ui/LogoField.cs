// The home screen's logo as a quiz answer field: "Quiznight?" in a text box with a blinking cursor. Every so often it
// deletes itself letter by letter and types an easter egg instead (Klaus? Mond? Autobahn? …), waits, and types
// "Quiznight?" back. Two spotlights sweep over it from above (Spotlights, drawn behind the page).

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
/// Two spotlights from above that sweep slowly across the top of the home screen and cross over the logo: soft cones,
/// bright at the lamp, fading out below. Drawn behind the page; the mouse goes through.
/// </summary>
public partial class Spotlights : Control
{
	double _t;
	/// <summary>Where the light lands, from the top (the logo's height on the screen).</summary>
	public float Target = 150;

	public Spotlights()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public override void _Process(double delta)
	{
		// (with less motion asked for, the lights stand still)
		if (!Announcer.ReduceMotion) _t += delta;
		QueueRedraw();
	}

	void Cone(Vector2 lamp, Vector2 aim, float width, Color color)
	{
		var dir = (aim - lamp).Normalized();
		var side = new Vector2(-dir.Y, dir.X);
		var far = lamp + dir * (aim - lamp).Length() * 1.35f;
		var bright = color;
		var faint = new Color(color, 0);
		DrawPolygon(new[] { lamp + side * 6, lamp - side * 6, far - side * width, far + side * width },
			new[] { bright, bright, faint, faint });
		// (the pool of light where it lands)
		for (var i = 0; i < 6; i++)
			DrawCircle(aim, width * 0.55f * (1 - i / 7f), new Color(color, color.A * 0.12f));
	}

	public override void _Draw()
	{
		var w = Size.X;
		// (two lamps at the top corners; they swing at different speeds, so their pools meet and part)
		var a = (float)Math.Sin(_t * 0.45) * 0.22f;
		var b = (float)Math.Sin(_t * 0.37 + 1.7) * 0.22f;
		var warm = new Color(C.Gold, 0.16f);
		var cool = new Color(C.Pink, 0.13f);
		Cone(new Vector2(w * 0.12f, -20), new Vector2(w * (0.5f + a), Target), 150, warm);
		Cone(new Vector2(w * 0.88f, -20), new Vector2(w * (0.5f + b), Target), 150, cool);
	}
}

/// <summary>
/// The experimental version (Settings → "Real spotlights"): the same two lamps as real 2D lights that cast shadows. Every
/// button, card and the logo on the page gets an occluder the size of its box, so the light falls past them and leaves
/// their shadows on the background. Godot has no ray tracing in 2D; its lights trace shadows per light from the occluders'
/// edges, which is what this uses. The occluders follow the page a few times a second (it hardly moves).
/// </summary>
public partial class LitSpotlights : Control
{
	double _t, _sinceOccluders = 1;
	public float Target = 150;
	/// <summary>The page whose boxes cast shadows.</summary>
	public Control? Page;
	readonly PointLight2D[] _lamps = new PointLight2D[2];
	readonly Node2D _occluders = new();
	static Texture2D? _cone;

	public LitSpotlights()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_occluders);
		var colours = new[] { new Color(C.Gold, 1), new Color(C.Pink, 1) };
		for (var i = 0; i < 2; i++)
		{
			_lamps[i] = new PointLight2D
			{
				Texture = _cone ??= ConeTexture(), TextureScale = 4f, Color = colours[i], Energy = 1.4f,
				BlendMode = Light2D.BlendModeEnum.Add, ShadowEnabled = true, ShadowFilter = Light2D.ShadowFilterEnum.Pcf5,
				ShadowFilterSmooth = 3, ShadowColor = new Color(0, 0, 0, 0),
				// (only the background takes shadows, ShadowLayer: the page's boxes are lit, not in their own shadow)
				ShadowItemCullMask = ShadowLayer,
				// (the texture's top middle is the lamp)
				Offset = new Vector2(0, 128),
			};
			AddChild(_lamps[i]);
		}
	}

	const int ShadowLayer = 2;

	/// <summary>The app's background (everything drawn before the screens) takes the page's shadows.</summary>
	public override void _Ready() => MarkBackground(true);

	public override void _ExitTree() => MarkBackground(false);

	void MarkBackground(bool on)
	{
		Node? screens = this;
		while (screens?.GetParent() is { } parent && parent != App.I) screens = parent;
		if (screens == null || App.I == null) return;
		static void Set(Node n, bool on)
		{
			if (n is CanvasItem item) item.LightMask = on ? item.LightMask | ShadowLayer : item.LightMask & ~ShadowLayer;
			foreach (var child in n.GetChildren()) Set(child, on);
		}
		foreach (var child in App.I.GetChildren())
		{
			if (child == screens) break;
			Set(child, on);
		}
	}

	/// <summary>A 256×256 cone of light pointing down from the top middle, bright near the lamp, soft at its edges.</summary>
	static ImageTexture ConeTexture()
	{
		const int n = 256;
		var img = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
		for (var y = 0; y < n; y++)
			for (var x = 0; x < n; x++)
			{
				var dx = (x - n / 2f) / n;
				var dy = (y + 1f) / n;
				var angle = Math.Abs(Math.Atan2(dx, dy));
				var edge = Math.Clamp((0.42 - angle) / 0.12, 0, 1);
				var fall = Math.Clamp(1 - dy * 0.85, 0, 1);
				var v = (float)(edge * edge * fall);
				img.SetPixel(x, y, new Color(v, v, v, 1));
			}
		return ImageTexture.CreateFromImage(img);
	}

	public override void _Process(double delta)
	{
		if (!Announcer.ReduceMotion) _t += delta;
		var w = Size.X;
		var a = (float)Math.Sin(_t * 0.45) * 0.22f;
		var b = (float)Math.Sin(_t * 0.37 + 1.7) * 0.22f;
		Aim(_lamps[0], new Vector2(w * 0.12f, -20), new Vector2(w * (0.5f + a), Target));
		Aim(_lamps[1], new Vector2(w * 0.88f, -20), new Vector2(w * (0.5f + b), Target));
		_sinceOccluders += delta;
		if (_sinceOccluders >= 0.25)
		{
			_sinceOccluders = 0;
			PlaceOccluders();
		}
	}

	static void Aim(PointLight2D lamp, Vector2 from, Vector2 to)
	{
		lamp.Position = from;
		lamp.Rotation = (to - from).Angle() - Mathf.Pi / 2;
	}

	/// <summary>An occluder per visible button, card and the logo (in this control's coordinates), reusing the ones there.</summary>
	void PlaceOccluders()
	{
		var rects = new List<Rect2>();
		void Walk(Node n)
		{
			if (n is Control { Visible: false }) return;
			// (not the page's own background panel: a box over most of the screen would put everything in its shadow)
			if (n is BaseButton or PanelContainer or LineEdit && n is Control c && c.Size.X > 8 && c.Size.Y > 8 && c.Size.X * c.Size.Y < Size.X * Size.Y * 0.35f)
			{
				rects.Add(new Rect2(c.GlobalPosition - GlobalPosition, c.Size * c.GetGlobalTransform().Scale));
				// (a card's own buttons are inside its shadow already)
				if (n is PanelContainer) return;
			}
			foreach (var child in n.GetChildren()) Walk(child);
		}
		if (Page != null && IsInstanceValid(Page)) Walk(Page);
		var have = _occluders.GetChildCount();
		for (var i = 0; i < Math.Max(have, rects.Count); i++)
		{
			if (i >= rects.Count)
			{
				_occluders.GetChild<LightOccluder2D>(i).Visible = false;
				continue;
			}
			var r = rects[i];
			var occ = i < have ? _occluders.GetChild<LightOccluder2D>(i) : new LightOccluder2D { Occluder = new OccluderPolygon2D() };
			if (i >= have) _occluders.AddChild(occ);
			occ.Visible = true;
			occ.Occluder.Polygon = new[] { r.Position, new Vector2(r.End.X, r.Position.Y), r.End, new Vector2(r.Position.X, r.End.Y) };
		}
	}
}
