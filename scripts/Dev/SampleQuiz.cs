// A quiz with one question of every kind, made in code (generated images and a beep), for
// testing and for trying the game without building a quiz first.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using QuizNight.Server;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Dev;

public static class SampleQuiz
{
	/// <summary>
	/// A made-up finished night of the sample quiz (--screen podium, the Test lab, the self-test): six players with points,
	/// buzzes, pins and typos, one who left, through a real Room so its awards are the real ones. players: fewer (the
	/// first ones) or more (made-up ones with a few points); tie: the first two equal in everything (they share 1st).
	/// </summary>
	public static FinalResults Podium(int players = 6, bool tie = false)
	{
		var room = new Room("TEST", Build()) { HostName = "maxlvl01" };
		var names = new[] { "[OTB] BassGaming", "Ann", "Kirito", "Lena", "Tom", "Sophie" };
		foreach (var (name, i) in names.Select((n, i) => (n, i))) room.Join($"p{i}", name);
		room.Start();
		void Q(int index, string id, int points, long? buzzMs = null, long? answerMs = null, double? km = null, int? typos = null, int wrong = 0)
		{
			// (the stats go to the current question: these are filed by hand under each one)
			var key = (index, id);
			var s = room.StatsOf(id);
			var q = room.QuestionStats.TryGetValue(key, out var x) ? x : room.QuestionStats[key] = new QuestionStat();
			room.Scores[id] = room.Score(id) + points;
			q.Points += points;
			if (points > 0) { s.Scored++; q.Right = true; }
			if (buzzMs is { } b) { s.BuzzMs += b; s.Buzzes++; q.BuzzMs = b; }
			if (answerMs is { } a) { s.AnswerMs += a; s.Answers++; q.AnswerMs = a; }
			if (km is { } k) { s.Km += k; s.KmAnswers++; q.Km = k; }
			q.Typos = typos;
			q.WrongBuzzes += wrong;
		}
		Q(0, "p0", 3, buzzMs: 900); Q(0, "p1", 0, buzzMs: 1500, wrong: 1); Q(0, "p2", 0, buzzMs: 2100, wrong: 1);
		Q(1, "p0", 2, answerMs: 4200, typos: 0); Q(1, "p1", 2, answerMs: 3100, typos: 0); Q(1, "p3", 2, answerMs: 6000, typos: 2);
		Q(2, "p1", 5, km: 12.4); Q(2, "p0", 2, km: 85); Q(2, "p2", 1, km: 140); Q(2, "p4", 0, km: 610);
		Q(3, "p2", 4, buzzMs: 700); Q(3, "p0", 0, buzzMs: 1100, wrong: 1); Q(3, "p5", 1, answerMs: 5200, typos: 0);
		Q(4, "p1", 1, km: 33); Q(4, "p0", 3, km: 4.2); Q(4, "p3", 1, answerMs: 2600, typos: 0); Q(4, "p5", 2, answerMs: 4800, typos: 0);
		room.NoteHostPoints("p4", 2);
		room.Scores["p4"] += 2;
		room.Leave("p5");
		room.GetPlayer("p5")!.LeftAt = 5;
		var extra = new[] { "Mia", "Jonas", "Ella", "Paul", "Noah", "Lina", "Ben", "Emma", "Finn", "Clara", "Leon", "Hanna", "Luis", "Marie", "Felix", "Ida", "Max", "Nora" };
		var rng = new Random(7);
		for (var i = 6; i < Math.Min(players, Limits.MaxPlayers); i++)
		{
			room.Join($"p{i}", extra[(i - 6) % extra.Length]);
			Q(rng.Next(5), $"p{i}", rng.Next(0, 3), answerMs: rng.Next(2000, 9000));
		}
		for (var i = Math.Max(players, 1); i < 6; i++) room.Remove($"p{i}");
		if (tie && players >= 2)
		{
			room.Scores["p1"] = room.Score("p0");
			room.Stats["p1"] = room.StatsOf("p0");
		}
		room.Finish();
		var results = FinalResults.From(room.ToState("host"));
		results.Host = room.HostName;
		return results;
	}

