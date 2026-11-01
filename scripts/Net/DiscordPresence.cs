// Discord Rich Presence: friends on Discord see "Playing Quiznight?" and what you're doing (menus, making a quiz, a quiz
// night's lobby or question). Talks to the Discord app running on this PC (DiscordRichPresence package, a local pipe);
// if Discord isn't running nothing happens. Never shows the room code.

using System;
using DiscordRPC;
using QuizNight.Shared;
using QuizNight.Storage;

namespace QuizNight.Net;

public static class DiscordPresence
{
	/// <summary>Quiznight?'s Discord application (not a secret: it only names the game in Discord).</summary>
	const string AppId = "1566331232659177582";

	static DiscordRpcClient? _client;
	static string _shown = "";
	/// <summary>When the current activity began (Discord shows "for 12:34"); a new activity starts it again.</summary>
	static DateTime _since = DateTime.UtcNow;
	static string _activity = "";

	/// <summary>What the player is doing now. "" for a line that isn't needed.</summary>
	public readonly record struct Status(string Activity, string Details, string State = "", int PartySize = 0, int PartyMax = 0);

	/// <summary>Shows this status (on change only: Discord takes a few updates a minute), or turns off with the setting.</summary>
	public static void Show(Status status)
	{
		if (!Settings.DiscordPresence || Godot.DisplayServer.GetName() == "headless")
		{
			Stop();
			return;
		}
		try
		{
			if (_client == null)
			{
				_client = new DiscordRpcClient(AppId) { SkipIdenticalPresence = true };
				_client.Initialize();
				_shown = "";
			}
			if (status.Activity != _activity)
			{
				_activity = status.Activity;
				_since = DateTime.UtcNow;
			}
			var key = $"{status.Details}|{status.State}|{status.PartySize}/{status.PartyMax}|{_since.Ticks}";
			if (key == _shown) return;
			_shown = key;
			_client.SetPresence(new RichPresence
			{
				Details = Lang.T(status.Details),
				State = status.State == "" ? null : status.State,
				Timestamps = new Timestamps(_since),
				Assets = new Assets { LargeImageKey = "logo", LargeImageText = "Quiznight? " + App.Version },
				Party = status.PartyMax > 0 ? new Party { ID = "quiznight", Size = status.PartySize, Max = status.PartyMax } : null,
			});
		}
		catch (Exception e)
		{
			// (Discord's pipe can go away any time: never let it break the game)
			Trace.Log("discord", $"presence failed: {e.Message}");
			Stop();
		}
	}

	public static void Stop()
	{
		if (_client == null) return;
		try
		{
			_client.ClearPresence();
			_client.Dispose();
		}
		catch (Exception) { }
		_client = null;
		_shown = "";
	}
}
