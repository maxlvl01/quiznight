// Reusable pieces: avatars, type icons, countdowns, images, number fields, steppers, dialogs.

using System;
using System.Globalization;
using System.Linq;
using Godot;
using QuizNight.Net;
using QuizNight.Shared;

namespace QuizNight.Ui;

/// <summary>The player's Steam picture in a circle ringed with their colour, or a coloured circle with their initials.</summary>
public partial class Avatar : Control
{
	readonly Color _color;
	readonly string _initials;
	readonly bool _offline;
	readonly string _id;

	public Avatar() : this(new PublicPlayer()) { }

	public Avatar(PublicPlayer player, float size = 30)
	{
		_color = new Color(player.Color);
		_initials = U.Initials(player.Name);
		_offline = !player.Connected;
		_id = player.Id;
		TextureFilter = TextureFilterEnum.LinearWithMipmaps;
		CustomMinimumSize = new Vector2(size, size);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		TooltipText = player.Name;
		AccessibilityName = player.Name;
		MouseFilter = MouseFilterEnum.Pass;
	}

	public override void _EnterTree() => SteamAvatars.Loaded += OnLoaded;
	public override void _ExitTree() => SteamAvatars.Loaded -= OnLoaded;
	void OnLoaded(string id)
	{
		if (id == _id) QueueRedraw();
	}

	public override void _Draw()
	{
		var r = Mathf.Min(Size.X, Size.Y) / 2;
		var center = Size / 2;
		var alpha = _offline ? 0.35f : 1f;
		DrawCircle(center, r, new Color(_color, alpha), true, -1, true);
		if (SteamAvatars.Get(_id) is { } picture)
		{
			// (the picture as a circle inside a thin ring of the player's colour)
			var inner = r - Mathf.Max(1.5f, r * 0.08f);
			const int n = 48;
			var points = new Vector2[n];
			var uvs = new Vector2[n];
			for (var i = 0; i < n; i++)
			{
				var dir = Vector2.FromAngle(Mathf.Tau * i / n);
				points[i] = center + dir * inner;
				uvs[i] = new Vector2(0.5f, 0.5f) + dir * 0.5f;
			}
			DrawPolygon(points, new[] { new Color(1, 1, 1, alpha) }, uvs, picture);
			return;
		}
		var fontSize = (int)(r * 0.8f);
		var font = Fonts.Bold;
		var ascent = font.GetAscent(fontSize);
		var height = font.GetHeight(fontSize);
		DrawString(font, new Vector2(0, center.Y - height / 2 + ascent), _initials, HorizontalAlignment.Center, Size.X, fontSize,
			new Color("#14151b", alpha));
	}
}

/// <summary>The little line drawing for each question type (same shapes as the web version's icons).</summary>
public partial class TypeIcon : Control
{
	readonly string _type;

	public TypeIcon() : this(QuestionType.Classic) { }

