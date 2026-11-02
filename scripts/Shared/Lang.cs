// UI text. The game is English only since 0.14 (German was dropped: quizzes can still be written in any language).
// Text built from parts goes through Lang.F with a "{0}" template, so it reads the same everywhere.

namespace QuizNight.Shared;

public static class Lang
{
	public static string T(string en) => en;

	public static string F(string en, params object?[] args) => string.Format(en, args);

	/// <summary>A place: "1st", "2nd", "3rd", "4th", "11th", "22nd".</summary>
	public static string Ordinal(int n)
	{
		var suffix = (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
		return $"{n}{suffix}";
	}
}
