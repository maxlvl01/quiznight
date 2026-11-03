using System;
using System.Globalization;
using Godot;
using QuizNight.Shared;

namespace QuizNight.Ui;

/// <summary>
/// A small month calendar that opens under a calendar button (the estimation's "Data as of"). Picking a day writes it into the
/// field as "1 November 2026"; the field stays free text, so "2024" or "March 2025" still work.
/// Esc or a click outside closes it.
/// </summary>
public partial class DatePicker : Control
{
	static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-GB");
	// (German dates still read: quizzes made while the game also spoke German have them)
	static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

	/// <summary>A day as the field shows it.</summary>
	public static string Format(DateTime day) => day.ToString("d MMMM yyyy", En);

	/// <summary>The day a field's text names, if it's written the way the picker writes it (in either language).</summary>
	public static DateTime? Parse(string text)
	{
		foreach (var (culture, pattern) in new[] { (En, "d MMMM yyyy"), (De, "d. MMMM yyyy") })
			if (DateTime.TryParseExact(text.Trim(), pattern, culture, DateTimeStyles.None, out var day)) return day;
		return null;
	}

	public const string Tooltip = "Pick the date from a calendar";

	/// <summary>A calendar button for a text field (drawn: MediaIcon.Kind.Calendar): opens the calendar under itself, and the
	/// picked day replaces the field's text.</summary>
	public static Button For(LineEdit field)
	{
		Button? button = null;
		button = MediaIcon.Button(MediaIcon.Kind.Calendar, Tooltip, () => App.I.AddChild(new DatePicker(button!, Parse(field.Text), day =>
		{
			field.Text = Format(day);
			// (setting Text doesn't send TextChanged, which is what saves the draft)
			field.EmitSignal(LineEdit.SignalName.TextChanged, field.Text);
		})), 38);
		button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		return button;
	}

	readonly Control _anchor;
	readonly DateTime? _picked;
	readonly Action<DateTime> _pick;
	readonly PanelContainer _card;
	readonly VBoxContainer _body = U.V(8);
	DateTime _month;

	public DatePicker() : this(new Control(), null, _ => { }) { }

	DatePicker(Control anchor, DateTime? picked, Action<DateTime> pick)
	{
		_anchor = anchor;
		_picked = picked;
		_pick = pick;
		var start = picked ?? DateTime.Today;
		_month = new DateTime(start.Year, start.Month, 1);
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		// (the card stops clicks itself, so whatever reaches this is outside it)
		GuiInput += e =>
		{
			if (e is InputEventMouseButton { Pressed: true }) QueueFree();
		};
		_card = U.Panel(U.Pad(_body, 12, 10, 12, 12));
		_card.Resized += Place;
		AddChild(_card);
		Fill();
	}

	public override void _Ready() => Place();

	/// <summary>Under the button, its right edge on the button's, kept inside the window.</summary>
	void Place()
	{
		if (!IsInstanceValid(_anchor)) return;
		var at = _anchor.GetGlobalRect();
		var size = _card.GetCombinedMinimumSize();
		var x = Mathf.Clamp(at.End.X - size.X, 8, Mathf.Max(8, Size.X - size.X - 8));
		var y = at.End.Y + 6;
		// (no room below: above the button instead)
		if (y + size.Y > Size.Y - 8) y = Mathf.Max(8, at.Position.Y - size.Y - 6);
		_card.Position = new Vector2(x, y);
		_card.Size = size;
	}

	void Fill()
	{
		foreach (var child in _body.GetChildren()) child.QueueFree();
		var title = U.Bold(_month.ToString("MMMM yyyy", En), 16, null, false, HorizontalAlignment.Center).Raw().Grow();
		title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_body.AddChild(U.H(8,
			U.Small("◀", () => Turn(-1)).Raw(),
			title,
			U.Small("▶", () => Turn(1)).Raw()));

		var grid = new GridContainer { Columns = 7 };
		grid.AddThemeConstantOverride("h_separation", 4);
		grid.AddThemeConstantOverride("v_separation", 4);
		// Weeks start on Monday.
		for (var i = 0; i < 7; i++)
			grid.AddChild(U.Text(En.DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7], 12, C.Muted, null, false, HorizontalAlignment.Center).Raw().MinSize(40));
		var blanks = ((int)_month.DayOfWeek + 6) % 7;
		for (var i = 0; i < blanks; i++) grid.AddChild(new Control());
		var today = DateTime.Today;
		for (var d = 1; d <= DateTime.DaysInMonth(_month.Year, _month.Month); d++)
		{
			var day = new DateTime(_month.Year, _month.Month, d);
			var button = U.Btn($"{d}", "SmallButton", () =>
			{
				_pick(day);
				QueueFree();
			}, 14).Raw().MinSize(40, 32);
			// The picked day orange, today teal. (Not the Accent/Teal button styles: their wider padding made today's
			// column wider than the others, since a grid column is as wide as its widest button.)
			if ((day == _picked ? C.Accent : day == today ? C.Teal : (Color?)null) is { } mark)
			{
				button.AddThemeStyleboxOverride("normal", UiTheme.Box(mark, 9, null, 0, 0, 0));
				button.AddThemeStyleboxOverride("hover", UiTheme.Box(mark.Lightened(0.15f), 9, null, 0, 0, 0));
				button.AddThemeStyleboxOverride("pressed", UiTheme.Box(mark.Darkened(0.1f), 9, null, 0, 0, 0));
				foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
					button.AddThemeColorOverride(state, C.AccentInk);
			}
			grid.AddChild(button);
		}
		// (on a dark base, like a calendar page)
		_body.AddChild(U.Panel(U.Pad(grid, 8, 8, 8, 8), "Inset"));
		_body.AddChild(U.Small("Today", () =>
		{
			_pick(today);
			QueueFree();
		}).With(b => b.SizeFlagsHorizontal = SizeFlags.ShrinkEnd));
	}

	void Turn(int months)
	{
		_month = _month.AddMonths(months);
		Fill();
	}

	public override void _UnhandledKeyInput(InputEvent e)
	{
		if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
		{
			QueueFree();
			GetViewport().SetInputAsHandled();
		}
	}
}