	public TypeIcon(string type, float size = 22)
	{
		_type = type;
		AccessibilityName = Lang.T(QuestionType.Infos.TryGetValue(type, out var info) ? info.Label : type);
		CustomMinimumSize = new Vector2(size, size);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		var k = Mathf.Min(Size.X, Size.Y) / 24f;
		var color = C.ForType(_type);
		var w = 1.8f * k;
		Vector2 P(float x, float y) => new(x * k, y * k);
		switch (_type)
		{
			case QuestionType.MapPin:
				DrawPolyline(new[] { P(12, 21), P(7, 15.5f), P(5, 10.5f), P(6, 6.5f), P(9, 3.4f), P(12, 2.5f), P(15, 3.4f), P(18, 6.5f), P(19, 10.5f), P(17, 15.5f), P(12, 21) }, color, w, true);
				DrawArc(P(12, 9.5f), 2.5f * k, 0, Mathf.Tau, 24, color, w, true);
				break;
			case QuestionType.Estimation:
				foreach (var y in new[] { 9f, 15f })
				{
					var pts = new Vector2[25];
					for (var i = 0; i <= 24; i++)
					{
						var x = 3 + i * 0.75f;
						pts[i] = P(x, y - Mathf.Sin((x - 3) / 6 * Mathf.Pi) * 1.6f);
					}
					DrawPolyline(pts, color, w, true);
				}
				break;
			case QuestionType.Classic:
			{
				// (a little arcade buzzer: a dome on a base, like the big BUZZ! button)
				var dome = new Vector2[21];
				for (var i = 0; i <= 20; i++)
				{
					var a = Mathf.Pi + Mathf.Pi * i / 20;
					dome[i] = P(12 + Mathf.Cos(a) * 7.5f, 15 + Mathf.Sin(a) * 9);
				}
				DrawColoredPolygon(dome, color);
				DrawPolyline(dome.Append(dome[0]).ToArray(), C.Ink, 1.4f * k, true);
				DrawRect(new Rect2(P(3, 15.5f), P(18, 4.5f)), C.Ink);
				DrawPolyline(new[] { P(8, 11.5f), P(9.2f, 8.6f), P(11.6f, 7.2f) }, new Color(1, 1, 1, 0.7f), 1.6f * k, true);
				break;
			}
			case QuestionType.Sound:
				DrawPolyline(new[] { P(9, 18), P(9, 5), P(20, 3), P(20, 16) }, color, w, true);
				DrawArc(P(6, 18), 3 * k, 0, Mathf.Tau, 20, color, w, true);
				DrawArc(P(17, 16), 3 * k, 0, Mathf.Tau, 20, color, w, true);
				break;
			case QuestionType.Video:
				DrawRect(new Rect2(P(3, 5), P(18, 14)), color, false, w, true);
				DrawPolyline(new[] { P(10, 9.5f), P(10, 14.5f), P(14.5f, 12), P(10, 9.5f) }, color, w, true);
				break;
			case QuestionType.Order:
				foreach (var y in new[] { 6f, 12f, 18f }) DrawLine(P(3, y), P(14, y), color, w, true);
				DrawPolyline(new[] { P(19, 4), P(19, 20) }, color, w, true);
				DrawPolyline(new[] { P(16, 17), P(19, 20.5f), P(22, 17) }, color, w, true);
				break;
			case QuestionType.Write:
				DrawPolyline(new[] { P(4, 20), P(5, 15), P(16, 4), P(20, 8), P(9, 19), P(4, 20) }, color, w, true);
				DrawLine(P(14, 6), P(18, 10), color, w, true);
				break;
			case QuestionType.Picture:
				// (two pictures, one behind the other: a slideshow; lines like the text of a review)
				DrawPolyline(new[] { P(7, 6), P(7, 3), P(21, 3), P(21, 14), P(18, 14) }, color, w, true);
				DrawRect(new Rect2(P(3, 7), P(15, 13)), color, false, w, true);
				DrawLine(P(6, 11), P(15, 11), color, w, true);
				DrawLine(P(6, 15), P(12, 15), color, w, true);
				break;
			case QuestionType.Steps:
				// (steps going up)
				DrawPolyline(new[] { P(3, 20), P(3, 16), P(8, 16), P(8, 11), P(13, 11), P(13, 6), P(18, 6), P(18, 2.5f) }, color, w, true);
				DrawLine(P(3, 20), P(21, 20), color, w, true);
				break;
			case QuestionType.Interactive:
				// (a microphone)
				DrawRect(new Rect2(P(9, 2.5f), P(6, 11)), color, false, w, true);
				DrawArc(P(12, 11), 6 * k, 0, Mathf.Pi, 20, color, w, true);
				DrawLine(P(12, 17), P(12, 21), color, w, true);
				DrawLine(P(8, 21), P(16, 21), color, w, true);
				break;
			case QuestionType.Image:
				DrawRect(new Rect2(P(3, 4), P(18, 16)), color, false, w, true);
				DrawPolyline(new[] { P(4, 18), P(10, 11), P(14, 15), P(16, 13), P(20, 17) }, color, w, true);
				DrawArc(P(16, 8), 1.8f * k, 0, Mathf.Tau, 16, color, w, true);
				break;
		}
	}
}

public static class Chips
{
	public static string TypeLabel(PublicQuestion q) =>
		q.Type == QuestionType.Classic ? (q.Mode == ClassicMode.Choice ? "A B C D" : q.Mode == ClassicMode.Multi ? "Multiple choice" : "Buzzer") : q.IsTimeline ? "Timeline" : QuestionType.Infos[q.Type].Label;

	/// <summary>"(icon) Buzzer" pill for the current question.</summary>
	public static Control Question(PublicQuestion q) =>
		U.Panel(U.H(6, new TypeIcon(q.Type, 18), U.Text(TypeLabel(q), 14, C.ForType(q.Type), Fonts.Bold)), "Pill")
			.With(p => p.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter);

