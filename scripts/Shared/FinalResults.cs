// The podium as it was when the quiz ended: everyone's place, points and numbers, the night's awards and who hosted.
// Every screen takes this once, from the first finished RoomState, so nobody drops off the podium by leaving; it's
// also what's kept on this PC to watch again (Storage.Podiums, the match history's "Your podiums").

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuizNight.Shared;

/// <summary>
/// One of the night's little awards, made by the host (Server.NightAwards): its kind, who won it (ties share it) and
/// their number. The text comes from the kind on each screen, in that player's language.
/// </summary>
public sealed class NightAward
{
	public string Kind = "";
	public List<string> Players = new();
	public double Value;
	/// <summary>The next best two (for the award pack's cards).</summary>
	public List<AwardRunner> Runners = new();
}

public sealed class AwardRunner
{
	public string Id = "";
	public double Value;
}

public sealed class FinalPlayer
{
	public string Id = "";
	public string Name = "";
	public string Color = "";
	public int Score;
	public int Place;
	public int? JoinedAt;
	public int? LeftAt;
	public NightStats Stats = new();
	/// <summary>A team on a team night's podium (FinalResults.Teams): its members' ids and names.</summary>
	public List<string>? MemberIds;
	public List<string>? Members;

	public PublicPlayer ToPublic() => new() { Id = Id, Name = Name, Color = Color, Connected = true, JoinedAt = JoinedAt, LeftAt = LeftAt };
}

public sealed class FinalResults
{
	public const int CurrentFormat = 1;
	public int Format = CurrentFormat;
	public string QuizTitle = "";
	public int Questions;
	/// <summary>Who hosted ("" when the host's game didn't say: before 0.13).</summary>
	public string Host = "";
	public DateTime Date;
	public string PodiumSound = "";
	/// <summary>Who this PC played as (null for the host), so a kept podium still shows "you".</summary>
	public string? Me;
	/// <summary>In ranked order.</summary>
	public List<FinalPlayer> Players = new();
	public List<NightAward> Awards = new();
	/// <summary>Team mode: the teams by place (the podium shows them; Players stay for the awards). Null on a solo night.</summary>
	public List<FinalPlayer>? Teams;

	/// <summary>Who stands on the podium: the teams on a team night, else the players.</summary>
	public List<FinalPlayer> Ranked => Teams ?? Players;

	/// <summary>The podium entry of this player: their team on a team night.</summary>
	public FinalPlayer? Entry(string id) => Teams?.Find(t => t.MemberIds?.Contains(id) == true) ?? Player(id);

	/// <summary>The room as it ended. Players who left before the end are in it (with when they left).</summary>
	public static FinalResults From(RoomState room) => new()
	{
		QuizTitle = room.Quiz.Title,
		Questions = room.Quiz.QuestionCount,
		Host = room.HostName,
		Date = DateTime.Now,
		PodiumSound = room.Settings.PodiumSound,
		Players = room.Ranked().Select(p => new FinalPlayer
		{
			Id = p.Id, Name = p.Name, Color = p.Color, Score = room.Score(p.Id), Place = room.Place(p.Id),
			JoinedAt = p.JoinedAt, LeftAt = p.LeftAt, Stats = room.Stats.GetValueOrDefault(p.Id) ?? new NightStats(),
		}).ToList(),
		Awards = room.Awards?.Where(a => NightAwards.Known(a.Kind) && a.Players.Count > 0).ToList() ?? new(),
		Teams = room.Teams?.Select(t => new FinalPlayer
		{
			Id = "team:" + t.Index, Name = t.Name, Color = t.Color, Score = t.Score, Place = t.Place,
			MemberIds = t.Members.ToList(), Members = t.Members.Select(id => room.Player(id).Name).ToList(),
		}).ToList(),
	};

	public FinalPlayer? Player(string id) => Players.Find(p => p.Id == id);

	static readonly JsonSerializerOptions Json = new()
	{
		IncludeFields = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		IgnoreReadOnlyProperties = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public string ToJson() => JsonSerializer.Serialize(this, Json);

	/// <summary>Null if it isn't a podium this game can read.</summary>
	public static FinalResults? FromJson(string text)
	{
		try
		{
			var r = JsonSerializer.Deserialize<FinalResults>(text, Json);
			return r is { Format: <= CurrentFormat, Players.Count: > 0 } ? r : null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}

/// <summary>What each award is called and how its number reads (the host only sends the kind and the number).</summary>
public static class NightAwards
{
	public sealed record Info(string Title, string Line, Func<double, string> Format);

	static string Count(double v) => ((int)Math.Round(v)).ToString();

	// (in the order the credits show them)
	public static readonly (string Kind, Info Info)[] All =
	{
		("most-right", new("Most right answers", "{0} questions scored", Count)),
		("fastest-buzzer", new("Fastest buzzer", "{0} on average", v => Ui.Screens.NightStatsView.Seconds(v))),
		("quickest-answers", new("Quickest answers", "{0} on average", v => Ui.Screens.NightStatsView.Seconds(v))),
		("big-haul", new("Biggest haul", "{0} points on one question", Count)),
		("map-master", new("Map master", "{0} off on average", v => Ui.Screens.NightStatsView.Distance(v))),
		("bullseye", new("Bullseye", "the closest pin: {0} off", v => Ui.Screens.NightStatsView.Distance(v))),
		("best-guesser", new("Best guesser", "{0} off on average", v => Ui.Screens.NightStatsView.Percent(v))),
		("best-speller", new("Best speller", "{0} answers without a typo", Count)),
		("mind-reader", new("Mind reader", "right at the first hint {0}×", Count)),
		("time-traveller", new("Time traveller", "{0} items placed right", Count)),
		("host-favourite", new("Host's favourite", "{0} points from the host", Count)),
		("trigger-happy", new("Trigger-happy", "{0} wrong buzzes", Count)),
	};

	public static bool Known(string kind) => All.Any(a => a.Kind == kind);

	public static Info? Of(string kind) => All.FirstOrDefault(a => a.Kind == kind).Info;

	public static int Order(string kind) => Array.FindIndex(All, a => a.Kind == kind);
}
