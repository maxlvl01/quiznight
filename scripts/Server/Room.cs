// A room on the host: code, players, settings, scores, start/next/finish. The host app owns
// exactly one room at a time; HostSession connects it to the network.

using System;
using System.Collections.Generic;
using System.Linq;
using QuizNight.Shared;

namespace QuizNight.Server;

public sealed class Player
{
	public string Id = "";
	public string Name = "";
	public string Color = "";
	public bool Connected;
	public long? DisconnectedAt;
	/// <summary>Joined after the start: the question (1-based) they came in at.</summary>
	public int? JoinedAt;
	/// <summary>Left during the game and not back yet: the question (1-based) they left at.</summary>
	public int? LeftAt;
	/// <summary>Their game's version ("" = before 0.12).</summary>
	public string Version = "";
	/// <summary>A seat from a saved night (Room.Restore): it keeps its points in the lobby until they're back.</summary>
	public bool Saved;
	/// <summary>Team mode: their team (TeamNames index), once drawn.</summary>
	public int? Team;
}

/// <summary>What breaks ties between players with the same score (see Room.Ranking).</summary>
public sealed class PlayerStats
{
	/// <summary>When their score last changed: of two with the same score, the one who got there first is ahead.</summary>
	public long? ScoreChangedAt;
	/// <summary>Answers locked in and recordings sent: time from the question's start.</summary>
	public long AnswerMs;
	public int Answers;
	/// <summary>Buzzes: time to buzz in (much quicker than typing, so it's its own average).</summary>
	public long BuzzMs;
	public int Buzzes;
	public double Km;
	public int KmAnswers;
	/// <summary>Questions they scored on.</summary>
	public int Scored;

	/// <summary>Points the host handed out: bonuses and score changes by hand (taking some away counts against it).</summary>
	public int HostPoints;

	/// <summary>Estimations: how far off, in % of the right number.</summary>
	public double PercentOff;
	public int PercentAnswers;

	public double? AvgAnswerMs => Answers > 0 ? (double)AnswerMs / Answers : null;
	public double? AvgBuzzMs => Buzzes > 0 ? (double)BuzzMs / Buzzes : null;
	public double? AvgKm => KmAnswers > 0 ? Km / KmAnswers : null;
	public double? AvgPercentOff => PercentAnswers > 0 ? PercentOff / PercentAnswers : null;
}

/// <summary>One player's go at one question, for the per-question rows of the saved results (stats per question type).</summary>
public sealed class QuestionStat
{
	/// <summary>Points from the question itself (with Double, bonus and hand-ticked answers; not the host's score changes).</summary>
	public int Points;
	public bool Right;
	/// <summary>Answered, buzzed, recorded or took a timeline turn (else they just let it pass).</summary>
	public bool Answered;
	public int Buzzes, WrongBuzzes, Placements, PlacementsRight;
	public long? BuzzMs, AnswerMs;
	public double? Km, PercentOff;
	/// <summary>Step by step: the hint (1 = first) they got it right at.</summary>
	public int? Hint;
	public string Joker = "";
	/// <summary>Typed answers: letters off from the closest accepted answer (0 = spelled perfectly).</summary>
	public int? Typos;
}

public sealed class Room : IGameHooks
{
	/// <summary>How long a disconnected player keeps their lobby seat.</summary>
	public const long PlayerGraceMs = 60_000;

	public string Phase { get; private set; } = Shared.Phase.Lobby;
	public readonly RoomSettings Settings = new();
	/// <summary>In join order.</summary>
	public readonly List<Player> Players = new();
	public Game? Game { get; private set; }
	public readonly Dictionary<string, int> Scores = new();
	/// <summary>Per player, for the ranking's tiebreaks and the saved results.</summary>
	public readonly Dictionary<string, PlayerStats> Stats = new();

	public PlayerStats StatsOf(string playerId) => Stats.TryGetValue(playerId, out var s) ? s : Stats[playerId] = new PlayerStats();

	/// <summary>Per question (0-based index) and player: how it went, for the saved results.</summary>
	public readonly Dictionary<(int Question, string Player), QuestionStat> QuestionStats = new();

