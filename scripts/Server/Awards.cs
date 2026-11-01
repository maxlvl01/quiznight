// The night's little awards for the podium's credits (Shared.NightAwards has their names): the best at buzzing,
// answering, maps, estimations … from the room's stats. An award needs enough tries to mean something, and ties share it.

using System;
using System.Collections.Generic;
using System.Linq;
using QuizNight.Shared;

namespace QuizNight.Server;

public static class Awards
{
	/// <summary>Every award somebody won tonight (one nobody qualified for is left out).</summary>
	public static List<NightAward> Make(Room room)
	{
		var ids = room.Players.Select(p => p.Id).ToList();
		var stats = ids.ToDictionary(id => id, room.StatsOf);
		var perQuestion = room.QuestionStats.Where(q => ids.Contains(q.Key.Player)).ToList();
		var awards = new List<NightAward>();

		// (lowest wins; null = didn't qualify)
		void Least(string kind, Func<string, double?> value) => Add(kind, ids.Select(id => (id, value(id))), false);
		void Most(string kind, Func<string, double?> value) => Add(kind, ids.Select(id => (id, value(id))), true);
		void Add(string kind, IEnumerable<(string Id, double? Value)> values, bool most)
		{
			var scored = values.Where(v => v.Value is { } x && !double.IsNaN(x)).Select(v => (v.Id, Value: v.Value!.Value)).ToList();
			if (scored.Count == 0) return;
			var best = most ? scored.Max(v => v.Value) : scored.Min(v => v.Value);
			if (most && best <= 0) return;
			awards.Add(new NightAward { Kind = kind, Value = best, Players = scored.Where(v => Math.Abs(v.Value - best) < 1e-9).Select(v => v.Id).ToList() });
		}
		int CountOf(string id, Func<QuestionStat, bool> where) => perQuestion.Count(q => q.Key.Player == id && where(q.Value));

		Most("most-right", id => stats[id].Scored);
		Least("fastest-buzzer", id => stats[id] is { Buzzes: >= 2 } s ? s.AvgBuzzMs / 1000 : null);
		Least("quickest-answers", id => stats[id] is { Answers: >= 2 } s ? s.AvgAnswerMs / 1000 : null);
		Most("big-haul", id => perQuestion.Where(q => q.Key.Player == id).Select(q => (double?)q.Value.Points).Max());
		Least("map-master", id => stats[id] is { KmAnswers: >= 2 } s ? s.AvgKm : null);
		Least("bullseye", id => perQuestion.Where(q => q.Key.Player == id && q.Value.Km != null).Select(q => q.Value.Km).Min());
		Least("best-guesser", id => stats[id] is { PercentAnswers: >= 2 } s ? s.AvgPercentOff : null);
		Most("best-speller", id => CountOf(id, q => q.Typos == 0) is var n and >= 2 ? n : null);
		Most("mind-reader", id => CountOf(id, q => q.Right && q.Hint == 1));
		Most("time-traveller", id => perQuestion.Where(q => q.Key.Player == id).Sum(q => q.Value.PlacementsRight) is var n and >= 2 ? n : null);
		Most("host-favourite", id => stats[id].HostPoints);
		Most("trigger-happy", id => perQuestion.Where(q => q.Key.Player == id).Sum(q => q.Value.WrongBuzzes) is var n and >= 2 ? n : null);
		return awards;
	}
}