	public static Quiz Build()
	{
		var skyrim = new MapAsset { Id = "map-skyrim", Name = "Skyrim", PresetId = "skyrim", WidthKm = MapPresets.Get("skyrim")!.WidthKm };
		var tex = Images.BuiltinTexture(MapPresets.Get("skyrim")!.ImagePath);
		skyrim.Image = new ImageAsset { Width = tex?.GetWidth() ?? SkyrimCrop.Width, Height = tex?.GetHeight() ?? SkyrimCrop.Height };
		var wow = new MapAsset { Id = "map-wow", Name = "WoW Forever", PresetId = WowAtlas.PresetId, Image = new ImageAsset { Width = WowAtlas.ImageWidth, Height = WowAtlas.ImageHeight } };
		var custom = new MapAsset { Id = "map-custom", Name = "Test grid", Image = Picture(1600, 1000, new Color("#2b6"), new Color("#135"), "GRID"), WidthKm = 10 };

		var q = new List<Question>
		{
			new()
			{
				Id = "q1", Type = QuestionType.Classic, Mode = ClassicMode.Buzzer, Prompt = "Which planet is known as the Red Planet?",
				AnswerText = "Mars", Points = 1, Image = Picture(1280, 720, new Color("#c1440e"), new Color("#2a0a05"), "?"),
				BonusPoints = 1, BonusText = "Also say which planet from the Sun it is (4th)",
			},
			new()
			{
				Id = "q2", Type = QuestionType.Classic, Mode = ClassicMode.Choice, Prompt = "Which of these is a prime number?",
				Choices = new() { "21", "27", "29", "33" }, CorrectChoice = 2, TimeLimitSec = 20, Points = 2,
			},
			new()
			{
				Id = "q3", Type = QuestionType.Estimation, Prompt = "How many keys does a standard piano have?", AnswerNumber = 88, Unit = "keys",
				TimeLimitSec = 30, Scoring = Scoring.Top3, Points = 1,
			},
			new()
			{
				Id = "q4", Type = QuestionType.MapPin, Prompt = "Where is Whiterun?", Image = Picture(1280, 720, new Color("#8a7a55"), new Color("#2c2a20"), "SCREENSHOT"),
				MapId = skyrim.Id, Target = new NormPoint(0.57, 0.555), TimeLimitSec = 60, Points = 1,
			},
			new()
			{
				Id = "q5", Type = QuestionType.MapPin, Prompt = "Where is Goldshire?", Image = Picture(1280, 720, new Color("#4a7a35"), new Color("#18240f"), "ELWYNN"),
				MapId = wow.Id, Target = WowAtlas.LocationToWorld(new WowAtlas.Location(0, -9465, 64)), TimeLimitSec = 60, Points = 1,
			},
			new()
			{
				Id = "q6", Type = QuestionType.MapPin, Prompt = "Find the yellow square", Image = Picture(800, 450, new Color("#f5b942"), new Color("#3a2a00"), "■"),
				MapId = custom.Id, Target = new NormPoint(0.7, 0.3), TimeLimitSec = 45, Points = 1,
			},
			new()
			{
				Id = "q7", Type = QuestionType.Sound, Prompt = "Name this sound!", AnswerText = "A beep / beep", Points = 1,
				Typed = true, TimeLimitSec = 30, BonusPoints = 2, BonusText = "Also say how many beeps",
				Media = new MediaAsset { Mime = "audio/wav", Name = "beep.wav", DataUrl = DataUrl.Make("audio/wav", Beep(4)), DurationSec = 4 },
			},
			new()
			{
				Id = "q8", Type = QuestionType.Classic, Mode = ClassicMode.Multi, Prompt = "Which of these are planets?",
				Choices = new() { "Mars", "Venus", "Pluto", "The Moon" }, CorrectChoices = new() { 0, 1 }, TimeLimitSec = 20, Points = 2,
			},
			new()
			{
				Id = "q9", Type = QuestionType.Image, Prompt = "Which game's map is this?", AnswerText = "Skyrim", Points = 1,
				Image = tex?.GetImage() is { } map ? Images.Encode((Image)map.Duplicate(), 1024) : Picture(1280, 720, new Color("#456"), new Color("#123"), "?"),
				RevealStyle = RevealStyle.Pixelate, RevealSec = 20, Focus = new NormPoint(0.5, 0.45),
			},
			new()
			{
				Id = "q10", Type = QuestionType.Order, Prompt = "Put these games in the order they came out",
				Items = new() { "Doom", "Half-Life", "Portal", "Elden Ring" }, OrderTop = "Oldest", OrderBottom = "Newest", TimeLimitSec = 30, Points = 2,
			},
			new()
			{
				Id = "q11", Type = QuestionType.Classic, Typed = true, Prompt = "What's the capital of Australia?", AnswerText = "Canberra", TimeLimitSec = 30, Points = 1,
			},
			new()
			{
				Id = "q12", Type = QuestionType.Order, OrderMode = OrderMode.Timeline, Prompt = "When did these games come out?",
				Items = new() { "Doom", "Half-Life", "Portal", "Elden Ring" }, ItemLabels = new() { "1993", "1998", "2007", "2022" },
				OrderTop = "Oldest", OrderBottom = "Newest", TimeLimitSec = 30, Points = 1,
			},
			new()
			{
				Id = "q13", Type = QuestionType.Picture, Mode = ClassicMode.Buzzer, Prompt = "Which game are these Steam reviews about?", AnswerText = "Skyrim",
				Points = 1, Slides = new()
				{
					Picture(1280, 720, new Color("#1b2838"), new Color("#2a475e"), "REVIEW 1"),
					Picture(1280, 720, new Color("#2a475e"), new Color("#1b2838"), "REVIEW 2"),
					Picture(1280, 720, new Color("#66c0f4"), new Color("#1b2838"), "REVIEW 3"),
				},
				// (the answer as a picture: the game's store page, at the reveal)
				AnswerImage = Picture(1280, 720, new Color("#c6d4df"), new Color("#1b2838"), "STORE PAGE"),
			},
			new()
			{
				Id = "q14", Type = QuestionType.Steps, Prompt = "Which season are we looking for?", AnswerText = "Bundesliga 2008/09 (VfL Wolfsburg)",
				Hints = new() { "The champion had never won the league before.", "Its two top scorers scored 54 goals together.", "Felix Magath was the coach.", "Grafite and Džeko.", "VfL Wolfsburg." },
			},
			new()
			{
				Id = "q15", Type = QuestionType.Interactive, Prompt = "Imitate the Super Mario jump sound!", TimeLimitSec = 40, Judging = Judging.Vote, Points = 1,
			},
		};
		return new Quiz
		{
			Id = "sample-quiz", Title = "Sample quiz night", CreatedAt = QuizStore.NowMs(), UpdatedAt = QuizStore.NowMs(),
			Maps = new() { skyrim, wow, custom }, Questions = q,
		};
	}