	/// <summary>The current question's record for a player.</summary>
	QuestionStat Q(string playerId)
	{
		var key = (Game?.Index ?? 0, playerId);
		return QuestionStats.TryGetValue(key, out var s) ? s : QuestionStats[key] = new QuestionStat();
	}

	public readonly string Code;
	/// <summary>Full quiz including answers and media — never send this to players as-is.</summary>
	public readonly Quiz Quiz;
	readonly Func<long> _clock;

	/// <summary>The files players get ahead of time, from the lobby on (see Game.SharedMedia).</summary>
	public readonly IReadOnlyList<Game.SharedMedia> SharedFiles;

	public Room(string code, Quiz quiz, Func<long>? clock = null)
	{
		Code = code;
		Quiz = quiz;
		SharedFiles = Game.ShareFiles(quiz);
		_clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
	}

	/// <summary>The editor's question preview (Ui/Screens/QuestionPreview): one player is enough to start.</summary>
	public bool Preview;

	/// <summary>The host's name, for the podium's credits.</summary>
	public string HostName = "";

	/// <summary>The night's awards, made once it's finished (until the room goes back to the lobby).</summary>
	List<NightAward>? _awards;

	public static string NewCode(Random? rng = null)
	{
		rng ??= Random.Shared;
		return string.Concat(Enumerable.Range(0, Limits.CodeLength).Select(_ => Limits.CodeAlphabet[rng.Next(Limits.CodeAlphabet.Length)]));
	}

	public Player? GetPlayer(string id) => Players.Find(p => p.Id == id);

	// ---------- IGameHooks ----------

	public long Now() => _clock();
	/// <summary>Fair buzzers' wait after the first buzz (HostSession sets Limits.BuzzWindowMs; rule checks keep 0: at once).</summary>
	public int BuzzWait;
	public int BuzzWindowMs() => BuzzWait;
	public int? TeamOf(string playerId) => Settings.TeamMode ? GetPlayer(playerId)?.Team : null;
	public IReadOnlyList<string> ActivePlayerIds() => Players.Where(p => p.Connected).Select(p => p.Id).ToList();
	public int AnswerTimeSec() => Settings.AnswerTimeSec;
	public bool HostShowsQuestions() => Settings.HostShowsQuestions;
	public bool HostUnlocksBuzzers() => Settings.HostUnlocksBuzzers;
	public bool JokerAllowed(string kind) => Settings.Jokers.Contains(kind);
	public bool ShowCategories() => Settings.ShowCategories;
	public int Score(string playerId) => Scores.GetValueOrDefault(playerId);
	/// <summary>Points from the current question (they count for its per-question stats too).</summary>
	public void Award(string playerId, int points)
	{
		ChangeScore(playerId, points);
		if (Game != null) Q(playerId).Points += points;
	}

	/// <summary>The host changes a score by hand: it counts for the night (and the host's points), not for any question.</summary>
	public void AdjustScore(string playerId, int points)
	{
		ChangeScore(playerId, points);
		NoteHostPoints(playerId, points);
	}

	public void NoteHostPoints(string playerId, int points) => StatsOf(playerId).HostPoints += points;

	void ChangeScore(string playerId, int points)
	{
		Scores[playerId] = (Scores.TryGetValue(playerId, out var s) ? s : 0) + points;
		if (points != 0) StatsOf(playerId).ScoreChangedAt = Now();
	}

	public void NoteAnswerTime(string playerId, long ms)
	{
		ms = Math.Max(0, ms);
		var s = StatsOf(playerId);
		var q = Q(playerId);
		// (a new recording replaces the last one: its time too)
		if (q.AnswerMs is { } before) s.AnswerMs -= before;
		else s.Answers++;
		s.AnswerMs += ms;
		q.AnswerMs = ms;
		q.Answered = true;
	}

	public void NoteBuzzTime(string playerId, long ms)
	{
		ms = Math.Max(0, ms);
		var s = StatsOf(playerId);
		s.BuzzMs += ms;
		s.Buzzes++;
		var q = Q(playerId);
		q.BuzzMs ??= ms;
		q.Buzzes++;
		q.Answered = true;
	}

