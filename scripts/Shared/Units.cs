// Units for estimation questions, grouped for the editor's drop-down. The question stores the unit as the text players
// see ("km", "light years"). Anything else can still
// be typed as a custom unit.

using System.Collections.Generic;
using System.Linq;

namespace QuizNight.Shared;

public static class Units
{
	/// <summary>(group, units): a unit is its symbol or a word.</summary>
	public static readonly (string Group, string[] Units)[] Groups =
	{
		("Length", new[] { "mm", "cm", "m", "km", "in", "ft", "yd", "mi", "nmi", "AU", "light years", "pc" }),
		("Mass", new[] { "mg", "g", "kg", "t", "oz", "lb" }),
		("Time", new[] { "ms", "s", "min", "h", "days", "weeks", "months", "years" }),
		("Speed", new[] { "m/s", "km/h", "mph", "knots" }),
		("Area", new[] { "m²", "km²", "ha", "acres" }),
		("Volume", new[] { "ml", "l", "m³", "gal" }),
		("Temperature", new[] { "°C", "°F", "K" }),
		("Energy and power", new[] { "J", "kJ", "kcal", "kWh", "W", "kW", "MW", "hp", "PS" }),
		("Data", new[] { "KB", "MB", "GB", "TB" }),
		("Money", new[] { "€", "$", "£" }),
		("Other", new[] { "%", "people", "pieces" }),
	};

	/// <summary>What a picked unit stores: the unit as it is.</summary>
	public static string Text(string unit) => unit;

	// (quizzes made while the game also spoke German stored these words in German)
	static readonly Dictionary<string, string> German = new()
	{
		["Lichtjahre"] = "light years", ["Tage"] = "days", ["Wochen"] = "weeks", ["Monate"] = "months", ["Jahre"] = "years",
		["Knoten"] = "knots", ["Acres"] = "acres", ["Personen"] = "people", ["Stück"] = "pieces",
	};

	/// <summary>The list unit a stored text is (also an old German word), or null for a custom one.</summary>
	public static string? Find(string text)
	{
		if (text == "") return null;
		foreach (var u in Groups.SelectMany(g => g.Units))
			if (u == text) return u;
		return German.GetValueOrDefault(text);
	}
}