	/// <summary>A gradient picture with a big label, as a WebP data URL.</summary>
	public static ImageAsset Picture(int w, int h, Color a, Color b, string label)
	{
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		for (var y = 0; y < h; y += 4)
		for (var x = 0; x < w; x += 4)
		{
			var t = (x / (float)w + y / (float)h) / 2;
			var c = a.Lerp(b, t);
			// grid lines every 100 px so maps have something to see
			if (x % 100 == 0 || y % 100 == 0) c = c.Lightened(0.25f);
			img.FillRect(new Rect2I(x, y, 4, 4), c);
		}
		var asset = Images.Encode(img, Math.Max(w, h));
		return asset;
	}

	/// <summary>A WAV file with a few seconds of a two-tone beep.</summary>
	public static byte[] Beep(double seconds)
	{
		const int rate = 22050;
		var samples = (int)(rate * seconds);
		using var ms = new MemoryStream();
		using var w = new BinaryWriter(ms);
		w.Write("RIFF"u8.ToArray());
		w.Write(36 + samples * 2);
		w.Write("WAVEfmt "u8.ToArray());
		w.Write(16);
		w.Write((short)1);
		w.Write((short)1);
		w.Write(rate);
		w.Write(rate * 2);
		w.Write((short)2);
		w.Write((short)16);
		w.Write("data"u8.ToArray());
		w.Write(samples * 2);
		for (var i = 0; i < samples; i++)
		{
			var t = i / (double)rate;
			var f = (int)(t * 2) % 2 == 0 ? 440 : 660;
			w.Write((short)(Math.Sin(2 * Math.PI * f * t) * 6000));
		}
		return ms.ToArray();
	}
}