	public void NoteWrongBuzz(string playerId) => Q(playerId).WrongBuzzes++;

	public void NoteTypos(string playerId, int typos) => Q(playerId).Typos = typos;

	public void NotePlacement(string playerId, bool right)
	{
		var q = Q(playerId);
		q.Placements++;
		if (right) q.PlacementsRight++;
		q.Answered = true;
	}

	public void NoteTookPart(string playerId, bool answered, string? joker, int? hint)
	{
		var q = Q(playerId);
		q.Answered |= answered;
		q.Joker = joker ?? "";
		q.Hint = hint ?? q.Hint;
	}

	public void NoteDistance(string playerId, double km)
	{
		var s = StatsOf(playerId);
		s.Km += km;
		s.KmAnswers++;
		Q(playerId).Km = km;
	}

	public void NoteScored(string playerId)
	{
		StatsOf(playerId).Scored++;
		Q(playerId).Right = true;
	}

	public void NotePercentOff(string playerId, double percent)
	{
		var s = StatsOf(playerId);
		s.PercentOff += percent;
		s.PercentAnswers++;
		Q(playerId).PercentOff = percent;
	}

	/// <summary>
	/// Everyone's position: most points first; same points, whoever got there first; then the faster average
	/// answer time, then the faster average buzz time; then the closer average map pin; then who joined first. Players
	/// equal in all of these share a place.
	/// </summary>
	public (List<string> Order, Dictionary<string, int> Places) Ranking()
	{
		(int, long, double, double, double) Key(Player p)
		{
			var s = StatsOf(p.Id);
			return (-Score(p.Id), s.ScoreChangedAt ?? long.MaxValue, s.AvgAnswerMs ?? double.MaxValue, s.AvgBuzzMs ?? double.MaxValue, s.AvgKm ?? double.MaxValue);
		}
		var order = Players.Select((p, joined) => (P: p, Key: Key(p), Joined: joined)).OrderBy(x => x.Key).ThenBy(x => x.Joined).ToList();
		var places = new Dictionary<string, int>();
		for (var i = 0; i < order.Count; i++)
			places[order[i].P.Id] = i > 0 && order[i].Key == order[i - 1].Key ? places[order[i - 1].P.Id] : i + 1;
		return (order.Select(x => x.P.Id).ToList(), places);
	}

	// ---------- team mode ----------

	/// <summary>Counts up with every draw (RoomState.TeamDraw): each screen plays the draw once.</summary>
	public int TeamDraw { get; private set; }

	/// <summary>
	/// The host draws the teams: everyone here, shuffled, dealt out one by one, so teams differ by one player at most.
	/// Again as often as the host likes, until the quiz starts.
	/// </summary>
	public Result DrawTeams(Random? rng = null)
	{
		if (Phase != Shared.Phase.Lobby) return Result.Fail("Teams are drawn in the waiting room.");
		if (!Settings.TeamMode) return Result.Fail("Team mode is off.");
		var here = Players.Where(p => p.Connected || p.Saved).ToList();
		if (here.Count < Settings.Teams) return Result.Fail(Lang.F("You need at least {0} players for {0} teams.", Settings.Teams));
		// (no team smaller than 3/4 of another: TeamNames.Splits)
		if (!TeamNames.Splits(here.Count).Contains(Settings.Teams))
			return Result.Fail(TeamNames.Possible(here.Count) ? Lang.F("{0} teams wouldn't be fair with {1} players.", Settings.Teams, here.Count)
				: Lang.F("{0} players can't be split into fair teams.", here.Count));
		rng ??= Random.Shared;
		var order = here.OrderBy(_ => rng.Next()).ToList();
		// (which team gets the first, and so the possible extra, player is drawn too)
		var start = rng.Next(Settings.Teams);
		for (var i = 0; i < order.Count; i++) order[i].Team = (start + i) % Settings.Teams;
		foreach (var p in Players.Except(here)) p.Team = null;
		// (new teams: the names they gave themselves go)
		_teamNames.Clear();
		TeamDraw++;
		Trace.Log("room", $"teams drawn: {string.Join(" | ", Enumerable.Range(0, Settings.Teams).Select(t => string.Join(", ", order.Where(p => p.Team == t).Select(p => p.Name))))}");
		return Result.Success();
	}

