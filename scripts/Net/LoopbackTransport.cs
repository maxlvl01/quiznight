// A host and one player in the same game, connected in memory: for the editor's question preview (no network, no port).

using System;
using System.Collections.Generic;

namespace QuizNight.Net;

/// <summary>
/// Both ends of an in-memory connection. Messages wait in a queue until the receiving side polls, like a real transport,
/// so the host and the player see the same order of events as over LAN or Steam.
/// </summary>
public sealed class Loopback
{
	public const string Peer = "local";

	public readonly LoopbackHost Host;
	public readonly LoopbackClient Client;

	public Loopback()
	{
		Host = new LoopbackHost(this);
		Client = new LoopbackClient(this);
	}

	internal readonly Queue<byte[]> ToHost = new();
	internal readonly Queue<byte[]> ToClient = new();
	internal bool Open = true;
}

public sealed class LoopbackHost : IHostTransport
{
	readonly Loopback _link;
	internal LoopbackHost(Loopback link) => _link = link;

	public event Action<string>? PeerConnected;
	public event Action<string>? PeerDisconnected;
	public event Action<string, byte[]>? Received;

	internal void Connect() => PeerConnected?.Invoke(Loopback.Peer);
	internal void Drop() => PeerDisconnected?.Invoke(Loopback.Peer);

	public string? PeerSteamId(string peer) => null;
	public void Send(string peer, byte[] data)
	{
		if (_link.Open) _link.ToClient.Enqueue(data);
	}
	public bool TrySend(string peer, byte[] data)
	{
		Send(peer, data);
		return true;
	}
	public void Disconnect(string peer) => _link.Client.Close("The preview ended.");
	public void Poll()
	{
		while (_link.ToHost.TryDequeue(out var data)) Received?.Invoke(Loopback.Peer, data);
	}
	// (no real message limit in memory; assets are still split like over LAN)
	public int MaxMessageBytes => 1024 * 1024;
	public void Dispose() => _link.Open = false;
}

public sealed class LoopbackClient : IClientTransport
{
	readonly Loopback _link;
	bool _connected;
	string? _closed;
	internal LoopbackClient(Loopback link) => _link = link;

	public event Action? Connected;
	public event Action<string>? Disconnected;
	public event Action<byte[]>? Received;

	internal void Close(string reason) => _closed ??= reason;

	public void Send(byte[] data)
	{
		if (_link.Open) _link.ToHost.Enqueue(data);
	}
	public void Poll()
	{
		// (connected on the first poll, after the session has hooked up its events)
		if (!_connected && _link.Open)
		{
			_connected = true;
			_link.Host.Connect();
			Connected?.Invoke();
		}
		while (_link.ToClient.TryDequeue(out var data)) Received?.Invoke(data);
		if (_closed is { } reason)
		{
			_closed = null;
			_link.Open = false;
			Disconnected?.Invoke(reason);
		}
	}
	public void Dispose()
	{
		if (!_link.Open) return;
		_link.Open = false;
		_link.Host.Drop();
	}
}
