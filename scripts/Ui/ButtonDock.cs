// Where the corner buttons (the jukebox's note and the 🐞) go. Bottom left, the note at the bottom and the 🐞 above it; on
// a window wider than 16:9 that's in the space beside the screen, where they cover nothing. A screen whose own things sit
// in that corner gives them a spot instead (ButtonDock.Spot(): in its top bar, or a free corner), on every window shape
// (since 0.13 also on ultrawide ones, so they never jump between places), side by side.

using System.Collections.Generic;
using System.Linq;
using Godot;

namespace QuizNight.Ui;

/// <summary>An empty box in a screen's layout that the corner buttons are drawn over (see ButtonDock).</summary>
public partial class ButtonSpot : Control
{
	public ButtonSpot() : this(36) { }

	public ButtonSpot(float height)
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(height * 2 + ButtonDock.Gap, height);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
	}

	public override void _EnterTree() => ButtonDock.Spots.Add(this);
	public override void _ExitTree() => ButtonDock.Spots.Remove(this);
}

public static class ButtonDock
{
	public const float Margin = 20, Gap = 8;
	/// <summary>The note button's size in the corner (the 🐞's is as wide, a bit lower).</summary>
	public const float Note = 44, Bug = 36;

	internal static readonly HashSet<ButtonSpot> Spots = new();

	/// <summary>A spot for the buttons, for a screen's top bar (or anchored in a free corner).</summary>
	public static Control Spot(float height = 36) => new ButtonSpot(height);

	/// <summary>
	/// The screen's spot if it has one (shown): the buttons sit there on every screen shape, also on ultrawide monitors
	/// with room beside the 16:9 picture, so they're always in the same place (they used to go to the corner there).
	/// </summary>
	static ButtonSpot? Active()
	{
		var app = App.I;
		if (app == null) return null;
		return Spots.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.IsVisibleInTree());
	}

	/// <summary>True while the buttons sit in the bottom left corner (the "Now playing" card goes beside them there).</summary>
	public static bool InCorner => Active() == null;

	/// <summary>Where a button goes now: slot 0 is the note, 1 the 🐞.</summary>
	public static Rect2 Place(int slot)
	{
		if (Active() is { } spot)
		{
			var r = spot.GetGlobalRect();
			var h = r.Size.Y;
			return new Rect2(r.Position.X + slot * (h + Gap), r.Position.Y, h, h);
		}
		var height = App.I?.Size.Y ?? 720;
		var note = new Rect2(Margin, height - Margin - Note, Note, Note);
		return slot == 0 ? note : new Rect2(Margin, note.Position.Y - 10 - Bug, Note, Bug);
	}

	/// <summary>Puts a button where its slot is now (called every frame: spots move with their screen's layout).</summary>
	public static void Follow(Control button, int slot)
	{
		var r = Place(slot);
		if (button.Position != r.Position) button.Position = r.Position;
		if (button.Size != r.Size) button.Size = r.Size;
	}
}