	/// <summary>Names the teams gave themselves (0.14), by team; the rest have their drawn name (TeamNames.Name).</summary>
	readonly Dictionary<int, string> _teamNames = new();

	public string TeamName(int team) => _teamNames.TryGetValue(team, out var name) ? name : TeamNames.Name(team);

	/// <summary>
	/// After the draw, in the waiting room, a team names itself: anyone in it, or the host for any team. Null rolls a new
	/// name (one no other team has). Two teams can't share a name.
	/// </summary>
	public Result RenameTeam(string by, int team, string? rawName, Random? rng = null)
	{
		if (Phase != Shared.Phase.Lobby) return Result.Fail("Team names can only be changed in the waiting room.");
		if (!Settings.TeamMode || team < 0 || team >= Settings.Teams || !Players.Any(p => p.Team != null)) return Result.Fail("The teams aren't drawn yet.");
		if (by != "host" && GetPlayer(by)?.Team != team) return Result.Fail("Only the team itself can change its name.");
		var others = Enumerable.Range(0, Settings.Teams).Where(t => t != team).Select(TeamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		string name;
		if (rawName == null)
		{
			var free = TeamNames.All.Select(n => n.Name).Concat(TeamNames.More).Where(n => !others.Contains(n) && n != TeamName(team)).ToList();
			name = free[(rng ?? Random.Shared).Next(free.Count)];
		}
		else
		{
			name = TeamNames.Clean(rawName);
			if (name == "") return Result.Fail("Please enter a name.");
			if (others.Contains(name)) return Result.Fail("Another team already has that name.");
		}
		_teamNames[team] = name;
		Trace.Log("room", $"team {team} is now called {name} (by {by})");
		return Result.Success();
	}

	/// <summary>The host picked how many teams (not left to "Play in teams" choosing by the number of players).</summary>
	bool _teamsPicked;

	/// <summary>Who a draw puts in teams: everyone here, and saved seats of a continued night.</summary>
	int TeamPlayers => Players.Count(p => p.Connected || p.Saved);

	/// <summary>
	/// Before the draw, the number of teams follows the players: the best split for how many are here (TeamNames.Suggest),
	/// unless the host picked one that still works. True if it changed.
	/// </summary>
	bool FollowPlayers()
	{
		if (!Settings.TeamMode || Players.Any(p => p.Team != null)) return false;
		if (_teamsPicked && TeamNames.Splits(TeamPlayers).Contains(Settings.Teams)) return false;
		_teamsPicked = false;
		var best = TeamNames.Suggest(TeamPlayers);
		if (best == Settings.Teams) return false;
		Settings.Teams = best;
		return true;
	}

	/// <summary>A newcomer once the teams are drawn: the smallest team gets them (no draw for one player).</summary>
	void JoinSmallestTeam(Player player)
	{
		if (!Settings.TeamMode || !Players.Any(p => p.Team != null)) return;
		player.Team = Enumerable.Range(0, Settings.Teams).OrderBy(t => Players.Count(p => p.Team == t && p.Connected)).First();
	}

	/// <summary>The teams by place: members' points together; equal points share a place.</summary>
	public List<PublicTeam>? Teams()
	{
		if (!Settings.TeamMode || !Players.Any(p => p.Team != null)) return null;
		var teams = Enumerable.Range(0, Settings.Teams).Select(t => new PublicTeam
		{
			Index = t, Name = TeamName(t), Color = TeamNames.Color(t),
			Members = Players.Where(p => p.Team == t).Select(p => p.Id).ToList(),
			Score = Players.Where(p => p.Team == t).Sum(p => Score(p.Id)),
		}).OrderByDescending(t => t.Score).ThenBy(t => t.Index).ToList();
		for (var i = 0; i < teams.Count; i++) teams[i].Place = i > 0 && teams[i].Score == teams[i - 1].Score ? teams[i - 1].Place : i + 1;
		return teams;
	}

	/// <summary>The room as one viewer may see it: "host", or a player id.</summary>
	public RoomState ToState(string viewer)
	{
		var state = SharedState();
		state.Game = ViewFor(viewer);
		return state;
	}

	/// <summary>The current question as one viewer sees it (null outside the game).</summary>
	public GameView? ViewFor(string viewer) => Game != null && Phase == Shared.Phase.Playing ? Game.View(viewer) : null;

	/// <summary>The state without the game view: the same for every viewer, so a broadcast makes it once (HostSession).</summary>
	public RoomState SharedState()
	{
		var (order, places) = Ranking();
		return new()
		{
			Ranking = order,
			Places = places,
			Stats = Stats.ToDictionary(s => s.Key, s => new NightStats
			{
				Scored = s.Value.Scored, Answers = s.Value.Answers, AvgAnswerS = s.Value.AvgAnswerMs / 1000, Buzzes = s.Value.Buzzes,
				AvgBuzzS = s.Value.AvgBuzzMs / 1000, AvgKm = s.Value.AvgKm, AvgPercentOff = s.Value.AvgPercentOff,
			}),
			Code = Code,
			Quiz = new QuizInfo { Id = Quiz.Id, Title = Quiz.Title, QuestionCount = Quiz.Questions.Count },
			Scores = new Dictionary<string, int>(Scores),
			ServerNow = Now(),
			Phase = Phase,
			Settings = Settings.Clone(),
			HostConnected = true,
			ResumeFrom = Phase == Shared.Phase.Lobby && Resume is { } save ? Math.Min(save.Next, Quiz.Questions.Count - 1) + 1 : null,
			HostName = HostName,
			Awards = Phase == Shared.Phase.Finished ? _awards ??= Server.Awards.Make(this) : null,
			Teams = Teams(),
			TeamDraw = TeamDraw,
			Players = Players.Select(p => new PublicPlayer { Id = p.Id, Name = p.Name, Color = p.Color, Connected = p.Connected, JoinedAt = p.JoinedAt, LeftAt = p.LeftAt, Version = p.Version, Team = Settings.TeamMode ? p.Team : null }).ToList(),
		};
	}

	public static string CleanName(string raw)
	{
		var name = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		static string Cut(string s, int max) => s.Length > max ? s[..max].TrimEnd() : s;
		// (a clan tag in front has its own room: "[OTB] maxlvl01" is fine)
		var (tag, rest) = Limits.SplitClanTag(name);
		return tag is { Length: > 0 } t ? $"[{Cut(t, Limits.ClanTagMaxLength)}] {Cut(rest, Limits.NameMaxLength)}" : Cut(name, Limits.NameMaxLength);
	}

	bool Playing => Phase == Shared.Phase.Playing && Game != null;

	/// <summary>
	/// Seats a new player, or gives a returning one (same id) their seat back (and the game goes on if it waited
	/// for them). During a game, new players join too: they play from the next question.
	/// </summary>
	public (Player? Player, string? Error) Join(string id, string rawName)
	{
		Trace.Log("room", $"join {id} as \"{rawName}\" ({Phase}, {Players.Count} in the room)");
		var existing = GetPlayer(id) ?? Reclaim(id, rawName);
		if (existing != null)
		{
			existing.Connected = true;
			existing.DisconnectedAt = null;
			existing.LeftAt = null;
			if (Playing) Game!.PlayerBack(id);
			return (existing, null);
		}
		var name = CleanName(rawName);
		if (name == "") return (null, "Please enter a name.");
		if (Phase == Shared.Phase.Finished) return (null, "This quiz has already ended.");
		if (Players.Count >= Limits.MaxPlayers) return (null, "This room is full.");
		if (Players.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
			return (null, "Someone in this room already uses that name.");
		var used = Players.Select(p => p.Color).ToHashSet();
		var player = new Player
		{
			Id = id,
			Name = name,
			Color = Limits.PlayerColors.FirstOrDefault(c => !used.Contains(c)) ?? Limits.PlayerColors[0],
			Connected = true,
		};
		Players.Add(player);
		JoinSmallestTeam(player);
		if (Playing)
		{
			player.JoinedAt = Game!.Index + 1;
			Scores[id] = 0;
			Game.LateJoin(id);
		}
		return (player, null);
	}

	/// <summary>
	/// A saved night's lobby: a LAN player gets their seat back by their name (their game's id is new every start). Steam
	/// ids stay the same, so Steam seats only go back to the same account.
	/// </summary>
	Player? Reclaim(string id, string rawName)
	{
		if (Resume == null || Phase != Shared.Phase.Lobby || id.StartsWith("steam:")) return null;
		var name = CleanName(rawName);
		var seat = Players.Find(p => p.Saved && !p.Connected && !p.Id.StartsWith("steam:") && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
		if (seat == null) return null;
		var old = seat.Id;
		seat.Id = id;
		if (Scores.Remove(old, out var score)) Scores[id] = score;
		if (Stats.Remove(old, out var stats)) Stats[id] = stats;
		foreach (var key in QuestionStats.Keys.Where(k => k.Player == old).ToList())
		{
			QuestionStats.Remove(key, out var q);
			QuestionStats[(key.Question, id)] = q!;
		}
		foreach (var j in Resume.Jokers.Where(j => j.PlayerId == old)) j.PlayerId = id;
		return seat;
	}

	/// <summary>A player changes their name (in the lobby only, and not to one someone else uses).</summary>
	public Result Rename(string id, string rawName)
	{
		if (Phase != Shared.Phase.Lobby || GetPlayer(id) is not { } player) return Result.Fail("Names can only be changed in the waiting room.");
		var name = CleanName(rawName);
		if (name == "") return Result.Fail("Please enter a name.");
		if (Players.Any(p => p != player && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
			return Result.Fail("Someone in this room already uses that name.");
		player.Name = name;
		return Result.Success();
	}

	/// <summary>The connection dropped. During a game the seat and score stay, and the game waits for them.</summary>
	public void Disconnect(string id)
	{
		Trace.Log("room", $"disconnected {id} ({Phase})");
		var p = GetPlayer(id);
		if (p == null || !p.Connected) return;
		p.Connected = false;
		p.DisconnectedAt = Now();
		if (!Playing) return;
		p.LeftAt = Game!.Index + 1;
		Game.PlayerLeft(id);
	}

	/// <summary>
	/// A player leaves: in the lobby their seat goes; during a game it's kept so they can come back, and on the podium
	/// too (they finished the night: they stay on it).
	/// </summary>
	public void Leave(string id)
	{
		Trace.Log("room", $"leave {id} ({Phase})");
		if (Playing || Phase == Shared.Phase.Finished || GetPlayer(id) is { Saved: true }) Disconnect(id);
		else Remove(id);
	}

	public void Remove(string id)
	{
		Players.RemoveAll(p => p.Id == id);
		if (Game == null) return;
		Game.PlayerBack(id);
		Game.CheckAllAnswered();
	}

	public void UpdateSettings(int? answerTimeSec = null, bool? hostShows = null, bool? hostUnlocks = null, (string Kind, bool On)? joker = null,
		bool? categories = null, bool? friendsOnly = null, int? teams = null)
	{
		// (another number of teams: the old draw is gone, the host draws again)
		if (teams is { } t && Phase == Shared.Phase.Lobby)
		{
			// "Play in teams" ticked: the number follows the players (Tick) until the host picks a split
			_teamsPicked = t >= TeamNames.Min;
			var count = t == TeamNames.Auto ? TeamNames.Suggest(TeamPlayers) : t < TeamNames.Min ? 0 : Math.Min(t, TeamNames.Max);
			if (count != Settings.Teams)
			{
				Settings.Teams = count;
				foreach (var p in Players) p.Team = null;
			}
		}
		if (categories is { } cats) Settings.ShowCategories = cats;
		if (friendsOnly is { } friends) Settings.FriendsOnly = friends;
		if (joker is { } j && Joker.All.Contains(j.Kind))
		{
			Settings.Jokers.Remove(j.Kind);
			if (j.On) Settings.Jokers.Add(j.Kind);
		}
		if (answerTimeSec is { } at) Settings.AnswerTimeSec = Math.Clamp(at, Limits.AnswerTimeMin, Limits.AnswerTimeMax);
		if (hostShows is { } shows) Settings.HostShowsQuestions = shows;
		if (hostUnlocks is { } unlocks) Settings.HostUnlocksBuzzers = unlocks;
	}

	/// <summary>When the quiz started (local time), for the results: the match history shows the night's start.</summary>
	public DateTime? StartedAt;

	public Result Start()
	{
		Trace.Log("room", $"start: {string.Join(", ", Players.Select(p => $"{p.Name}{(p.Connected ? "" : " (away)")}"))}");
		if (Phase != Shared.Phase.Lobby) return Result.Fail("The quiz is already running.");
		var connected = Players.Count(p => p.Connected);
		if (connected < (Preview ? 1 : Limits.MinPlayersToStart)) return Result.Fail(Lang.F("You need at least {0} connected players.", Limits.MinPlayersToStart));
		if (Settings.TeamMode && Players.Any(p => p.Connected && p.Team == null)) return Result.Fail("Draw the teams first.");
		Phase = Shared.Phase.Playing;
		if (Resume is { } save) return GoOn(save);
		StartedAt = DateTime.Now;
		Scores.Clear();
		Stats.Clear();
		QuestionStats.Clear();
		foreach (var p in Players)
		{
			Scores[p.Id] = 0;
			p.JoinedAt = p.LeftAt = null;
		}
		Game = new Game(Quiz, this, null, SharedFiles);
		NoteOpened();
		return Result.Success();
	}

	// ---------- saving the night (SavedNight, Storage.NightSaves) ----------

	/// <summary>The saved night this lobby continues (Restore), until the quiz starts.</summary>
	public SavedNight? Resume { get; private set; }

	/// <summary>The night as it was when the current question opened (what a save holds until its reveal).</summary>
	string? _openSave;

	void NoteOpened() => _openSave = Game is { Index: > 0 } g ? Snapshot(g.Index).ToJson() : null;

	/// <summary>
	/// The night so far, to save: during a reveal, everything up to and with this question; while a question runs,
	/// everything before it. Null when there's nothing to keep (no game, or still on the first question).
	/// </summary>
	public string? SaveJson() =>
		!Playing ? null
		: Game!.Stage == Shared.Stage.Reveal ? Snapshot(Game.Index + 1).ToJson()
		: _openSave;

	/// <summary>Everything up to question <paramref name="next"/> (0-based; it and the ones after it are still to play).</summary>
	internal SavedNight Snapshot(int next) => new()
	{
		Version = Godot.ProjectSettings.GetSetting("application/config/version").AsString(),
		QuizId = Quiz.Id,
		QuizTitle = Quiz.Title,
		Questions = Quiz.Questions.Count,
		Next = next,
		StartedAt = StartedAt,
		Settings = Settings.Clone(),
		Players = Players.Select(p => new SavedPlayer
		{
			Id = p.Id, Name = p.Name, Color = p.Color, Score = Score(p.Id), JoinedAt = p.JoinedAt, LeftAt = p.LeftAt, Stats = StatsOf(p.Id), Team = p.Team,
		}).ToList(),
		QuestionStats = QuestionStats.Where(q => q.Key.Question < next)
			.Select(q => new SavedQuestionStat { Question = q.Key.Question, Player = q.Key.Player, Stat = q.Value }).ToList(),
		Jokers = Game?.UsedJokers(next) ?? new(),
		TeamNames = _teamNames.Count == 0 ? null : Enumerable.Range(0, Settings.Teams).Select(t => _teamNames.GetValueOrDefault(t)).ToList(),
	};

	/// <summary>
	/// The lobby continues a saved night: its rules, and everyone's seat with their points and stats (away until they
	/// join again).
	/// </summary>
	public void Restore(SavedNight save)
	{
		if (Phase != Shared.Phase.Lobby) return;
		Resume = save;
		var s = save.Settings;
		(Settings.AnswerTimeSec, Settings.BuzzerSound, Settings.PodiumSound) = (s.AnswerTimeSec, s.BuzzerSound, s.PodiumSound);
		(Settings.HostShowsQuestions, Settings.HostUnlocksBuzzers, Settings.ShowCategories, Settings.FriendsOnly) = (s.HostShowsQuestions, s.HostUnlocksBuzzers, s.ShowCategories, s.FriendsOnly);
		Settings.Jokers = s.Jokers.Where(Joker.All.Contains).ToList();
		Settings.Teams = s.Teams;
		Players.Clear();
		Scores.Clear();
		Stats.Clear();
		QuestionStats.Clear();
		foreach (var p in save.Players.Where(p => p.Id != "").DistinctBy(p => p.Id).Take(Limits.MaxPlayers))
		{
			Players.Add(new Player { Id = p.Id, Name = p.Name, Color = p.Color, JoinedAt = p.JoinedAt, LeftAt = p.LeftAt, Saved = true, Team = p.Team });
			Scores[p.Id] = p.Score;
			Stats[p.Id] = p.Stats ?? new PlayerStats();
		}
		foreach (var q in save.QuestionStats.Where(q => q.Stat != null)) QuestionStats[(q.Question, q.Player)] = q.Stat;
		_teamNames.Clear();
		for (var t = 0; t < (save.TeamNames?.Count ?? 0); t++)
			if (save.TeamNames![t] is { } name && TeamNames.Clean(name) is { Length: > 0 } clean) _teamNames[t] = clean;
	}

	/// <summary>
	/// Start, for a saved night: everyone keeps their points and goes on at the next question. Whoever isn't back keeps
	/// their seat (they can still come back); new players join from here, like joining a running game.
	/// </summary>
	Result GoOn(SavedNight save)
	{
		Resume = null;
		StartedAt = save.StartedAt ?? DateTime.Now;
		var next = Math.Clamp(save.Next, 0, Quiz.Questions.Count);
		foreach (var p in Players)
		{
			if (!p.Saved)
			{
				Scores[p.Id] = 0;
				p.JoinedAt = next + 1;
			}
			else if (!p.Connected) p.LeftAt ??= next + 1;
		}
		Game = new Game(Quiz, this, null, SharedFiles, next);
		Game.RestoreJokers(save.Jokers.Where(j => GetPlayer(j.PlayerId) != null));
		// (saved during the last reveal: straight to the podium)
		if (next >= Quiz.Questions.Count)
		{
			Game.Next();
			Phase = Shared.Phase.Finished;
		}
		NoteOpened();
		return Result.Success();
	}

	/// <summary>Reveal if still open, otherwise the next question (or the podium after the last one).</summary>
	public void Next()
	{
		Trace.Log("room", $"next (question {(Game?.Index ?? -1) + 1}, {Game?.Stage})");
		if (Game == null || Phase != Shared.Phase.Playing || Game.Paused) return;
		if (Game.Stage == Stage.Open) Game.HostReveal();
		else
		{
			Game.Next();
			if (Game.Finished) Phase = Shared.Phase.Finished;
			else NoteOpened();
		}
	}

	public void Finish()
	{
		if (Phase != Shared.Phase.Playing) return;
		Phase = Shared.Phase.Finished;
	}

	public void BackToLobby()
	{
		Game = null;
		Scores.Clear();
		Stats.Clear();
		QuestionStats.Clear();
		foreach (var p in Players)
		{
			p.JoinedAt = p.LeftAt = null;
			p.Saved = false;
		}
		_openSave = null;
		_awards = null;
		Phase = Shared.Phase.Lobby;
	}

	/// <summary>Runs game timers and drops lobby players whose grace period ran out. True if anything changed.</summary>
	public bool Tick()
	{
		var changed = Phase == Shared.Phase.Playing && Game != null && Game.Tick();
		if (Phase == Shared.Phase.Lobby)
		{
			var now = Now();
			changed |= Players.RemoveAll(p => !p.Saved && !p.Connected && p.DisconnectedAt is { } at && now - at > PlayerGraceMs) > 0;
			changed |= FollowPlayers();
		}
		return changed;
	}
}