	/// <summary>A player name chip with avatar.</summary>
	public static PanelContainer Player(PublicPlayer p, string? suffix = null, bool me = false, bool dim = false)
	{
		var row = U.H(6, new Avatar(p, 24), U.Text(p.Name, 15).Raw());
		if (suffix != null) row.AddChild(U.Muted(suffix, 13, false));
		var chip = U.Panel(row, me ? "ChipMe" : "Chip");
		if (dim || !p.Connected) chip.Modulate = new Color(1, 1, 1, 0.55f);
		return chip;
	}
}

/// <summary>"0:12" countdown to a host-time deadline; turns red in the last 5 seconds.</summary>
public partial class Countdown : HBoxContainer
{
	long? _endsAt;
	/// <summary>Where the deadline comes from, read every frame (the host's "+5 s" moves it without rebuilding the screen).</summary>
	public Func<long?>? Follow;
	readonly Label _time;
	readonly Color? _ink;

	public Countdown() : this(null) { }

	/// <param name="ink">Text colour on a coloured panel (e.g. the orange "Your turn!"), instead of the usual light/muted.</param>
	public Countdown(long? endsAt, string? label = null, int size = 26, Color? ink = null)
	{
		_endsAt = endsAt;
		_ink = ink;
		AddThemeConstantOverride("separation", 8);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		if (label != null) AddChild(U.Text(label, 15, ink ?? C.Muted, Fonts.Bold).With(l => l.SizeFlagsVertical = SizeFlags.ShrinkCenter));
		_time = U.Mono("", size);
		AddChild(_time);
		Visible = endsAt != null;
		Update();
	}

	void Update()
	{
		if (Follow != null)
		{
			_endsAt = Follow();
			Visible = _endsAt != null;
		}
		if (ServerClock.SecondsLeft(_endsAt) is not { } left) return;
		var s = (int)Math.Ceiling(left);
		_time.Text = $"{s / 60}:{s % 60:00}";
		_time.AddThemeColorOverride("font_color", _ink ?? (s <= 5 ? C.Coral : C.Text));
	}

	public override void _Process(double delta) => Update();
}

/// <summary>Shows a texture as large as fits (keeping its shape), up to a maximum height.</summary>
public partial class FitImage : Control
{
	Texture2D? _texture;
	float _maxHeight = 420;
	/// <summary>Tallest it gets; U.Flexible: as tall as the space left.</summary>
	public float MaxHeight
	{
		get => _maxHeight;
		set
		{
			_maxHeight = value;
			if (value <= 0) this.MarkFlexible();
			UpdateHeight();
		}
	}
	/// <summary>The least a flexible picture shrinks to (maps keep U.FlexMinHeight: they need room to pin).</summary>
	public const float PictureMinHeight = 100;
	/// <summary>Aspect (height / width) to reserve space with while the texture is still loading.</summary>
	public float PlaceholderAspect = 0.5625f;

	public FitImage() => SizeFlagsHorizontal = SizeFlags.ExpandFill;

	public Texture2D? Texture
	{
		get => _texture;
		set
		{
			_texture = value;
			UpdateHeight();
			QueueRedraw();
		}
	}

	float Aspect => _texture != null ? (float)_texture.GetHeight() / _texture.GetWidth() : PlaceholderAspect;

	void UpdateHeight()
	{
		if (MaxHeight <= 0) this.MarkFlexible();
		// (a flexible picture gives way down to PictureMinHeight, so a long question with four answers still fits
		// without scrolling; a click shows it big anyway)
		var h = MaxHeight > 0 ? U.FitHeight(MaxHeight, Size.X * Aspect) : Mathf.Min(PictureMinHeight, Size.X * Aspect);
		if (Mathf.Abs(CustomMinimumSize.Y - h) > 0.5f) CustomMinimumSize = new Vector2(0, h);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized) UpdateHeight();
	}

	public override void _Draw()
	{
		var w = Mathf.Min(Size.X, Size.Y / Aspect);
		var h = w * Aspect;
		var rect = new Rect2((Size.X - w) / 2, (Size.Y - h) / 2, w, h);
		if (_texture == null)
		{
			DrawRect(rect, C.Panel2);
			var font = Fonts.Regular;
			DrawString(font, new Vector2(0, Size.Y / 2), Lang.T("Loading image…"), HorizontalAlignment.Center, Size.X, 14, C.Muted);
			return;
		}
		DrawTextureRect(_texture, rect, false);
	}
}

/// <summary>An image that comes from an AssetSource (players load it from the host).</summary>
public partial class AssetImage : FitImage
{
	readonly AssetSource? _source;
	readonly string _key;

	public AssetImage() : this(null, "") { }

	public AssetImage(AssetSource? source, string key, float maxHeight = 420)
	{
		_source = source;
		_key = key;
		MaxHeight = maxHeight;
		if (source == null) return;
		Texture = source.Get(key);
		if (Texture == null) source.Loaded += OnLoaded;
	}

	/// <summary>A click shows the picture as big as the window allows (e.g. a map question's screenshot).</summary>
	public bool Zoomable
	{
		get => _zoomable;
		set
		{
			_zoomable = value;
			MouseDefaultCursorShape = value ? CursorShape.PointingHand : CursorShape.Arrow;
			TooltipText = value ? Lang.T("Click to see it bigger") : "";
		}
	}
	bool _zoomable;

	public override void _GuiInput(InputEvent e)
	{
		if (!_zoomable || Texture == null || e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
		ImagePopup.Open(Texture);
		AcceptEvent();
	}

	void OnLoaded(string key)
	{
		if (key != _key || !IsInstanceValid(this)) return;
		Texture = _source!.Get(key);
	}

	public override void _ExitTree()
	{
		if (_source != null) _source.Loaded -= OnLoaded;
	}
}

/// <summary>A box whose content is rebuilt only when its key changes (keeps inputs and zoom alive between updates).</summary>
public partial class Keyed : VBoxContainer
{
	string? _key;

	public void Set(string key, Func<Control?> build)
	{
		if (key == _key) return;
		_key = key;
		this.Clear();
		SizeFlagsVertical = SizeFlags.Fill;
		if (build() is { } c)
		{
			AddChild(c);
			if (U.PropagateGrow(c)) SizeFlagsVertical = SizeFlags.ExpandFill;
		}
	}

	/// <summary>Forces the next Set to rebuild.</summary>
	public void Reset() => _key = null;
}

/// <summary>Number input that lets you type freely ("-", "1.") and reports a number or null.</summary>
public partial class NumberField : LineEdit
{
	readonly double? _min;
	readonly double? _max;
	readonly bool _integer;
	readonly Action<double?> _changed;
	public double? Value { get; private set; }

	public NumberField() : this(null, _ => { }) { }

	public NumberField(double? value, Action<double?> changed, double? min = null, double? max = null, bool integer = false, string placeholder = "")
	{
		Value = value;
		_changed = changed;
		_min = min;
		_max = max;
		_integer = integer;
		Text = Format(value);
		PlaceholderText = placeholder;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		VirtualKeyboardType = integer ? VirtualKeyboardTypeEnum.Number : VirtualKeyboardTypeEnum.NumberDecimal;
		TextChanged += OnText;
		FocusExited += Clamp;
		TextSubmitted += _ => Clamp();
	}

	// (every digit typed after the point; a comma in German, though both a dot and a comma are read)
	static string Format(double? v) => v is { } n
		? n.ToString("0.###############", TranslationServer.GetLocale() == "de" ? CultureInfo.GetCultureInfo("de-DE") : CultureInfo.InvariantCulture)
		: "";

	public static double? Parse(string raw)
	{
		var t = raw.Trim().Replace(',', '.').Replace(" ", "");
		return t != "" && double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : null;
	}

	void OnText(string raw)
	{
		Value = Parse(raw);
		_changed(Value);
	}

	/// <summary>The field between [−] and [+] buttons that count it down/up by one (it can still be typed in).</summary>
	public HBoxContainer WithSteps()
	{
		void Step(int by)
		{
			var n = (Value ?? _min ?? 0) + by;
			if (_min is { } min) n = Math.Max(min, n);
			if (_max is { } max) n = Math.Min(max, n);
			Value = n;
			Text = Format(n);
			_changed(n);
		}
		SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		Alignment = HorizontalAlignment.Center;
		// (room for two digits, not the usual four characters)
		AddThemeConstantOverride("minimum_character_width", 2);
		return U.H(6, U.Small("−", () => Step(-1)).MinSize(40), this, U.Small("+", () => Step(1)).MinSize(40));
	}

	void Clamp()
	{
		if (Value is not { } v) return;
		var n = _integer ? Math.Round(v) : v;
		if (_min is { } min) n = Math.Max(min, n);
		if (_max is { } max) n = Math.Min(max, n);
		if (n != v)
		{
			Value = n;
			_changed(n);
		}
		var caret = CaretColumn;
		Text = Format(n);
		CaretColumn = Math.Min(caret, Text.Length);
	}
}

/// <summary>"Label  [−] 8 [+]".</summary>
public static class Stepper
{
	public static HBoxContainer Make(string label, int value, int min, int max, Action<int> changed, string suffix = "")
	{
		var minus = U.Small("−", () => changed(value - 1));
		minus.Disabled = value <= min;
		var plus = U.Small("+", () => changed(value + 1));
		plus.Disabled = value >= max;
		var shown = U.Mono($"{value}{suffix}", 18).With(l => l.HorizontalAlignment = HorizontalAlignment.Center).MinSize(64);
		return U.H(8, U.Text(label, 15).Grow(), minus, shown, plus);
	}
}

public static class Dialogs
{
	/// <summary>Asks a yes/no question; runs onYes only on yes.</summary>
	/// <param name="dontAskAgain">With it, a "Don't ask me again" box; ticked and confirmed, this runs too.</param>
	/// <param name="option">With it, a box of its own (ticked to start with); confirmed, optionChosen hears whether it's still ticked.</param>
	public static void Confirm(Node owner, string text, string yes, Action onYes, string title = "Are you sure?", Action? dontAskAgain = null,
		string? option = null, Action<bool>? optionChosen = null)
	{
		var d = new ConfirmationDialog { Title = Lang.T(title), DialogText = Lang.T(text), OkButtonText = Lang.T(yes), CancelButtonText = Lang.T("Cancel"), DialogAutowrap = true };
		d.MinSize = new Vector2I(460, 0);
		if (dontAskAgain != null || option != null)
		{
			// (a dialog's own text and an added control would sit on top of each other: both go in one column instead)
			d.DialogText = "";
			var question = new Label { Text = Lang.T(text), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(420, 0) };
			var column = U.V(12, question);
			if (option != null)
			{
				var extra = new CheckBox { Text = Lang.T(option), ButtonPressed = true, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
				column.AddChild(extra);
				d.Confirmed += () => optionChosen?.Invoke(extra.ButtonPressed);
			}
			if (dontAskAgain != null)
			{
				var box = new CheckBox { Text = Lang.T("Don't ask me again"), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
				column.AddChild(box);
				d.Confirmed += () =>
				{
					if (box.ButtonPressed) dontAskAgain();
				};
			}
			d.AddChild(column);
		}
		d.Confirmed += () => onYes();
		d.Confirmed += d.QueueFree;
		d.Canceled += d.QueueFree;
		owner.AddChild(d);
		d.PopupCentered();
	}

	public static AcceptDialog Info(Node owner, string text, string title = "Quiznight?")
	{
		var d = new AcceptDialog { Title = Lang.T(title), DialogText = Lang.T(text), DialogAutowrap = true };
		d.MinSize = new Vector2I(460, 0);
		d.Confirmed += d.QueueFree;
		d.Canceled += d.QueueFree;
		owner.AddChild(d);
		d.PopupCentered();
		return d;
	}

	/// <summary>"Please wait…" (no OK button) while something happens; Close it when done.</summary>
	public static AcceptDialog Wait(Node owner, string text, string title = "Quiznight?")
	{
		var d = Info(owner, text, title);
		d.GetOkButton().Visible = false;
		return d;
	}

	public static void Close(AcceptDialog d)
	{
		if (GodotObject.IsInstanceValid(d)) d.QueueFree();
	}

	/// <summary>The system's file dialog for opening a file.</summary>
	public static void OpenFile(Node owner, string title, string[] filters, Action<string> picked)
	{
		var d = new FileDialog
		{
			Title = Lang.T(title),
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			Filters = filters,
			UseNativeDialog = true,
		};
		d.FileSelected += path => picked(path);
		d.FileSelected += _ => d.QueueFree();
		d.Canceled += d.QueueFree;
		owner.AddChild(d);
		d.PopupCentered(new Vector2I(900, 600));
	}

	public static void SaveFile(Node owner, string title, string fileName, string[] filters, Action<string> picked)
	{
		var d = new FileDialog
		{
			Title = Lang.T(title),
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Access = FileDialog.AccessEnum.Filesystem,
			Filters = filters,
			CurrentFile = fileName,
			UseNativeDialog = true,
		};
		d.FileSelected += path => picked(path);
		d.FileSelected += _ => d.QueueFree();
		d.Canceled += d.QueueFree;
		owner.AddChild(d);
		d.PopupCentered(new Vector2I(900, 600));
	}
}

/// <summary>A grid with a thin line between its rows (stats tables), so a row reads across even when it's sorted.</summary>
public partial class LinedGrid : GridContainer
{
	public override void _Notification(int what)
	{
		if (what == NotificationSortChildren) QueueRedraw();
	}

	public override void _Draw()
	{
		// (the line sits in the middle of the gap above each row's first cell; none above the heading row)
		var gap = GetThemeConstant("v_separation");
		var color = C.Border with { A = 0.45f };
		var cells = GetChildren();
		for (var i = Columns; i < cells.Count; i += Columns)
			if (cells[i] is Control c && c.Visible)
			{
				var y = Mathf.Round(c.Position.Y - gap / 2f);
				DrawLine(new Vector2(0, y), new Vector2(Size.X, y), color, 1);
			}
	}
}

/// <summary>
/// The Fast Pass joker's lightning bolt, drawn in the party show style (filled, ink outline) instead of the ⚡ emoji,
/// which looked out of place. As a texture for buttons, or a small picture for pills and rows.
/// </summary>
public static class Bolt
{
	const string Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M14.5 1.5 3.5 14h7l-2 8.5 12-13.5h-7.2z' "
		+ "fill='#{0}' stroke='#{1}' stroke-width='1.8' stroke-linejoin='round'/></svg>";

	static readonly System.Collections.Generic.Dictionary<(int, Color), Texture2D?> Cache = new();

	/// <summary>The bolt, size × size (null in headless runs, which have no renderer for textures).</summary>
	public static Texture2D? Icon(int size, Color? color = null)
	{
		var c = color ?? C.Gold;
		if (Cache.TryGetValue((size, c), out var cached)) return cached;
		Texture2D? texture = null;
		if (DisplayServer.GetName() != "headless")
		{
			var image = new Image();
			// (drawn at 2× and scaled down, for smooth edges at the canvas_items stretch)
			if (image.LoadSvgFromString(string.Format(Svg, c.ToHtml(false), C.Ink.ToHtml(false)), size * 2 / 24f) == Error.Ok)
				texture = ImageTexture.CreateFromImage(image);
		}
		Cache[(size, c)] = texture;
		return texture;
	}

	/// <summary>The bolt as a control (for pills and rows).</summary>
	public static Control Rect(int size, Color? color = null) => new TextureRect
	{
		Texture = Icon(size, color),
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		CustomMinimumSize = new Vector2(size, size),
		SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		MouseFilter = Control.MouseFilterEnum.Ignore,
	};

	/// <summary>A joker's pill: "×2 Double points", or the bolt and "Fast Pass" (just the icon with <paramref name="iconOnly"/>).</summary>
	public static Control JokerPill(string kind, Color color, bool iconOnly = false)
	{
		if (kind != Joker.FastPass) return U.Pill(iconOnly ? "×2" : Lang.T(Joker.Label(kind)), color);
		var row = U.H(4, Rect(16, color == C.Muted ? C.Muted : C.Gold));
		if (!iconOnly) row.AddChild(U.Text(Lang.T(Joker.Label(kind)), 14, color, Fonts.Bold));
		return U.Panel(row, "Pill").With(p => p.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter);
	}
}

/// <summary>
/// Small drawn icons for buttons and notices, instead of emoji (which come out as colour pictures from another app and
/// don't fit the party show look; Max's rule since 0.13.3b). Drawn white and coloured where they're used: on a button
/// they take its text colour (every skin and button kind), next to a label the label's colour.
/// </summary>
public static class Glyph
{
	public enum Kind { Trophy, Gamepad, Folder, Camera, Eye, Dice, Magnifier, Hourglass, Sun, Warning, Pause, Play, Info, Refresh }

	// (24 × 24, white; holes by even-odd, so one path can carry the details)
	static string Body(Kind kind) => kind switch
	{
		Kind.Trophy => "<path fill-rule='evenodd' d='M6 2.5h12V5h3.2a.8.8 0 0 1 .8.8v2.4a5.2 5.2 0 0 1-4.9 5.2A6.2 6.2 0 0 1 13.2 16.4V18.5H16a1 1 0 0 1 1 1V22H7v-2.5a1 1 0 0 1 1-1h2.8v-2.1A6.2 6.2 0 0 1 6.9 13.4 5.2 5.2 0 0 1 2 8.2V5.8A.8.8 0 0 1 2.8 5H6zM4 7v1.2a3.2 3.2 0 0 0 2.3 3.1A6.4 6.4 0 0 1 6 9.3V7zm16 0h-2v2.3a6.4 6.4 0 0 1-.3 2A3.2 3.2 0 0 0 20 8.2z'/>",
		Kind.Gamepad => "<path fill-rule='evenodd' d='M7 6h10a5.5 5.5 0 0 1 5.5 5.5v3.3a3.6 3.6 0 0 1-6.4 2.3L14.6 15.3H9.4l-1.5 1.8A3.6 3.6 0 0 1 1.5 14.8v-3.3A5.5 5.5 0 0 1 7 6zM6.3 8.8v1.9H4.4v2h1.9v1.9h2v-1.9h1.9v-2H8.3V8.8zM16.2 8.9a1.25 1.25 0 1 0 0 2.5 1.25 1.25 0 0 0 0-2.5zM18.6 11.6a1.25 1.25 0 1 0 0 2.5 1.25 1.25 0 0 0 0-2.5z'/>",
		Kind.Folder => "<path fill-rule='evenodd' d='M2 5.5A1.5 1.5 0 0 1 3.5 4h5.3l2.2 2.2h9.5A1.5 1.5 0 0 1 22 7.7v11.3a1.5 1.5 0 0 1-1.5 1.5h-17A1.5 1.5 0 0 1 2 19zM2 9.2h20v1.6H2z'/>",
		Kind.Camera => "<path fill-rule='evenodd' d='M3.2 7H7l1.6-2.6h6.8L17 7h3.8A1.2 1.2 0 0 1 22 8.2v10.6a1.2 1.2 0 0 1-1.2 1.2H3.2A1.2 1.2 0 0 1 2 18.8V8.2A1.2 1.2 0 0 1 3.2 7zM12 8.6a4.9 4.9 0 1 0 0 9.8 4.9 4.9 0 0 0 0-9.8zM12 10.6a2.9 2.9 0 1 1 0 5.8 2.9 2.9 0 0 1 0-5.8z'/>",
		Kind.Eye => "<path fill-rule='evenodd' d='M1.5 12C4 7.2 7.8 5 12 5s8 2.2 10.5 7c-2.5 4.8-6.3 7-10.5 7S4 16.8 1.5 12zM12 7.6a4.4 4.4 0 1 0 0 8.8 4.4 4.4 0 0 0 0-8.8zM12 10a2 2 0 1 1 0 4 2 2 0 0 1 0-4z'/>",
		Kind.Dice => "<path fill-rule='evenodd' d='M5.5 2.5h13a3 3 0 0 1 3 3v13a3 3 0 0 1-3 3h-13a3 3 0 0 1-3-3v-13a3 3 0 0 1 3-3zM7.8 5.9a1.9 1.9 0 1 0 0 3.8 1.9 1.9 0 0 0 0-3.8zM16.2 5.9a1.9 1.9 0 1 0 0 3.8 1.9 1.9 0 0 0 0-3.8zM12 10.1a1.9 1.9 0 1 0 0 3.8 1.9 1.9 0 0 0 0-3.8zM7.8 14.3a1.9 1.9 0 1 0 0 3.8 1.9 1.9 0 0 0 0-3.8zM16.2 14.3a1.9 1.9 0 1 0 0 3.8 1.9 1.9 0 0 0 0-3.8z'/>",
		Kind.Magnifier => "<path fill-rule='evenodd' d='M10 2a8 8 0 1 0 0 16 8 8 0 0 0 0-16zM10 5a5 5 0 1 1 0 10 5 5 0 0 1 0-10z'/><path d='M15.4 17.6l2.2-2.2 4.9 4.9a1.55 1.55 0 0 1-2.2 2.2z'/>",
		Kind.Hourglass => "<path fill-rule='evenodd' d='M5 2h14v2.2h-1.4c-.2 3.3-2 5.4-4.2 7.8 2.2 2.4 4 4.5 4.2 7.8H19V22H5v-2.2h1.4c.2-3.3 2-5.4 4.2-7.8C8.4 9.6 6.6 7.5 6.4 4.2H5zM8.6 4.2c.2 2.5 1.6 4.2 3.4 6.2 1.8-2 3.2-3.7 3.4-6.2zM12 13.6c-1.6 1.8-2.9 3.4-3.3 5.6h6.6c-.4-2.2-1.7-3.8-3.3-5.6z'/><path d='M9.6 6.8h4.8c-.6 1.1-1.4 2-2.4 3.1-1-1.1-1.8-2-2.4-3.1zM12 15.6c1 1.1 1.9 2.2 2.3 3.6H9.7c.4-1.4 1.3-2.5 2.3-3.6z'/>",
		Kind.Sun => "<circle cx='12' cy='12' r='4.8'/>" + string.Concat(Enumerable.Range(0, 8).Select(i =>
			$"<rect x='10.9' y='0.8' width='2.2' height='4.6' rx='1.1' transform='rotate({i * 45} 12 12)'/>")),
		Kind.Warning => "<path fill-rule='evenodd' stroke='#fff' stroke-width='2' stroke-linejoin='round' d='M12 3.2 21.8 20.2H2.2zM10.9 9h2.2l-.4 6.2h-1.4zM12 16.4a1.3 1.3 0 1 0 0 2.6 1.3 1.3 0 0 0 0-2.6z'/>",
		Kind.Pause => "<rect x='5' y='3' width='5' height='18' rx='1.6'/><rect x='14' y='3' width='5' height='18' rx='1.6'/>",
		Kind.Play => "<path stroke='#fff' stroke-width='2.4' stroke-linejoin='round' d='M6.5 3.8 19.5 12 6.5 20.2z'/>",
		Kind.Info => "<path fill-rule='evenodd' d='M12 1.5a10.5 10.5 0 1 0 0 21 10.5 10.5 0 0 0 0-21zM12 5.4a1.6 1.6 0 1 1 0 3.2 1.6 1.6 0 0 1 0-3.2zM10.2 10.2h3.1v6.6h1.5v2h-4.6v-2h1.1v-4.6h-1.1z'/>",
		Kind.Refresh => "<path fill='none' stroke='#fff' stroke-width='2.6' stroke-linecap='round' d='M19.4 13.2A7.6 7.6 0 1 1 17 6.3'/><path d='M14.2 2.6h7.2v7.2z'/>",
		_ => "",
	};

	static readonly System.Collections.Generic.Dictionary<(Kind, int), Texture2D?> Cache = new();

	/// <summary>The icon, white, size × size (null in headless runs, which have no renderer for textures).</summary>
	public static Texture2D? Icon(Kind kind, int size)
	{
		if (Cache.TryGetValue((kind, size), out var cached)) return cached;
		Texture2D? texture = null;
		if (DisplayServer.GetName() != "headless")
		{
			var image = new Image();
			// (drawn at 2× and scaled down, for smooth edges at the canvas_items stretch)
			if (image.LoadSvgFromString($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='#fff'>{Body(kind)}</svg>", size * 2 / 24f) == Error.Ok)
				texture = ImageTexture.CreateFromImage(image);
		}
		Cache[(kind, size)] = texture;
		return texture;
	}

	/// <summary>Puts the icon before a button's text, in the button's own text colour (hover, pressed and disabled too).</summary>
	public static Button On(this Button b, Kind kind, int size = 16)
	{
		b.Icon = Icon(kind, size);
		b.AddThemeConstantOverride("icon_max_width", size);
		// (read from the theme once the button is in it, and again when a skin changes the theme)
		void Tint()
		{
			foreach (var (font, icon) in States)
			{
				var c = b.GetThemeColor(font);
				if (b.GetThemeColor(icon) != c) b.AddThemeColorOverride(icon, c);
			}
		}
		b.Ready += Tint;
		b.ThemeChanged += Tint;
		return b;
	}

	static readonly (string Font, string Icon)[] States =
	{
		("font_color", "icon_normal_color"), ("font_hover_color", "icon_hover_color"), ("font_pressed_color", "icon_pressed_color"),
		("font_hover_pressed_color", "icon_hover_pressed_color"), ("font_focus_color", "icon_focus_color"), ("font_disabled_color", "icon_disabled_color"),
	};

	/// <summary>The icon as a control in one colour (for rows and notices).</summary>
	public static TextureRect Rect(Kind kind, int size, Color color) => new()
	{
		Texture = Icon(kind, size),
		SelfModulate = color,
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		CustomMinimumSize = new Vector2(size, size),
		SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		MouseFilter = Control.MouseFilterEnum.Ignore,
	};

	/// <summary>A label with the icon before it, in the label's colour (with <paramref name="grow"/> the row and label take
	/// the width left; wrapping labels stay wrapping).</summary>
	public static HBoxContainer Before(Kind kind, Label label, int size = 0, bool grow = true)
	{
		var px = size > 0 ? size : Mathf.RoundToInt((label.HasThemeFontSizeOverride("font_size") ? label.GetThemeFontSize("font_size") : UiTheme.BaseFontSize) * 1.05f);
		var icon = Rect(kind, px, label.HasThemeColorOverride("font_color") ? label.GetThemeColor("font_color") : C.Text);
		// (on the first line of a wrapping label, not in the middle of all of them)
		if (label.AutowrapMode != TextServer.AutowrapMode.Off) icon.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		var row = U.H(Mathf.Max(4, px / 3), icon, label);
		if (grow) row.SizeFlagsHorizontal = label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		return row;
	}
}
